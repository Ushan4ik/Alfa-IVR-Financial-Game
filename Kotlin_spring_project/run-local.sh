#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$PROJECT_DIR"

if [[ ! -f .env ]]; then
  echo "Ошибка: файл $PROJECT_DIR/.env не найден."
  echo "Создайте его: cp .env.example .env"
  exit 1
fi

set -a
# shellcheck disable=SC1091
source .env
set +a

docker compose up -d --wait db web finstory
echo "Unity WebGL: http://localhost:8081"
echo "FinStory: http://localhost:8000/internal/v1/health"

export FINSTORY_URL="${FINSTORY_URL:-http://127.0.0.1:8000}"
export ML_SERVICE_URL="${ML_SERVICE_URL:-$FINSTORY_URL}"

if ss -ltnH 'sport = :8080' | rg -q '.'; then
  http_code="$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:8080/market/state || true)"
  if [[ "$http_code" == "403" || "$http_code" == "200" ]]; then
    echo "Spring API уже запущен: http://localhost:8080 (HTTP $http_code)."
    echo "Второй экземпляр запускать не нужно."
    exit 0
  fi

  echo "Ошибка: порт 8080 уже занят другим процессом."
  ss -ltnp 'sport = :8080' || true
  exit 1
fi
exec ./gradlew bootRun --args='--spring.profiles.active=local'
