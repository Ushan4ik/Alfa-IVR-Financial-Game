FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    FINSTORY_DEMO=1

WORKDIR /app
COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt
COPY finstory ./finstory
COPY data ./data
COPY dictionary.json ./dictionary.json

RUN useradd --create-home --uid 10001 finstory
USER finstory
EXPOSE 8000

CMD ["uvicorn", "finstory.api:app", "--host", "0.0.0.0", "--port", "8000"]

