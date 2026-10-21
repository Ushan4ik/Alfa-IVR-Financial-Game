# Spring API и PostgreSQL

Сервер банковской игры: Spring Boot 4.1.0, Kotlin 2.3.21, Java 21, Spring Security/JWT, JPA и PostgreSQL 16. [Общая архитектура](../ARCHITECTURE.md), [персональное обучение](PERSONAL_LEARNING.md).

## Локальный запуск

Нужны Docker и JDK 21. Выполняйте команды из этого каталога:

```bash
git clone https://github.com/ushan4ik/Alfa-IVR-Financial-Game.git
cd Alfa-IVR-Financial-Game/Kotlin_spring_project
```

Скопируйте `.env.example` в `.env` и задайте локальный пароль БД. Перед развёртыванием задайте случайный `JWT_SECRET` длиной не менее 32 символов. Секреты не коммитьте. Базовый `application.properties` содержит только безопасные значения для разработки, а `application.properties.example` показывает пример конфигурации внешней PostgreSQL.

```bash
set -a
source .env
set +a
docker compose up -d --wait db
./gradlew bootRun --args='--spring.profiles.active=local'
```

`compose.yaml` создаёт `ivr-postgres` на `127.0.0.1:5432`. Профиль `local` использует БД `ivr`, пользователя `ivr`, пароль из `IVR_DB_PASSWORD`, миграции Flyway и проверку схемы Hibernate. API обычно доступен по `http://localhost:8080`. При запуске из `~` команды `source .env` и `./gradlew` не найдут файлы.

```bash
./gradlew test
docker compose ps
```

Тесты Spring работают на H2. Для остановки сервера нажмите Ctrl+C, для остановки БД выполните `docker compose down`. Данные сохраняются в Docker volume.

### Ошибка `password authentication failed for user "ivr"`

Пароль PostgreSQL записывается в Docker volume при первом создании. Если позднее изменить `IVR_DB_PASSWORD` в `.env`, контейнер продолжит использовать старый пароль. Данные удалять не требуется: при работающем `ivr-postgres` синхронизируйте роль с текущим `.env`:

```bash
set -a
source .env
set +a
python3 - <<'PY' | docker exec -i ivr-postgres psql -U ivr -d ivr
import os
password = os.environ["IVR_DB_PASSWORD"].replace("'", "''")
print(f"ALTER ROLE ivr WITH PASSWORD '{password}';")
PY
```

После сообщения `ALTER ROLE` повторите `bootRun`.

## Конфигурация

| Параметр | Назначение |
| --- | --- |
| `IVR_DB_PASSWORD` | Пароль локальной БД |
| `IVR_ADMIN_EMAILS` | Уже зарегистрированные аккаунты, которым при запуске назначается роль ADMIN |
| `jwt.secret` | Подпись JWT, минимум 32 символа |
| `ml.service.url` | Адрес классификатора интересов; при недоступности возвращается `другое` |
| `GEMINI_API_KEY`, `GEMINI_MODEL` | Необязательная генерация текста уроков; без них действует fallback |
| `learning.generation-delay-ms` | Интервал обработки уроков `PENDING`, по умолчанию 5000 мс |

`application-dev.properties` содержит `create-drop` и отключает Flyway для быстрых H2-тестов. Профиль `local` применяет SQL из `src/main/resources/db/migration`, затем запускает Hibernate с `ddl-auto=validate`. Существующая схема автоматически принимается как baseline версии 1; миграция V2 добавляет роли. Новая пустая БД проходит V1 и V2 с нуля.

## API

Все пути, кроме `/auth/**`, требуют `Authorization: Bearer <JWT>`. Запросы и ответы — JSON. Тела операций заданы DTO соответствующих пакетов; примеры обучения есть в [PERSONAL_LEARNING.md](PERSONAL_LEARNING.md).

| Метод и путь | Назначение |
| --- | --- |
| `POST /auth/register`, `POST /auth/login` | Создание/вход игрока и JWT |
| `GET /player/profile` | Профиль, уровень, очки, интересы |
| `POST /player/interests` | Классификация интересов с fallback |
| `POST /player/progress` | Устаревший путь: всегда 409 |
| `POST /learning/attempts` | Начать вводный, обычный или повторный квиз |
| `POST /learning/attempts/{id}/answers` | Зафиксировать и проверить ответ |
| `POST /learning/attempts/{id}/finish` | Результат, очки и персональный урок |
| `GET /learning/lessons`, `GET /learning/profile` | Уроки и прогресс по темам |
| `GET /market/state`, `/stocks`, `/stocks/{id}/history`, `/news` | Состояние учебного рынка |
| `GET /market/leaderboard`, `/portfolio` | Рейтинг и портфель |
| `POST /market/order` | BUY, SELL, SHORT или COVER |
| `GET /questions`, `/questions/category/{category}`, `/questions/lesson/{lessonId}` | Старый каталог вопросов |
| `POST /questions`, `/questions/bulk`, `DELETE /questions/category/{category}` | Управление старым каталогом |
| `GET /admin/players`, `PUT /admin/players/{id}` | Просмотр/изменение игроков |
| `GET /admin/market/state`, `PUT /admin/market/interval`, `POST /admin/market/skip` | Управление временем симуляции |

Сервер берёт идентификатор игрока из JWT и проверяет владение его попыткой/уроком. Каталоги `/questions` и `/learning` различны: индивидуальные уроки используют проверенные вопросы `LearningCatalog`.

`/admin/**`, создание/массовая загрузка и удаление `/questions` требуют роли ADMIN. Остальные игровые запросы требуют обычной аутентификации. Чтобы назначить первого администратора, сначала зарегистрируйте аккаунт, укажите его email в `IVR_ADMIN_EMAILS` и перезапустите Spring. Публичного endpoint повышения роли нет. CORS пока разрешает все origin без cookies.

## Где менять логику

`auth` — регистрация, BCrypt, JWT; `player` — профиль и интересы; `learning` — каталог, попытки, очки, уроки и worker Gemini; `market` — цены, новости, портфели, заявки и scheduler; `quiz` — старый каталог; `ml` — клиент внешнего классификатора. Правила размещены в сервисах, JPA-сущности и репозитории — рядом с пакетами домена.

Панель `src/main/resources/static/admin.html` есть для разработки. Её HTML доступен как оболочка, но данные и изменения сервер разрешает только JWT пользователя с ролью ADMIN.
