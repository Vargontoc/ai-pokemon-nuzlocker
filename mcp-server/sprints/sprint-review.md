## Sprint Review [11-02-2026 - Stream & Context memory]

### Objetivos

- **Response optimization** (inicio de sprint)
  - Resumen: reducir tokens enviados al LLM y tamaño de payloads incluyendo sólo los campos necesarios desde PokeAPI. Implementar opción para devolver un subset JSON configurable desde el conector y agregar pruebas que verifiquen formato y tamaño.

  - Entregables:
    - `subset` JSON definido (esquema mínimo aprobado)
    - Implementación en `CachedPokeApiConnector`/`PokeApiConnector` que permita `fields` o `subset` param
    - Tests unitarios que validen formato y tamaño máximo
    - Métrica que calcule reducción media de tokens/respuesta

  - Implementación (pasos):
    1. Definir y acordar el `subset` por defecto:
       - `id`, `name`, `types`, `stats` (HP, Atk, Def, SpAtk, SpDef, Speed), `moves_basicos` (nombre + tipo + power si disponible), `sprite_min` (url pequeña)
    2. Añadir un parámetro opcional `subset` al conector (ej. `GetPokemon(name, subset=true|fields=...)`).
    3. Implementar transformación que toma la respuesta completa y devuelve sólo el subset.
    4. Añadir tests unitarios que validen: campos presentes, formato y que la longitud/respuesta sea menor que la completa.
    5. Instrumentar métricas simples (contador y tamaño bytes antes/después, tokens estimados) para evaluar reducción.

  - Criterios de aceptación:
    - El conector devuelve el `subset` por defecto cuando se solicita.
    - Tests unitarios cubren 3 pokémon distintos y pasan en CI.
    - Reducción media de tokens >= 40% en respuestas de ejemplo (medición en ambiente de prueba).

  - Primeros pasos (hoy):
    - [x] Documentar el `subset` definitivo en `docs/` y en `sprints/sprint-review.md` (esta tarea).
    - [x] Crear una rama/PR de prototipo para la función `subset` en el conector (rama creada y subida: `sprint/response-optimization`).
    - [x] Escribir 1 test unitario que valide la transformación para `pikachu`.

  - Progreso (11-02-2026):
    - Implementación del `PokemonSubset` model y mapeo en `PokeApiConnector` y `CachedPokeApiConnector`.
    - Test unitario `PokeApiSubsetTests` añadido en `tests/McpServer.Tests/`.
    - Cambios commiteados y rama `sprint/response-optimization` empujada al remoto.
    - Pull request sugerido: https://github.com/Vargontoc/ai-pokemon-nuzlocker/pull/new/sprint/response-optimization


- **Caching (PokeAPI)**
  - [ ] Diseñar estrategia: `MemoryCache` con TTL configurable por ambiente.
  - [ ] Implementar caché en `CachedPokeApiConnector` con métricas de hit/miss.
  - [ ] Añadir tests que verifiquen TTL y comportamiento de expiración.

- **Context memory**
  - [ ] Diseñar modelo: `game_state` (persistente por `nuzlockeId`) y `battle_context` (temporal durante combate).
  - [ ] Implementar persistencia ligera (in-memory con opción a persistir en SQLite) y políticas de truncado por tokens.
  - [ ] Añadir tests de integración que simulen sesiones largas y combates.

- **Session management**
  - [ ] Definir API: `POST /nuzlocke`, `GET /nuzlocke/{id}`, `DELETE /nuzlocke/{id}`.
  - [ ] Implementar gestión concurrente de varias sesiones y pruebas de carga básicas.

- **NuzlockeAgent — Tests**
  - [ ] Test: ciclo sin llamadas a herramientas (respuesta directa).
  - [ ] Test: ciclo con 1-2 llamadas a herramientas y consolidación de resultados.
  - [ ] Test: manejo de errores (time-outs y herramientas que devuelven JSON inválido).

- **LLM: streaming / retries**
  - [ ] Investigar soporte de streaming y retries en el proveedor actual (Ollama/OpenAI).
  - [ ] Implementar retry/backoff o streaming según viabilidad; añadir tests de integración.

- **Prioridad inicial (primeros 3 días)**
  - [ ] Definir subset de `Response optimization` (campo y formato).  <-- empezar aquí
  - [ ] Diseñar TTL por defecto para `Caching` (pregunta abierta).
  - [ ] Prototipar `game_state` minimal para `Context memory`.



### Aprobación Sprint review
- [ ] Compilación sin errores
- [ ] Tests unitarios e integración relevantes pasan en verde
- [ ] `NuzlockeAgent` responde de forma estable con y sin tools

### Riesgos
- Cambios en formato de respuesta pueden romper consumidores si no se coordinan

### Fallos
- [ ] 

### Sugerencias para el próximo Sprint

### Preguntas abiertas



### Aprobación Sprint review
- [ ] Compilación sin errores
- [ ] Tests unitarios e integración relevantes pasan en verde
- [ ] `NuzlockeAgent` responde de forma estable con y sin tools


### Fallos
- [ ] 

### Sugerencias para el próximo Sprint
- **Calculadora de daño Gen 1**: Implementar como nuevo MCP tool
- **Team analysis tool**: Analizar debilidades del equipo y sugerir capturas

