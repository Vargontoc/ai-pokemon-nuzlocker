#!/bin/sh
set -eu

: "${AGENT_NAME:?AGENT_NAME requerido}"
: "${MODELFILE_PATH:?MODELFILE_PATH requerido}"
: "${OLLAMA_HOST:?OLLAMA_HOST requerido}"

export OLLAMA_HOST="$(echo "$OLLAMA_HOST" | sed 's#^http://##' | sed 's#^https://##')"

echo "[register] Esperando a Ollama en ${OLLAMA_HOST}..."
until curl -fsS "http://${OLLAMA_HOST}/api/tags" >/dev/null; do
  sleep 2
done

if curl -fsS "http://${OLLAMA_HOST}/api/tags" | grep -q "\"name\":\"${AGENT_NAME}\""; then
  echo "[register] El modelo ${AGENT_NAME} ya existe."
  exit 0
fi

echo "[register] Creando modelo ${AGENT_NAME}..."
ollama create "${AGENT_NAME}" -f "${MODELFILE_PATH}"

echo "[register] Modelo ${AGENT_NAME} registrado."