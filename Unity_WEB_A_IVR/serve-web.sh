#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$PROJECT_DIR/WebGLBuild"
BACKEND_DIR="$(cd "$PROJECT_DIR/../Kotlin_spring_project" && pwd)"

if [[ ! -f "$BUILD_DIR/index.html" ]]; then
  echo "WebGL-сборка не найдена. Сначала выполните: ./build-web.sh"
  exit 1
fi

docker compose \
  --env-file "$BACKEND_DIR/.env" \
  -f "$BACKEND_DIR/compose.yaml" \
  up -d --wait web

echo "Игра запущена: http://localhost:8081"
echo "Spring API должен работать отдельно на http://localhost:8080"
