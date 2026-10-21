# Диаграммы последовательностей

## Регистрация и переход к онбордингу

```mermaid
sequenceDiagram
    actor User as Пользователь
    participant UI as RegisterScreen
    participant Auth as AuthService
    participant API as Spring AuthController
    participant DB as PostgreSQL

    User->>UI: Никнейм, email, пароль
    UI->>UI: Проверка формы
    UI->>Auth: Register(request)
    Auth->>API: POST /auth/register
    API->>DB: Проверить уникальность email
    API->>API: BCrypt(password), создать JWT
    API->>DB: INSERT player
    DB-->>API: Player
    API-->>Auth: token, playerId
    Auth-->>UI: Успех
    UI->>UI: Сохранить JWT, открыть onboarding
```

## Диагностический квиз и персональный урок

```mermaid
sequenceDiagram
    actor User as Игрок
    participant Unity as QuizManager
    participant API as LearningController
    participant Service as LearningService
    participant DB as PostgreSQL
    participant Worker as LessonGenerator
    participant Gemini as Gemini API

    Unity->>API: POST /learning/attempts diagnostic=true
    API->>Service: start(playerId, request)
    Service->>DB: Сохранить попытку и снимок вопросов
    API-->>Unity: attemptId, вопросы без ответов
    loop Каждый вопрос
        User->>Unity: Выбрать вариант
        Unity->>API: POST /attempts/{id}/answers
        API->>Service: answer(playerId, attemptId, answer)
        Service->>DB: Зафиксировать выбранный вариант
        API-->>Unity: correct, correctIndex, explanation
    end
    Unity->>API: POST /attempts/{id}/finish
    API->>Service: finish(playerId, attemptId)
    Service->>DB: Очки, результат, PersonalLesson
    API-->>Unity: score, awardedScore, lessonId
    opt Gemini настроен
        Worker->>DB: Получить PENDING lesson
        Worker->>Gemini: Проверенный контекст ошибки
        Gemini-->>Worker: Структурированное объяснение
        Worker->>DB: READY либо FALLBACK
    end
```

## Повторный квиз

```mermaid
sequenceDiagram
    actor User as Игрок
    participant Hub as HubScreen
    participant API as LearningController
    participant Service as LearningService
    participant DB as PostgreSQL

    Hub->>API: GET /learning/lessons
    API->>DB: Уроки игрока
    API-->>Hub: title, explanation, cheatSheet, completed
    User->>Hub: Начать проверку
    Hub->>API: POST /learning/attempts lessonId
    API->>Service: Подобрать вопросы слабой темы
    Service->>DB: Создать дочернюю попытку
    API-->>Hub: Новые вопросы
    Note over Hub,API: Ответы отправляются как в обычном квизе
    Hub->>API: POST /attempts/{id}/finish
    API->>DB: Сохранить результат
    alt Результат не ниже 50%
        API->>DB: completed = true
    end
    API-->>Hub: Итог повторения
```

## Биржевая операция

```mermaid
sequenceDiagram
    actor User as Игрок
    participant UI as MarketPanel
    participant API as MarketController
    participant Service as MarketService
    participant DB as PostgreSQL

    User->>UI: Выбрать акцию, операцию, количество
    UI->>API: POST /market/order + JWT
    API->>Service: placeOrder(playerId, request)
    Service->>DB: Заблокировать и прочитать игрока/позицию
    Service->>Service: Проверить деньги, количество и тип
    Service->>DB: Обновить cash и holding
    Service->>DB: INSERT MarketOrder
    Service-->>API: PortfolioResponse
    API-->>UI: Новый баланс и позиции
```
