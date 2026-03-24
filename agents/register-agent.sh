#!/bin/sh
set -eu

: "${AGENT_NAME:?AGENT_NAME requerido}"
: "${MODELFILE_PATH:?MODELFILE_PATH requerido}"
: "${OLLAMA_HOST:?OLLAMA_HOST requerido}"

OLLAMA_BASE="${OLLAMA_HOST}"
OLLAMA_CURL="$(echo "$OLLAMA_HOST" | sed 's#^https\?://##')"
export OLLAMA_HOST="${OLLAMA_BASE}"

MAX_WAIT=60
WAITED=0
echo "[register] Esperando a Ollama en ${OLLAMA_BASE}..."
until curl -fsS "http://${OLLAMA_CURL}/api/tags" >/dev/null; do
  if [ "$WAITED" -ge "$MAX_WAIT" ]; then
    echo "[register] ERROR: Ollama no disponible tras ${MAX_WAIT}s. Abortando."
    exit 1
  fi
  sleep 2
  WAITED=$((WAITED + 2))
done
echo "[register] Ollama disponible."

if curl -fsS "http://${OLLAMA_CURL}/api/tags" | grep -qF "\"${AGENT_NAME}\""; then
  echo "[register] Modelo ${AGENT_NAME} ya existe. Nada que hacer."
  exit 0
fi

echo "[register] Creando modelo ${AGENT_NAME}..."
if ollama create "${AGENT_NAME}" -f "${MODELFILE_PATH}"; then
  echo "[register] ✓ ${AGENT_NAME} registrado correctamente."
else
  echo "[register] ERROR: Falló la creación de ${AGENT_NAME}."
  exit 1
fi