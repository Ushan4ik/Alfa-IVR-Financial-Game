# IVR Project — банковская игра для молодёжи

[![CI and delivery](https://github.com/Ushan4ik/Alfa-IVR-Financial-Game/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/Ushan4ik/Alfa-IVR-Financial-Game/actions/workflows/ci-cd.yml)

Проект состоит из Unity-клиента и Spring Boot API с PostgreSQL. Игрок регистрируется, проходит вводный квиз, изучает финансовые темы, получает персональный разбор и может играть на учебном рынке акций.

## Документация

- [Архитектура и потоки данных](ARCHITECTURE.md) — компоненты, ответственность, БД, безопасность и границы текущей реализации.
- [Состояние и roadmap](IMPLEMENTATION_STATUS.md) — что реализовано, что масштабируется, чего не хватает и что оптимизировать.
- [Реализация этапа 1](STAGE_1_IMPLEMENTATION.md) — миграции, роли, защита админки и единая точка запуска Unity.
- [Экраны из макетов](SCREENS_STATUS.md) — соответствие каждого экрана дизайнера текущей реализации.
- [Unity-клиент](Unity_WEB_A_IVR/README.md) — сцены, UI Toolkit, навигация, сетевой слой и WebGL.
- [Все Unity-скрипты](Unity_WEB_A_IVR/UNITY_SCRIPTS.md) — разбор каждого C#-файла, контроллеров и связей между ними.
- [Spring API](Kotlin_spring_project/README.md) — локальный запуск, настройки, API и тесты.
- [Персональное обучение](Kotlin_spring_project/PERSONAL_LEARNING.md) — квиз, генерация разбора и повторная попытка.
- [Пользовательские сценарии и UML](docs/README.md) — сценарии, Use Case, классы, последовательности, ER- и объектная диаграммы.
- [Техническое задание](docs/TECHNICAL_SPECIFICATION.md) — границы продукта и проверяемые требования.
- [CI/CD](docs/CI_CD.md) — автоматические тесты, Docker-образ backend и ручная WebGL-сборка.
- [Проверка полного маршрута](docs/ACCEPTANCE_TEST.md) — ручная приёмка перед исправлением ошибок.

## Быстрый запуск локально

Нужны Docker, Java 21 и Unity 6000.4.6f1. Команды выполняются **из каталога Spring-проекта**, а не из домашней папки:

```bash
git clone https://github.com/ushan4ik/Alfa-IVR-Financial-Game.git
cd Alfa-IVR-Financial-Game/Kotlin_spring_project
cp .env.example .env
set -a
source .env
set +a
docker compose up -d --wait db
./gradlew bootRun --args='--spring.profiles.active=local'
```

Если `.env` или `src/main/resources/application.properties` отсутствует, подготовьте их по [документации сервера](Kotlin_spring_project/README.md). PostgreSQL доступен только на `127.0.0.1:5432`, API — по `http://localhost:8080`.

Затем откройте `Unity_WEB_A_IVR` в Unity Hub и запустите сцену `Assets/Scenes/Main.unity`. Для браузера нужна WebGL-сборка; порядок описан в [документации Unity](Unity_WEB_A_IVR/README.md).
