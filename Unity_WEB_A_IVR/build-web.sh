#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
UNITY_EDITOR="${UNITY_EDITOR:-/home/ushan/Unity/Hub/Editor/6000.4.1f1/Editor/Unity}"
LOG_FILE="$PROJECT_DIR/webgl-build.log"

if [[ ! -x "$UNITY_EDITOR" ]]; then
  echo "Unity Editor не найден: $UNITY_EDITOR"
  exit 1
fi

if [[ ! -d "$(dirname "$UNITY_EDITOR")/Data/PlaybackEngines/WebGLSupport" ]]; then
  echo "WebGL Build Support не установлен."
  echo "Установите: unityhub -- --headless install-modules --version 6000.4.1f1 -m webgl"
  exit 1
fi

echo "Собираю Unity WebGL. Лог: $LOG_FILE"
"$UNITY_EDITOR" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_DIR" \
  -buildTarget WebGL \
  -executeMethod WebGLBuilder.Build \
  -logFile "$LOG_FILE"

if [[ ! -f "$PROJECT_DIR/WebGLBuild/index.html" ]]; then
  echo "Сборка не создала WebGLBuild/index.html. Последние строки лога:"
  tail -80 "$LOG_FILE"
  exit 1
fi

echo "Сборка готова: $PROJECT_DIR/WebGLBuild/index.html"
echo "Запуск в браузере: ./serve-web.sh"
