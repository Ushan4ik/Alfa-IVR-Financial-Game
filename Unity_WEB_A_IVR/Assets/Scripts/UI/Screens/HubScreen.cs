using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IVR;
using IVR.Game;
using IVR.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace IVR.UI
{
    public enum HubTab { Home, Learn, MiniGames, Quests, Rating, Profile, Market }

    // Общая оболочка главного меню. Новые вкладки добавляются здесь и в HubScreen.uxml.
    public class HubScreen : BaseScreen
    {
        private readonly Dictionary<HubTab, VisualElement> _pages = new();
        private readonly Dictionary<HubTab, Button> _navButtons = new();
        private PlayerProfile _profile;
        private DailyProgressDto _dailyProgress;
        private LessonDefinition _selectedLesson;
        private PersonalLessonDto _selectedPersonalLesson;
        private int _learningRequestVersion;
        private string _pendingPersonalLessonId;
        private bool _hasPendingLessons;
        private bool _learningRefreshInFlight;
        private HubTab _activeTab;
        private int _questFilter; // 0 — все, 1 — в процессе, 2 — готово
        private int _officeClicks;
        private bool _miniGameReported;
        private bool _miniGameReportInFlight;
        private MarketPanel _market;

        public HubScreen(VisualTreeAsset asset, VisualElement container)
            : base(asset, container) { }

        protected override void RegisterCallbacks()
        {
            BindTab(HubTab.Home, "home");
            BindTab(HubTab.Learn, "learn");
            BindTab(HubTab.MiniGames, "minigames");
            BindTab(HubTab.Quests, "quests");
            BindTab(HubTab.Rating, "rating");
            BindTab(HubTab.Profile, "profile");
            _pages[HubTab.Market] = Root.Q<VisualElement>("tab-market");
            _market = new MarketPanel(Root, () => Navigate(HubTab.Home));

            Root.Q<Button>("home-start-quiz").clicked += () => GameManager.Instance.StartQuiz();
            Root.Q<Button>("home-avatar").clicked += () => Navigate(HubTab.Profile);
            Root.Q<Button>("profile-quiz").clicked += () => GameManager.Instance.StartQuiz();
            Root.Q<Button>("home-market").clicked += () => Navigate(HubTab.Market);
            Root.Q<Button>("profile-market").clicked += () => Navigate(HubTab.Market);
            Root.Q<Button>("profile-interests-button").clicked += () => ScreenManager.Instance.ShowInterests();
            Root.Q<Button>("profile-logout").clicked += Logout;

            Root.Q<Button>("lesson-back").clicked += CloseLesson;
            Root.Q<Button>("lesson-story-next").clicked += ShowLessonLearningStep;
            Root.Q<Button>("lesson-learning-back").clicked += ShowLessonStoryStep;
            Root.Q<Button>("learning-refresh").clicked += () => _ = RefreshPersonalLessons();
            Root.Q<Button>("learning-diagnostic").clicked += () => GameManager.Instance.StartQuiz(QuizConfig.Diagnostic());
            Root.Q<Button>("lesson-quiz").clicked += () =>
            {
                if (_selectedPersonalLesson != null)
                    GameManager.Instance.StartQuiz(QuizConfig.ForPersonalLesson(_selectedPersonalLesson.id, _selectedPersonalLesson.topicId));
                else if (_selectedLesson != null)
                    GameManager.Instance.StartQuiz(QuizConfig.ForLesson(_selectedLesson.Id));
            };

            Root.Q<Button>("quest-all").clicked += () => SetQuestFilter(0);
            Root.Q<Button>("quest-active").clicked += () => SetQuestFilter(1);
            Root.Q<Button>("quest-done").clicked += () => SetQuestFilter(2);
            Root.Q<Button>("rating-refresh").clicked += () => _ = RefreshRating();
            Root.Q<Button>("hub-result-close").clicked += CloseResult;
            Root.Q<Button>("office-clicker-button").clicked += OnOfficeClick;
            Root.schedule.Execute(() =>
            {
                if (_hasPendingLessons && !_learningRefreshInFlight && _activeTab == HubTab.Learn &&
                    IvrApi.Auth.IsLoggedIn) _ = RefreshPersonalLessons();
            }).Every(3000);
            RenderMiniGames();
        }

        private void BindTab(HubTab tab, string name)
        {
            _pages[tab] = Root.Q<VisualElement>("tab-" + name);
            var button = Root.Q<Button>("nav-" + name);
            _navButtons[tab] = button;
            button.clicked += () => Navigate(tab);
        }

        public new void Show()
        {
            base.Show();
            _learningRequestVersion++;
            Root.Q<Label>("top-score").text = "Загрузка...";
            Root.Q<Label>("top-streak").text = "Серия —";
            Root.Q<Label>("home-greeting").text = "Привет!";
            Root.Q<Label>("profile-name").text = "Игрок";
            Root.Q<Label>("profile-email").text = "";
            Root.Q<Label>("profile-portfolio").text = "Загрузка...";
            Root.Q<VisualElement>("hub-result-overlay").style.display = DisplayStyle.None;
            Navigate(HubTab.Home);
            _ = RefreshProfile();
            _ = RefreshDailyProgress();

            var result = GameManager.Instance?.LastResult;
            if (result != null)
            {
                Root.Q<Label>("hub-result-title").text = result.Passed ? "Отличная работа!" : "Попробуй ещё раз";
                Root.Q<Label>("hub-result-score").text = "+" + result.Score + " очков";
                Root.Q<Label>("hub-result-message").text = GameManager.Instance.LastSaveError ??
                    (result.Passed ? "Твой результат сохранён. Продолжай изучать темы!"
                        : "Повтори материал и попробуй снова.");
                Root.Q<Button>("hub-result-close").text = string.IsNullOrEmpty(GameManager.Instance.LastLearningLessonId)
                    ? "Продолжить" : "Открыть разбор →";
                Root.Q<VisualElement>("hub-result-overlay").style.display = DisplayStyle.Flex;
            }
        }

        public void Navigate(HubTab tab)
        {
            _activeTab = tab;
            foreach (var pair in _pages)
                pair.Value.style.display = pair.Key == tab ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var pair in _navButtons)
                pair.Value.EnableInClassList("hub-nav-active", pair.Key == tab);

            Root.Q<VisualElement>("hub-nav").style.display =
                tab == HubTab.Market ? DisplayStyle.None : DisplayStyle.Flex;

            Root.Q<ScrollView>("hub-scroll").scrollOffset = Vector2.zero;
            if (tab == HubTab.Learn) { CloseLesson(); _ = RefreshPersonalLessons(); }
            if (tab == HubTab.MiniGames) ShowMiniGameCatalog();
            if (tab == HubTab.Quests) _ = RefreshDailyProgress();
            if (tab == HubTab.Rating) _ = RefreshRating();
            if (tab == HubTab.Profile) _ = RefreshPortfolio();
            if (tab == HubTab.Market) _ = _market.Refresh();
        }

        private async Task RefreshProfile()
        {
            var result = await IvrApi.Player.GetProfile();
            if (ScreenManager.Instance.Current != AppScreen.Hub || !IvrApi.Auth.IsLoggedIn) return;
            if (!result.Success)
            {
                if (result.Error != null && result.Error.StartsWith("401"))
                {
                    IvrApi.Auth.Logout();
                    ScreenManager.Instance.Show(AppScreen.Login);
                }
                else
                    Root.Q<Label>("top-score").text = "Нет связи";
                return;
            }

            _profile = result.Data;
            GameManager.Instance.SetProfile(_profile);
            RenderProfile();
            RenderHome();
            RenderLessons();
            RenderQuests();
            if (_activeTab == HubTab.Profile) _ = RefreshPortfolio();
        }

        private void RenderProfile()
        {
            if (_profile == null) return;
            Root.Q<Label>("top-score").text = _profile.finLiteracyScore + " очков";
            Root.Q<Label>("profile-name").text = _profile.nickname;
            Root.Q<Label>("profile-email").text = _profile.email;
            Root.Q<Label>("profile-level").text = "Уровень " + _profile.level;
            Root.Q<Label>("profile-score").text = _profile.finLiteracyScore + " очков знаний";
            Root.Q<Label>("profile-interests").text = "Интересы: " +
                (_profile.interests == null || _profile.interests.Count == 0
                    ? "не указаны"
                    : string.Join(", ", _profile.interests));
        }

        private void RenderHome()
        {
            if (_profile == null) return;
            Root.Q<Label>("home-greeting").text = "Привет, " + _profile.nickname + "!";
            Root.Q<Label>("home-level").text = "УРОВЕНЬ " + _profile.level;
            Root.Q<Label>("home-score").text = _profile.finLiteracyScore.ToString();
            Root.Q<Label>("home-lessons").text = LessonCatalog.CompletedCount(_profile.id) + "/" + LessonCatalog.All.Count;
            if (_dailyProgress != null)
                Root.Q<Label>("top-streak").text = "Серия " + _dailyProgress.currentStreak;
            RenderTopicButtons("home-topic-list", "hub-path-item", true);
        }

        private void RenderLessons()
        {
            RenderTopicButtons("lesson-topic-list", "hub-lesson-card", false);
        }

        private void RenderTopicButtons(string containerName, string className, bool compact)
        {
            var container = Root.Q<VisualElement>(containerName);
            container.Clear();
            for (var index = 0; index < LessonCatalog.All.Count; index++)
            {
                var lesson = LessonCatalog.All[index];
                var done = _profile != null && LessonCatalog.IsCompleted(_profile.id, lesson.Id);
                var captured = lesson;
                var button = new Button(() => OpenLesson(captured));
                button.AddToClassList(className);
                button.text = compact
                    ? (done ? "✓  " : "○  ") + (index + 1).ToString("00") + "   " + lesson.Title + "    →"
                    : lesson.Title + (done ? "   ·   Пройдено ✓" : "   ·   Читать →");
                container.Add(button);
            }
        }

        private void OpenLesson(LessonDefinition lesson)
        {
            Navigate(HubTab.Learn);
            _selectedLesson = lesson;
            _selectedPersonalLesson = null;
            Root.Q<VisualElement>("lesson-list").style.display = DisplayStyle.None;
            Root.Q<VisualElement>("lesson-detail").style.display = DisplayStyle.Flex;
            Root.Q<Label>("lesson-title").text = lesson.Title;
            Root.Q<Label>("lesson-summary").text = lesson.Summary;
            RenderCatalogLessonContent(lesson);
            ShowLessonStoryStep();
        }

        private void RenderCatalogLessonContent(LessonDefinition lesson)
        {
            Root.Q<VisualElement>("lesson-fallback-card").style.display = DisplayStyle.None;
            Root.Q<VisualElement>("lesson-ai-content").style.display = DisplayStyle.Flex;
            Root.Q<VisualElement>("lesson-character-card").style.display = DisplayStyle.None;
            Root.Q<Label>("lesson-story").text = lesson.Story;
            Root.Q<Label>("lesson-takeaway").text = lesson.Body;
            Root.Q<Label>("lesson-cheatsheet").text = "• " + lesson.Summary;
            Root.Q<VisualElement>("lesson-terms-group").style.display = DisplayStyle.None;
            Root.Q<VisualElement>("lesson-terms-list").Clear();
        }

        private void CloseLesson()
        {
            _selectedLesson = null;
            _selectedPersonalLesson = null;
            Root.Q<VisualElement>("lesson-list").style.display = DisplayStyle.Flex;
            Root.Q<VisualElement>("lesson-detail").style.display = DisplayStyle.None;
        }

        private async Task RefreshPersonalLessons()
        {
            if (_learningRefreshInFlight) return;
            _learningRefreshInFlight = true;
            int version = ++_learningRequestVersion;
            var status = Root.Q<Label>("learning-status");
            var list = Root.Q<VisualElement>("personal-lesson-list");
            list.Clear();
            status.text = "Загружаем твои уроки...";
            try
            {
                var lessonsTask = IvrApi.Learning.Lessons();
                var profileTask = IvrApi.Learning.Profile();
                await Task.WhenAll(lessonsTask, profileTask);
                if (version != _learningRequestVersion || !IvrApi.Auth.IsLoggedIn) return;
                var result = lessonsTask.Result;
                if (!result.Success) { status.text = "Нет связи. Нажми «Обновить», чтобы загрузить разборы."; return; }
                _hasPendingLessons = result.Data.Exists(item => item.status == "PENDING");
                status.text = result.Data.Count == 0 ? "Пройди квиз — здесь появится твой разбор и шпаргалка." : "";
                var knowledge = profileTask.Result;
                if (knowledge.Success)
                {
                    Root.Q<Button>("learning-diagnostic").style.display = knowledge.Data.diagnosticCompleted ? DisplayStyle.None : DisplayStyle.Flex;
                    var lines = new List<string>();
                    foreach (var topic in knowledge.Data.topics)
                        if (topic.total > 0) lines.Add(topic.title + ": " + topic.correct + "/" + topic.total + " последних ответов верно");
                    Root.Q<Label>("learning-knowledge").text = string.Join("\n", lines);
                }
                foreach (var lesson in result.Data)
                {
                    var captured = lesson;
                    var button = new Button(() => OpenPersonalLesson(captured)) {
                        text = lesson.title + (lesson.completed ? " · Закреплено ✓"
                            : lesson.status == "PENDING" ? " · Создаём историю…" : " · Читать →")
                    };
                    button.enableRichText = false;
                    button.AddToClassList("hub-lesson-card");
                    button.style.whiteSpace = WhiteSpace.Normal;
                    list.Add(button);
                    if (lesson.id == _pendingPersonalLessonId)
                    {
                        _pendingPersonalLessonId = null;
                        OpenPersonalLesson(lesson);
                    }
                    else if (_selectedPersonalLesson != null && lesson.id == _selectedPersonalLesson.id &&
                        lesson.status != _selectedPersonalLesson.status)
                        OpenPersonalLesson(lesson);
                }
            }
            catch (Exception)
            {
                if (version == _learningRequestVersion) status.text = "Не удалось загрузить уроки. Нажми «Обновить».";
            }
            finally
            {
                _learningRefreshInFlight = false;
            }
        }

        private void OpenPersonalLesson(PersonalLessonDto lesson)
        {
            _selectedLesson = null;
            _selectedPersonalLesson = lesson;
            Root.Q<VisualElement>("lesson-list").style.display = DisplayStyle.None;
            Root.Q<VisualElement>("lesson-detail").style.display = DisplayStyle.Flex;
            Root.Q<Label>("lesson-title").enableRichText = false;
            Root.Q<Label>("lesson-body").enableRichText = false;
            Root.Q<Label>("lesson-title").text = lesson.title;
            Root.Q<Label>("lesson-summary").text = lesson.completed ? "Ты закрепил тему. Можно повторить без повторной награды."
                : lesson.status == "PENDING" ? "Шпаргалка уже готова. Персональное объяснение дополняется — обнови список чуть позже."
                : "Твой разбор · примерно 3 минуты";
            RenderPersonalLessonContent(lesson);
            if (!string.IsNullOrWhiteSpace(lesson.story)) ShowLessonStoryStep();
            else ShowFallbackLessonStep();
            Root.Q<ScrollView>("hub-scroll").scrollOffset = Vector2.zero;
        }

        private void ShowLessonStoryStep()
        {
            Root.Q<VisualElement>("lesson-story-step").style.display = DisplayStyle.Flex;
            Root.Q<VisualElement>("lesson-learning-step").style.display = DisplayStyle.None;
            Root.Q<Button>("lesson-story-next").style.display = DisplayStyle.Flex;
            Root.Q<Button>("lesson-learning-back").style.display = DisplayStyle.None;
            Root.Q<Button>("lesson-quiz").style.display = DisplayStyle.None;
            Root.Q<Label>("lesson-progress").text = "Шаг 1 из 2 · История";
            Root.Q<ScrollView>("hub-scroll").scrollOffset = Vector2.zero;
        }

        private void ShowLessonLearningStep()
        {
            var hasPersonalStory = _selectedPersonalLesson != null &&
                !string.IsNullOrWhiteSpace(_selectedPersonalLesson.story);
            var hasCatalogStory = _selectedLesson != null && !string.IsNullOrWhiteSpace(_selectedLesson.Story);
            if (!hasPersonalStory && !hasCatalogStory) return;
            Root.Q<VisualElement>("lesson-story-step").style.display = DisplayStyle.None;
            Root.Q<VisualElement>("lesson-learning-step").style.display = DisplayStyle.Flex;
            Root.Q<Button>("lesson-story-next").style.display = DisplayStyle.None;
            Root.Q<Button>("lesson-learning-back").style.display = DisplayStyle.Flex;
            Root.Q<Button>("lesson-quiz").style.display = DisplayStyle.Flex;
            Root.Q<Label>("lesson-progress").text = "Шаг 2 из 2 · Разбор";
            Root.Q<ScrollView>("hub-scroll").scrollOffset = Vector2.zero;
        }

        private void ShowFallbackLessonStep()
        {
            Root.Q<Button>("lesson-story-next").style.display = DisplayStyle.None;
            Root.Q<Button>("lesson-learning-back").style.display = DisplayStyle.None;
            Root.Q<Button>("lesson-quiz").style.display = DisplayStyle.Flex;
            Root.Q<Label>("lesson-progress").text = "Материал и шпаргалка";
        }

        private void RenderPersonalLessonContent(PersonalLessonDto lesson)
        {
            var hasStory = !string.IsNullOrWhiteSpace(lesson.story);
            Root.Q<VisualElement>("lesson-ai-content").style.display = hasStory ? DisplayStyle.Flex : DisplayStyle.None;
            Root.Q<VisualElement>("lesson-fallback-card").style.display = hasStory ? DisplayStyle.None : DisplayStyle.Flex;
            if (!hasStory)
            {
                var rules = lesson.cheatSheet == null || lesson.cheatSheet.Count == 0
                    ? "Проверь основное правило темы" : string.Join("\n• ", lesson.cheatSheet);
                Root.Q<Label>("lesson-body").text = lesson.explanation + "\n\nПример\n" + lesson.example
                    + "\n\nЗапомни\n• " + rules;
                return;
            }

            var character = lesson.character;
            var characterCard = Root.Q<VisualElement>("lesson-character-card");
            characterCard.style.display = character == null || string.IsNullOrWhiteSpace(character.name)
                ? DisplayStyle.None : DisplayStyle.Flex;
            if (character != null)
            {
                Root.Q<Label>("lesson-character-name").text = character.name + ", " + character.age + " лет";
                Root.Q<Label>("lesson-character-personality").text = character.personality;
                Root.Q<Label>("lesson-character-avatar").text = string.IsNullOrEmpty(character.name)
                    ? "А" : character.name.Substring(0, 1).ToUpperInvariant();
            }
            Root.Q<Label>("lesson-story").text = lesson.story;
            Root.Q<Label>("lesson-takeaway").text = lesson.takeaway;
            Root.Q<Label>("lesson-cheatsheet").text = lesson.cheatSheet == null
                ? "" : "• " + string.Join("\n• ", lesson.cheatSheet);

            var termsGroup = Root.Q<VisualElement>("lesson-terms-group");
            var termsList = Root.Q<VisualElement>("lesson-terms-list");
            termsList.Clear();
            termsGroup.style.display = lesson.terms == null || lesson.terms.Count == 0
                ? DisplayStyle.None : DisplayStyle.Flex;
            if (lesson.terms == null) return;
            foreach (var term in lesson.terms)
            {
                var card = new VisualElement();
                card.AddToClassList("hub-term-card");
                var name = new Label(term.term ?? "Термин");
                name.AddToClassList("hub-term-name");
                name.enableRichText = false;
                var text = new Label(!string.IsNullOrWhiteSpace(term.text) ? term.text
                    : (term.definition ?? "") + (string.IsNullOrWhiteSpace(term.example) ? "" : " " + term.example));
                text.AddToClassList("hub-term-text");
                text.enableRichText = false;
                card.Add(name);
                card.Add(text);
                termsList.Add(card);
            }
        }

        private void SetQuestFilter(int filter)
        {
            _questFilter = filter;
            Root.Q<Button>("quest-all").EnableInClassList("hub-filter-active", filter == 0);
            Root.Q<Button>("quest-active").EnableInClassList("hub-filter-active", filter == 1);
            Root.Q<Button>("quest-done").EnableInClassList("hub-filter-active", filter == 2);
            RenderQuests();
        }

        private void RenderQuests()
        {
            var list = Root.Q<VisualElement>("quest-list");
            list.Clear();
            if (_dailyProgress == null)
            {
                Root.Q<Label>("quest-summary").text = "Загружаем задания...";
                Root.Q<Label>("quest-streak").text = "Серия входов загружается";
                return;
            }

            Root.Q<Label>("quest-summary").text = _dailyProgress.completedCount + " из " +
                _dailyProgress.totalCount + " заданий выполнено";
            Root.Q<Label>("quest-streak").text = "Серия входов: " + _dailyProgress.currentStreak +
                " · рекорд: " + _dailyProgress.bestStreak;

            foreach (var quest in _dailyProgress.quests)
            {
                Action action = quest.id == "COMPLETE_TEST"
                    ? () => GameManager.Instance.StartQuiz()
                    : quest.id == "PLAY_MINIGAME" ? () => Navigate(HubTab.MiniGames) : null;
                AddQuest(list, quest.title, quest.progress, quest.target,
                    quest.id == "COMPLETE_TEST" ? "Пройти тест" : "Играть",
                    quest.available, action);
            }

            if (list.childCount == 0)
            {
                var empty = new Label("Здесь пока нет заданий для этого фильтра.");
                empty.AddToClassList("hub-muted");
                list.Add(empty);
            }
        }

        private void RenderMiniGames()
        {
            var list = Root.Q<VisualElement>("minigame-list");
            list.Clear();
            foreach (var game in MiniGameCatalog.All)
            {
                var card = new VisualElement();
                card.AddToClassList("hub-minigame-card");

                var badge = new Label(game.Status);
                badge.AddToClassList(game.IsAvailable
                    ? "hub-minigame-badge-ready"
                    : "hub-minigame-badge-soon");
                var title = new Label(game.Title);
                title.AddToClassList("hub-minigame-title");
                var description = new Label(game.Description);
                description.AddToClassList("hub-minigame-description");
                var meta = new Label(game.Duration + " · результат попадёт в ежедневное задание");
                meta.AddToClassList("hub-minigame-meta");
                var button = new Button(() => StartMiniGame(game))
                {
                    text = game.IsAvailable ? "Играть →" : "Скоро"
                };
                button.AddToClassList("hub-minigame-button");
                button.SetEnabled(game.IsAvailable);

                card.Add(badge);
                card.Add(title);
                card.Add(description);
                card.Add(meta);
                card.Add(button);
                list.Add(card);
            }
        }

        private void StartMiniGame(MiniGameDefinition game)
        {
            if (!game.IsAvailable) return;
            if (game.Id == "office-clicker")
            {
                _officeClicks = 0;
                Root.Q<Button>("office-clicker-button").text = ClickerButtonText();
                Root.Q<VisualElement>("minigame-catalog").style.display = DisplayStyle.None;
                Root.Q<VisualElement>("office-clicker").style.display = DisplayStyle.Flex;
                Root.Q<ScrollView>("hub-scroll").scrollOffset = Vector2.zero;
                return;
            }
            GameManager.Instance.LoadGame(game.SceneName);
        }

        private void ShowMiniGameCatalog()
        {
            Root.Q<VisualElement>("minigame-catalog").style.display = DisplayStyle.Flex;
            Root.Q<VisualElement>("office-clicker").style.display = DisplayStyle.None;
        }

        private void OnOfficeClick()
        {
            _officeClicks++;
            Root.Q<Button>("office-clicker-button").text = ClickerButtonText();
            if (!_miniGameReported && !_miniGameReportInFlight)
                _ = ReportMiniGamePlayed();
        }

        private string ClickerButtonText() => "РАЗВИВАТЬ ОФИС\n\nКликов: " + _officeClicks;

        private async Task ReportMiniGamePlayed()
        {
            _miniGameReportInFlight = true;
            var result = await IvrApi.Daily.CompleteMiniGame();
            _miniGameReportInFlight = false;
            if (!result.Success) return;
            _dailyProgress = result.Data;
            _miniGameReported = _dailyProgress.quests != null &&
                _dailyProgress.quests.Exists(quest => quest.id == "PLAY_MINIGAME" && quest.completed);
            Root.Q<Label>("top-streak").text = "Серия " + _dailyProgress.currentStreak;
        }

        private void AddQuest(VisualElement list, string title, int progress, int target, string action,
            bool available, Action onClick)
        {
            bool done = progress >= target;
            if ((_questFilter == 1 && done) || (_questFilter == 2 && !done)) return;

            var card = new VisualElement();
            card.AddToClassList("hub-quest-card");
            if (done) card.AddToClassList("hub-quest-done");
            var heading = new Label(title);
            heading.AddToClassList("hub-quest-title");
            var boundedProgress = Math.Max(0, Math.Min(progress, target));
            var status = new Label(done ? "Готово ✓" : boundedProgress + "/" + target);
            status.AddToClassList("hub-quest-progress");
            var track = new VisualElement();
            track.AddToClassList("hub-progress-track");
            var fill = new VisualElement();
            fill.AddToClassList("hub-progress-fill");
            fill.style.width = Length.Percent(100f * boundedProgress / target);
            track.Add(fill);
            card.Add(heading);
            card.Add(status);
            card.Add(track);
            if (!done && available)
            {
                var button = new Button(onClick) { text = action + " →" };
                button.AddToClassList("hub-quest-action");
                card.Add(button);
            }
            else if (!done)
            {
                var unavailable = new Label(action);
                unavailable.AddToClassList("hub-muted");
                card.Add(unavailable);
            }
            list.Add(card);
        }

        private async Task RefreshDailyProgress()
        {
            var result = await IvrApi.Daily.GetProgress();
            if (ScreenManager.Instance.Current != AppScreen.Hub || !IvrApi.Auth.IsLoggedIn) return;
            if (!result.Success)
            {
                if (_activeTab == HubTab.Quests)
                {
                    _dailyProgress = null;
                    Root.Q<Label>("quest-summary").text = "Не удалось загрузить задания";
                    Root.Q<Label>("quest-streak").text = "Проверь соединение с сервером";
                    Root.Q<VisualElement>("quest-list").Clear();
                }
                return;
            }

            _dailyProgress = result.Data;
            _miniGameReported = _dailyProgress.quests != null &&
                _dailyProgress.quests.Exists(quest => quest.id == "PLAY_MINIGAME" && quest.completed);
            Root.Q<Label>("top-streak").text = "Серия " + _dailyProgress.currentStreak;
            if (_activeTab == HubTab.Quests) RenderQuests();
        }

        private async Task RefreshRating()
        {
            var list = Root.Q<VisualElement>("rating-list");
            var podium = Root.Q<VisualElement>("rating-podium");
            var status = Root.Q<Label>("rating-status");
            status.text = "Загружаем рейтинг...";
            list.Clear();
            podium.Clear();
            var result = await IvrApi.Market.GetLeaderboard();
            if (!result.Success)
            {
                status.text = "Не удалось загрузить рейтинг. Нажми «Обновить».";
                return;
            }
            var entries = result.Data;
            status.text = entries.Count == 0 ? "Пока нет игроков в рейтинге." : "";

            if (entries.Count > 1) AddPodium(podium, entries[1]);
            if (entries.Count > 0) AddPodium(podium, entries[0]);
            if (entries.Count > 2) AddPodium(podium, entries[2]);

            foreach (var entry in entries)
            {
                var row = new VisualElement();
                row.AddToClassList("hub-ranking-row");
                if (_profile != null && entry.playerId == _profile.id) row.AddToClassList("hub-ranking-self");
                var rank = new Label(entry.rank.ToString());
                rank.AddToClassList("hub-ranking-rank");
                var name = new Label(_profile != null && entry.playerId == _profile.id ? "Ты · " + entry.nickname : entry.nickname);
                name.AddToClassList("hub-ranking-name");
                var value = new Label(entry.totalValue.ToString("F0") + " ₸");
                value.AddToClassList("hub-ranking-score");
                row.Add(rank);
                row.Add(name);
                row.Add(value);
                list.Add(row);
            }
        }

        private static void AddPodium(VisualElement podium, LeaderboardEntryDto entry)
        {
            var card = new VisualElement();
            card.AddToClassList("hub-podium-item");
            if (entry.rank == 1) card.AddToClassList("hub-podium-first");
            if (entry.rank == 2) card.AddToClassList("hub-podium-second");
            var rank = new Label("#" + entry.rank);
            rank.AddToClassList("hub-podium-rank");
            var name = new Label(entry.nickname);
            name.AddToClassList("hub-podium-name");
            var score = new Label(entry.totalValue.ToString("F0") + " ₸");
            score.AddToClassList("hub-podium-score");
            card.Add(rank);
            card.Add(name);
            card.Add(score);
            podium.Add(card);
        }

        private async Task RefreshPortfolio()
        {
            var label = Root.Q<Label>("profile-portfolio");
            var result = await IvrApi.Market.GetPortfolio();
            label.text = result.Success
                ? result.Data.totalValue.ToString("F0") + " ₸"
                : "Не удалось загрузить портфель";
        }

        private void CloseResult()
        {
            Root.Q<VisualElement>("hub-result-overlay").style.display = DisplayStyle.None;
            _pendingPersonalLessonId = GameManager.Instance.LastLearningLessonId;
            GameManager.Instance.ClearLastResult();
            _ = RefreshProfile();
            _ = RefreshDailyProgress();
            if (!string.IsNullOrEmpty(_pendingPersonalLessonId)) Navigate(HubTab.Learn);
        }

        private void Logout()
        {
            IvrApi.Auth.Logout();
            _learningRequestVersion++;
            _pendingPersonalLessonId = null;
            _profile = null;
            _dailyProgress = null;
            _officeClicks = 0;
            _miniGameReported = false;
            _miniGameReportInFlight = false;
            GameManager.Instance.SetProfile(null);
            GameManager.Instance.ClearLastResult();
            ScreenManager.Instance.Show(AppScreen.Welcome);
        }
    }
}
