# Unity-клиент

Unity 6000.4.1f1. Игра использует UI Toolkit (UXML/USS), сцену `Main` для приложения и сцену `Quiz` для квиза. Серверное API и данные описаны в [архитектуре](../ARCHITECTURE.md) и [Spring README](../Kotlin_spring_project/README.md).

Подробное назначение и связи каждого C#-файла: [UNITY_SCRIPTS.md](UNITY_SCRIPTS.md).

## Запуск в Unity Editor

Откройте `/home/ushan/IVR_PROJECT/Unity_WEB_A_IVR` через Unity Hub. Запустите `Assets/Scenes/Main.unity` и нажмите Play. В Build Settings включены `Main` и `Quiz`. `AppEntry` в `Main` задаёт адрес сервера `http://localhost:8080`; сервер и PostgreSQL запускаются по [инструкции Spring](../Kotlin_spring_project/README.md).

Если сцена показывает лишь фон, проверьте ссылки `UIDocument` и UXML на `ScreenManager` и ошибки в Console. Hub UXML дополнительно загружается из `Assets/Resources/UI/HubScreen.uxml`.

## Экраны и навигация

`Assets/Scripts/UI/ScreenManager.cs` переключает Welcome, Login, Register, Onboarding и Hub в одной сцене. `AppEntry.cs` восстанавливает сессию по JWT и профилю; при недоступном/невалидном токене показывает Welcome. Обработчики находятся в `Assets/Scripts/UI/Screens/`. UXML размещены в `Assets/UI/Screens/` и `Assets/Resources/UI/`, стили — в `Assets/UI/Styles/`. Главное меню `HubScreen` содержит Home, Learn, Quests, Rating, Profile и Market. `GameManager` загружает сцену `Quiz`, переносит параметры выбранного урока и результат обратно в `Main`.

Новый экран добавляется как UXML/USS + класс на базе `BaseScreen`, затем включается в `AppScreen` и `ScreenManager`, а VisualTreeAsset привязывается в сцене `Main`. Для новой вкладки Hub измените `HubTab`, UXML и обработчики `HubScreen`. UI вызывает сервис и показывает результат; начисление очков остаётся на сервере.

## Сетевой слой и квиз

`Assets/Scripts/Api/ApiClient.cs` использует `UnityWebRequest`, `JsonUtility`, 20-секундный timeout и JWT из `PlayerPrefs`. Обёртки `Services/` отделяют экран от HTTP, DTO в `Models/` описывают JSON. После регистрации открывается онбординг: он отправляет интересы и запускает диагностический квиз. `QuizManager` создаёт попытку через `/learning/attempts`, фиксирует ответы и завершает её через `/finish`; сервер начисляет очки и создаёт персональный урок. Если сервер недоступен, квиз переключается на локальную **тренировку без сохранения**. Hub загружает уроки и прогресс через `/learning/lessons` и `/learning/profile`.

Старые классы UI и API `/questions` ещё присутствуют. Для персонального обучения используйте цепочку `/learning`; локальный каталог нужен для offline-тренировки.

## Проверка прямо в браузере

WebGL можно запускать локально во время разработки: в Unity Hub установите **WebGL Build Support** для Unity 6000.4.1f1, затем в Unity выберите Web в Build Profiles, создайте сборку и обслуживайте каталог сборки через HTTP-сервер. Например, из каталога, где лежит `index.html`:

```bash
python3 -m http.server 8008
```

Откройте `http://localhost:8008`. Не запускайте `index.html` через `file://`. После изменений Unity-кода или ассетов браузерную сборку нужно пересобрать; для частых итераций быстрее Play в Editor. На этой машине модуль WebGL Build Support не обнаружен в проверенном пути Unity Hub, поэтому фактическая WebGL-сборка здесь ещё не подтверждена.

Адрес API в `AppEntry` по умолчанию — `localhost:8080`. Для браузера на той же машине это подходит. Для теста с другого устройства измените адрес на доступный ему сервер; для размещённой HTTPS-страницы API тоже должен быть доступен по HTTPS. Текущая CORS-настройка Spring разрешает браузерные запросы без cookie.
