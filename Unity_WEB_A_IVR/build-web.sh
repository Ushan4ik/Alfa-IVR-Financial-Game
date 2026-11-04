#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
UNITY_EDITOR="${UNITY_EDITOR:-/home/ushan/Unity/Hub/Editor/6000.4.6f1/Editor/Unity}"
LOG_FILE="$PROJECT_DIR/webgl-build.log"

if [[ ! -x "$UNITY_EDITOR" ]]; then
  echo "Unity Editor не найден: $UNITY_EDITOR"
  exit 1
fi

if [[ ! -d "$(dirname "$UNITY_EDITOR")/Data/PlaybackEngines/WebGLSupport" ]]; then
  echo "WebGL Build Support не установлен."
  echo "Установите: unityhub -- --headless install-modules --version 6000.4.6f1 -m webgl"
  exit 1
fi

echo "Собираю Unity WebGL. Лог: $LOG_FILE"
# Ограничиваем сборку четырьмя ядрами, чтобы первый импорт WebGL не
# замораживал рабочую систему даже на 16-ядерной машине.
taskset -c 0-3 nice -n 10 ionice -c 3 "$UNITY_EDITOR" \
  -batchmode \
  -nographics \
  -quit \
  -job-worker-count 4 \
  -projectPath "$PROJECT_DIR" \
  -buildTarget WebGL \
  -executeMethod WebGLBuilder.Build \
  -logFile "$LOG_FILE"

if [[ ! -f "$PROJECT_DIR/WebGLBuild/index.html" ]]; then
  echo "Сборка не создала WebGLBuild/index.html. Последние строки лога:"
  tail -80 "$LOG_FILE"
  exit 1
fi

# Имена Unity-артефактов постоянны. Версия в URL не даёт браузеру смешать
# framework/wasm/data от разных сборок после очередного локального build.
BUILD_VERSION="$(date -u +%Y%m%d%H%M%S)"
python3 - "$PROJECT_DIR/WebGLBuild/index.html" "$BUILD_VERSION" <<'PY'
import re
import sys
from pathlib import Path

index = Path(sys.argv[1])
version = sys.argv[2]
html = index.read_text(encoding="utf-8")
for asset in (
    "WebGLBuild.loader.js",
    "WebGLBuild.data",
    "WebGLBuild.framework.js",
    "WebGLBuild.wasm",
):
    html = re.sub(re.escape(asset) + r"(?:\?v=[0-9]+)?", f"{asset}?v={version}", html)
index.write_text(html, encoding="utf-8")
PY

echo "Сборка готова: $PROJECT_DIR/WebGLBuild/index.html"
echo "Запуск в браузере: ./serve-web.sh"
