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
  - [x] Diseñar estrategia: `MemoryCache` con TTL configurable por ambiente.
    - L1 (IMemoryCache) con TTL configurable: dev=5min, base=60min, prod=1440min. TTL=0 desactiva L1.
    - L2 (SQLite) persistente sin expiración. Flujo: L1 → L2 → PokeAPI.
  - [x] Implementar caché en `CachedPokeApiConnector` con métricas de hit/miss.
    - Helper genérico `GetOrFetchAsync<TData, TCached>` con logging estructurado (CacheResult, CacheLayer).
    - Absolute expiration (no sliding) para datos estáticos de PokeAPI.
  - [x] Añadir tests que verifiquen TTL y comportamiento de expiración.
    - 7 unit tests en `CachedPokeApiConnectorMemoryCacheTests`: L1 hit, L2 hit con promoción, TTL expiración, TTL=0, full miss, null handling, cross-entity.

- **Nuzlocke management**
  - Resumen: implementar gestión multi-sesión de Nuzlocke con registro central, archivos por sesión y endpoints HTTP para CRUD de sesiones y sus datos.

  - Entregables implementados:
    - `INuzlockeSessionManager` + `NuzlockeSessionManager` implementados y registrados en DI.
    - Persistencia por sesión: archivo oculto `.nuzlocke` en el directorio de la sesión y registro central `nuzlocke_registry.json`.
    - Control de concurrencia por sesión usando `SemaphoreSlim` en `NuzlockeSessionManager`.
    - Endpoints HTTP añadidos en `Program.cs`:
      - `POST /nuzlocke/sessions` — crear sesión (`CreateSessionRequest`)
      - `GET /nuzlocke/sessions` — listar sesiones
      - `GET /nuzlocke/sessions/{id}` — obtener metadata de sesión
      - `DELETE /nuzlocke/sessions/{id}` — eliminar sesión
      - `GET /nuzlocke/sessions/{id}/data` — cargar `.nuzlocke` (NuzlockeFileData)
      - `POST /nuzlocke/sessions/{id}/data` — guardar `.nuzlocke`
    - `StateManager` refactorizado para soportar overloads con `sessionId` y delegar a `INuzlockeSessionManager` cuando corresponde.
    - Kernel/tools/plugin/agent actualizados para ser `sessionId`-aware:
      - `NuzlockeStateTools` (MCP tools) ahora aceptan `sessionId` opcional.
      - `ToolExecutor` propaga `sessionId` desde los argumentos de la llamada a las operaciones de estado.
      - `NuzlockePlugin` funciones del kernel aceptan `sessionId` opcional y usan las sobrecargas de `IStateManager`.
      - `NuzlockeAgent.GetAdviceAsync` acepta `sessionId` opcional y lo inyecta en `ToolCall` antes de ejecutar herramientas.
    - Tests unitarios añadidos:
      - `tests/McpServer.Tests/NuzlockeSessionManagerTests.cs` — cubre crear, listar, cargar, guardar y borrar sesiones (.nuzlocke file lifecycle).

  - Estado actual y criterios de aceptación:
    - Endpoints implementados y registrados en `Program.cs` — COMPLETADO.
    - `NuzlockeSessionManager` implementado con bloqueo por sesión — COMPLETADO.
    - Tests unitarios para el manager — COMPLETADO (ver `NuzlockeSessionManagerTests`).
    - Integración con `StateManager` y tools/agent/plugin para propagar `sessionId` — COMPLETADO.
    - Tests del proyecto: todos los tests pasan en local (49 tests, 0 fallos).
    - Review y fixes aplicados: serialización consistente (camelCase + WriteIndented), race condition en registro, async I/O completo, null-dereference warnings resueltos.

- **LLM: streaming / retries**
  - [ ] Investigar soporte de streaming y retries en el proveedor actual (Ollama/OpenAI).
  - [ ] Implementar retry/backoff o streaming según viabilidad; añadir tests de integración.

- **Context memory**
  - [ ] Diseñar modelo: `game_state` (persistente por `nuzlockeId`) y `battle_context` (temporal durante combate).
  - [ ] Implementar persistencia ligera (in-memory con opción a persistir en SQLite) y políticas de truncado por tokens.
  - [ ] Añadir tests de integración que simulen sesiones largas y combates.


- **NuzlockeAgent — Tests**
  - [ ] Test: ciclo sin llamadas a herramientas (respuesta directa).
  - [ ] Test: ciclo con 1-2 llamadas a herramientas y consolidación de resultados.
  - [ ] Test: manejo de errores (time-outs y herramientas que devuelven JSON inválido).





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

