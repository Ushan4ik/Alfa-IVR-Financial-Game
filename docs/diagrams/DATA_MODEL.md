# Модель данных

## ER-диаграмма

Связи заданы идентификаторами, хотя в текущих JPA-сущностях не все оформлены внешними ключами на уровне аннотаций.

```mermaid
erDiagram
    PLAYER ||--|| PLAYER_PROGRESS : has
    PLAYER ||--o{ LEARNING_ATTEMPT : completes
    PLAYER ||--o{ PERSONAL_LESSON : receives
    LEARNING_ATTEMPT ||--o| PERSONAL_LESSON : creates
    PERSONAL_LESSON ||--o{ LEARNING_ATTEMPT : repeated_by
    PLAYER ||--o{ HOLDING : owns
    PLAYER ||--o{ MARKET_ORDER : places
    STOCK ||--o{ HOLDING : held_as
    STOCK ||--o{ MARKET_ORDER : traded_in
    STOCK ||--o{ PRICE_POINT : has_history

    PLAYER {
        string id PK
        string email UK
        string password_hash
        string nickname
        string interests
        double cash
        string role
        timestamp created_at
    }
    PLAYER_PROGRESS {
        string player_id PK
        int fin_literacy_score
        int level
        timestamp last_updated
    }
    LEARNING_ATTEMPT {
        string id PK
        string player_id
        boolean diagnostic
        string parent_lesson_id
        text questions_json
        boolean finished
        int awarded_score
        string lesson_id
        timestamp created_at
    }
    PERSONAL_LESSON {
        string id PK
        string player_id
        string attempt_id UK
        string topic_id
        string status
        boolean completed
        text content_json
        timestamp created_at
    }
    STOCK {
        string id PK
        string ticker
        string name
        string sector
        double current_price
        double previous_price
    }
    HOLDING {
        string id PK
        string player_id
        string stock_id
        double quantity
        double avg_price
    }
    MARKET_ORDER {
        string id PK
        string player_id
        string stock_id
        string type
        double quantity
        double price
        int day
    }
    PRICE_POINT {
        string id PK
        string stock_id
        int day
        double price
    }
```

Дополнительные независимые сущности: `NEWS_EVENT` хранит новость игрового дня и влияние на сектор; `MARKET_STATE` — единственную строку с текущим днём и интервалом; `QUESTION` — старый административный каталог вопросов.

## Объектная диаграмма примера

Диаграмма показывает возможный снимок объектов после диагностического квиза и покупки акции. Значения демонстрационные.

```mermaid
flowchart LR
    P["player: Player<br/>id=p1<br/>nickname=Alex<br/>role=PLAYER<br/>cash=9200"]
    PP["progress: PlayerProgress<br/>playerId=p1<br/>score=30<br/>level=1"]
    A["attempt: LearningAttempt<br/>id=a1<br/>diagnostic=true<br/>finished=true<br/>awardedScore=30"]
    L["lesson: PersonalLesson<br/>id=l1<br/>attemptId=a1<br/>topicId=credits<br/>status=FALLBACK<br/>completed=false"]
    S["stock: Stock<br/>id=s1<br/>ticker=TECH<br/>currentPrice=100"]
    H["holding: Holding<br/>playerId=p1<br/>stockId=s1<br/>quantity=8<br/>avgPrice=100"]
    O["order: MarketOrder<br/>playerId=p1<br/>stockId=s1<br/>type=BUY<br/>quantity=8"]

    P -->|has| PP
    P -->|completed| A
    A -->|created| L
    P -->|owns| H
    H -->|references| S
    P -->|placed| O
    O -->|references| S
```
