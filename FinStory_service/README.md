# FinStory service

Внутренний Python-сервис персональных финансовых историй. Unity не обращается к нему напрямую:
запросы отправляет Spring после проверки ответов пользователя.

## Возможности

- детерминированная классификация интересов;
- история по слабой теме и ошибкам пользователя;
- сохранение героя между уроками;
- объяснение терминов на примерах из истории;
- деморежим без внешней сети;
- Yandex AI как подключаемый генератор;
- необязательная локальная модель терминов.

## Локальная проверка

```bash
python3 -m venv .venv
.venv/bin/python -m pip install -r requirements.txt
PYTHONPATH=. FINSTORY_DEMO=1 .venv/bin/python -m unittest discover -s tests -q
PYTHONPATH=. FINSTORY_DEMO=1 .venv/bin/uvicorn finstory.api:app --host 127.0.0.1 --port 8000
```

Проверка состояния: `http://127.0.0.1:8000/internal/v1/health`.

Для локальной модели установите `requirements-model.txt`, поместите веса в `models/terms`
или `models/adapter` и задайте `FINSTORY_TERM_MODEL=1`. Веса не хранятся в Git.

## Внутренние endpoints

- `POST /internal/v1/interests/classify` — категории и веса интересов;
- `POST /internal/v1/lessons/generate` — структурированный персональный урок;
- `GET /internal/v1/health` — состояние сервиса без секретов.

Если `FINSTORY_INTERNAL_TOKEN` задан, POST-запросы должны содержать такой же
`X-Internal-Token`. Yandex API key хранится только в окружении сервиса.

