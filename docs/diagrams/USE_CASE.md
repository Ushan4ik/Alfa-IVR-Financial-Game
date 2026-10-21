# Use Case: роли и функции

```mermaid
flowchart LR
    Guest[Гость]
    Player[Игрок]
    Admin[Администратор]
    Worker[Планировщик сервера]

    subgraph Product[Банковская обучающая игра]
        Register((Регистрация))
        Login((Вход))
        Onboarding((Онбординг))
        Diagnostic((Диагностика знаний))
        Learn((Изучение уроков))
        Quiz((Тематический квиз))
        Review((Персональный разбор))
        Retry((Повтор слабой темы))
        Profile((Профиль и прогресс))
        Market((Учебная биржа))
        Rating((Рейтинг))
        ManagePlayers((Управление игроками))
        ManageMarket((Управление рынком))
        GenerateLesson((Дополнение текста урока))
        MarketTick((Новый рыночный день))
    end

    Guest --> Register
    Guest --> Login
    Register --> Onboarding
    Onboarding --> Diagnostic

    Player --> Login
    Player --> Learn
    Player --> Quiz
    Player --> Review
    Review --> Retry
    Player --> Profile
    Player --> Market
    Player --> Rating

    Admin --> ManagePlayers
    Admin --> ManageMarket
    Worker --> GenerateLesson
    Worker --> MarketTick
```

Администратор является зарегистрированным игроком с дополнительной ролью `ADMIN`. Gemini не является актором, принимающим решения: правильные ответы и слабая тема рассчитываются сервером, а внешняя модель может только дополнить текст уже созданного урока.
