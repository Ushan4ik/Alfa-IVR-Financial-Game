using System.Collections.Generic;
using System.Threading.Tasks;
using IVR.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace IVR.Game
{
    public class LeaderboardUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private static readonly string[] Medals = { "🥇", "🥈", "🥉" };

        private void Start()
        {
            if (uiDocument == null) { Debug.LogError("UIDocument не назначен!"); return; }

            var root = uiDocument.rootVisualElement;
            root.Q<Button>("exit-btn").clicked += () => GameManager.Instance.BackToMain();

            _ = Load(root.Q<ScrollView>("leaderboard-list"));
        }

        private async Task Load(ScrollView list)
        {
            var result = await IvrApi.Market.GetLeaderboard();
            if (!result.Success)
            {
                var err = new Label($"Ошибка загрузки: {result.Error}");
                err.AddToClassList("subtitle");
                list.Add(err);
                return;
            }
            BuildRows(list, result.Data);
        }

        private static void BuildRows(ScrollView list, List<LeaderboardEntryDto> entries)
        {
            list.Clear();
            foreach (var entry in entries)
            {
                var row = new VisualElement();
                row.AddToClassList("profile-row");

                var rankLabel = new Label(entry.rank <= 3 ? Medals[entry.rank - 1] : entry.rank.ToString());
                rankLabel.AddToClassList("profile-value");
                rankLabel.style.width = 48;

                var nameLabel = new Label(entry.nickname);
                nameLabel.AddToClassList("profile-value");
                nameLabel.style.flexGrow = 1;

                var cashLabel = new Label($"{entry.cash:F0} ₸");
                cashLabel.AddToClassList("profile-label");
                cashLabel.style.width = 130;
                cashLabel.style.unityTextAlign = TextAnchor.MiddleRight;

                var holdingsLabel = new Label($"{entry.holdingsValue:F0} ₸");
                holdingsLabel.AddToClassList("profile-label");
                holdingsLabel.style.width = 130;
                holdingsLabel.style.unityTextAlign = TextAnchor.MiddleRight;

                var totalLabel = new Label($"{entry.totalValue:F0} ₸");
                totalLabel.AddToClassList("profile-value");
                totalLabel.style.width = 140;
                totalLabel.style.unityTextAlign = TextAnchor.MiddleRight;

                if (entry.rank == 1)
                    totalLabel.style.color = new StyleColor(new Color(1f, 0.85f, 0.2f));
                else if (entry.rank == 2)
                    totalLabel.style.color = new StyleColor(new Color(0.75f, 0.75f, 0.78f));
                else if (entry.rank == 3)
                    totalLabel.style.color = new StyleColor(new Color(0.8f, 0.55f, 0.3f));

                row.Add(rankLabel);
                row.Add(nameLabel);
                row.Add(cashLabel);
                row.Add(holdingsLabel);
                row.Add(totalLabel);

                list.Add(row);
            }
        }
    }
}
