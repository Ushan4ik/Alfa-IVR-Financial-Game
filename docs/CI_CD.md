# CI/CD проекта

## Что запускается автоматически

Workflow `.github/workflows/ci-cd.yml` работает при pull request, push в `main`, теге `v*` и ручном запуске.

1. Запускает тесты FinStory на Python 3.12.
2. Устанавливает Java 21 и запускает все тесты Spring на H2.
3. Сохраняет HTML-отчёт Spring-тестов на 14 дней.
4. Собирает production Docker-образы backend и FinStory.
5. После push в `main` или тега публикует образы в GitHub Container Registry:
   `ghcr.io/ushan4ik/alfa-ivr-financial-game-backend` и
   `ghcr.io/ushan4ik/alfa-ivr-financial-game-finstory`.

Для pull request образ только собирается и не публикуется. Поэтому код из внешнего PR не получает право записи в registry.

## Production-конфигурация backend

Контейнер запускается с профилем `prod` от непривилегированного пользователя. Обязательные переменные среды:

| Переменная | Назначение |
| --- | --- |
| `DB_PASSWORD` | пароль PostgreSQL |
| `JWT_SECRET` | случайная строка длиной от 32 байт для подписи JWT |
| `CORS_ALLOWED_ORIGINS` | адрес опубликованной игры, например `https://game.example.ru` |

Дополнительно используются `DATABASE_URL`, `DB_USER`, `IVR_ADMIN_EMAILS`, `FINSTORY_URL`,
`FINSTORY_INTERNAL_TOKEN`, `ML_SERVICE_URL`, `GEMINI_API_KEY` и `GEMINI_MODEL`.

Yandex-секреты передаются только контейнеру FinStory: `YANDEX_API_KEY`, `YANDEX_FOLDER_ID`
и `FINSTORY_YANDEX_MODEL`. Они не должны попадать в Unity, Spring-ответы или Docker-образ.

Пример запуска опубликованного образа:

```bash
docker run --rm -p 8080:8080 \
  -e DATABASE_URL=jdbc:postgresql://host.docker.internal:5432/ivr \
  -e DB_USER=ivr \
  -e DB_PASSWORD=change-me \
  -e JWT_SECRET=replace-with-a-long-random-secret \
  -e CORS_ALLOWED_ORIGINS=https://game.example.ru \
  ghcr.io/ushan4ik/alfa-ivr-financial-game-backend:main
```

## Unity WebGL

WebGL собирается отдельным ручным workflow `.github/workflows/unity-webgl.yml`, потому что Unity требует лицензию. В настройках GitHub `Settings → Secrets and variables → Actions` нужно добавить:

- `UNITY_LICENSE` — содержимое файла лицензии Unity;
- `UNITY_EMAIL` и `UNITY_PASSWORD` — если выбранный тип лицензии требует авторизацию.

После этого в `Actions → Unity WebGL build → Run workflow` создаётся артефакт `unity-webgl`, доступный 14 дней. Локальная сборка по-прежнему выполняется командой `Unity_WEB_A_IVR/build-web.sh`.

## Граница текущего CD

Pipeline выполняет continuous delivery: публикует готовый серверный образ и WebGL-артефакт. Автоматическое развёртывание на конкретный домен добавляется после выбора хостинга и выдачи его deploy-token. Секреты приложения не хранятся в репозитории и не попадают в Docker-образ.
