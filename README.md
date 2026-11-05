# Alfa-IVR — финансовая игра для молодёжи

[![CI and delivery](https://github.com/Ushan4ik/Alfa-IVR-Financial-Game/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/Ushan4ik/Alfa-IVR-Financial-Game/actions/workflows/ci-cd.yml)

**Автор:** Александр Ильин, 11И2  
**Формат:** браузерная игра Unity WebGL с сервером Kotlin/Spring Boot и PostgreSQL  
**Целевая аудитория:** пользователи 14–22 лет

Проект объединяет короткие уроки по финансовой грамотности, серверные тесты, персональные разборы ошибок, ежедневные задания, игровую экономику и учебную биржу. Игровые деньги, коины и операции на бирже являются виртуальными.

## Реализованные функции

- регистрация, вход и восстановление сессии;
- онбординг и сохранение интересов;
- диагностика по 11 финансовым темам;
- базовые уроки, тематические тесты и объяснения ответов;
- персональный разбор слабой темы с проверенным fallback-материалом;
- ежедневная серия, два задания, коины и награды за 7/14 дней;
- обмен одного коина на 1000 виртуальных тенге учебной биржи;
- учебный рынок с BUY, SELL, SHORT и COVER;
- рейтинг по стоимости виртуального портфеля;
- профиль и административная панель;
- мини-игра «Офис мечты»; остальные карточки явно отмечены как запланированные.

## Архитектура

| Компонент | Технологии | Назначение |
| --- | --- | --- |
| Клиент | Unity 6.4, C#, UI Toolkit, WebGL | Интерфейс, навигация и отображение данных |
| API | Kotlin, Spring Boot 4, Java 21 | Авторизация, обучение, прогресс, рынок и администрирование |
| База данных | PostgreSQL 16, Flyway | Пользователи и долговременное серверное состояние |
| FinStory | Python | Классификация интересов и генерация историй с безопасным fallback |

Unity не начисляет серверные деньги, коины, награды, роль или учебные очки самостоятельно. Эти значения проверяет и сохраняет Spring API.

## Быстрый запуск на Windows

Требуются Docker Desktop, JDK 21 и Unity `6000.4.6f1`.

1. Запустите Docker Desktop.
2. Откройте PowerShell в каталоге `Kotlin_spring_project`.
3. Если файла `.env` нет, создайте его из примера и замените локальный пароль:

```powershell
Copy-Item .env.example .env
```

4. Запустите PostgreSQL и FinStory:

```powershell
docker compose up -d --build --wait db finstory
```

5. Запустите Spring API:

```powershell
.\gradlew.bat bootRun --args="--spring.profiles.active=local"
```

6. Откройте `Unity_WEB_A_IVR` в Unity Hub и запустите сцену `Assets/Scenes/Main.unity`.

Адреса после запуска:

- Spring API: `http://localhost:8080`;
- FinStory: `http://localhost:8000`;
- административная панель: `http://localhost:8080/admin.html`.

## Сборка WebGL

Для Unity `6000.4.6f1` должен быть установлен модуль **Web Build Support**.

### Через интерфейс Unity

1. Откройте `File → Build Profiles`.
2. Выберите `Web` и нажмите `Switch Platform`.
3. Нажмите `Build` и выберите каталог `Unity_WEB_A_IVR/WebGLBuild`.

### Автоматически из PowerShell

Закройте открытый редактор Unity, затем из корня репозитория выполните:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "$PWD\Unity_WEB_A_IVR" `
  -buildTarget WebGL `
  -executeMethod WebGLBuilder.Build `
  -logFile "$PWD\Unity_WEB_A_IVR\webgl-build.log"
```

После сборки запустите статический сайт через Docker:

```powershell
Set-Location Kotlin_spring_project
docker compose up -d --wait web
```

Игра откроется по адресу `http://localhost:8081`. Двойной щелчок по `index.html` использовать нельзя: WebGL должен работать через HTTP-сервер.

## CI/CD

Основной workflow [`.github/workflows/ci-cd.yml`](.github/workflows/ci-cd.yml) автоматически запускается при каждом push в `main`, pull request, теге `v*` и вручную.

CI выполняет:

1. тесты FinStory на Python 3.12;
2. интеграционные тесты Spring на Java 21;
3. сохранение HTML-отчёта Spring-тестов как артефакта.

CD после успешных тестов:

1. собирает production Docker-образ Spring API;
2. собирает production Docker-образ FinStory;
3. публикует оба образа в GitHub Container Registry.

Последний проверенный успешный запуск: [CI and delivery №18](https://github.com/Ushan4ik/Alfa-IVR-Financial-Game/actions/runs/42100856110). Все четыре задания завершились со статусом `success`.

Отдельный workflow [`.github/workflows/unity-webgl.yml`](.github/workflows/unity-webgl.yml) вручную собирает Unity WebGL и сохраняет загружаемый артефакт `unity-webgl`. Для запуска в GitHub Actions требуется секрет `UNITY_LICENSE`. Это continuous delivery: workflow создаёт проверенные образы и сборочные артефакты, но не развёртывает приложение на публичный домен.

Подробное описание и переменные окружения приведены в [документации CI/CD](docs/CI_CD.md).

## Основные документы

- [Итоговые пользовательские сценарии](final_scripts.md)
- [Техническое задание](docs/TECHNICAL_SPECIFICATION.md)
- [Архитектура](ARCHITECTURE.md)
- [Проверка полного пользовательского маршрута](docs/ACCEPTANCE_TEST.md)
- [Spring API](Kotlin_spring_project/README.md)
- [Unity-клиент](Unity_WEB_A_IVR/README.md)
- [FinStory](FinStory_service/README.md)
- [CI/CD](docs/CI_CD.md)

## Ограничения текущей версии

- полностью работает только мини-игра «Офис мечты»;
- будущие узлы дорожки являются визуальными пустышками;
- отдельный список старых персональных разборов в новом интерфейсе скрыт;
- базовый статус прохождения уроков пока хранится локально, а серверные тесты, персональные уроки, профиль, задания, серия, коины и рынок сохраняются в PostgreSQL;
- публичный production-хостинг не настроен, поэтому локальный WebGL обращается к API `http://localhost:8080`.

Секреты и файл `.env` не должны попадать в GitHub.
