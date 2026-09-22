using UnityEngine.UIElements;

namespace IVR.UI
{
    // Базовый класс для всех экранов.
    // Каждый экран знает как показать/скрыть себя и держит ссылку на свой корневой элемент.
    public abstract class BaseScreen
    {
        protected VisualElement Root { get; private set; }

        protected BaseScreen(VisualTreeAsset asset, VisualElement container)
        {
            Root = asset.Instantiate();
            Root.style.display = DisplayStyle.None;
            Root.style.width = Length.Percent(100);
            Root.style.height = Length.Percent(100);
            Root.style.flexGrow = 1;
            container.Add(Root);
        }

        protected abstract void RegisterCallbacks();

        // Вызывается после конструктора производного экрана: его поля уже инициализированы.
        public void InitializeCallbacks() => RegisterCallbacks();

        public void Show() => Root.style.display = DisplayStyle.Flex;
        public void Hide() => Root.style.display = DisplayStyle.None;

        protected void SetStatus(string name, string message, bool isError = false)
        {
            var label = Root.Q<Label>(name);
            if (label == null) return;
            label.text = message;
            label.EnableInClassList("status-error", isError);
        }
    }
}
