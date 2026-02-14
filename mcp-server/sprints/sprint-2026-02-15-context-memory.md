## Sprint Review [DD-MM-YYYY-Title]

### Objetivos
- [x] **Context memory**: Diseñar e implementar `game_state` persistente y `battle_context` temporal por evento de batalla

  - Entregables implementados:
    - [x] Modelo `BattleContext` enriquecido con `BattleStartedAt` y `BattleType`
    - [x] `IStateManager` ampliado con 8 métodos de battle context (4 pares con/sin sessionId)
    - [x] `StateManager` implementa battle context: default session usa `session_state.battle.json`, sessions con manager usan `NuzlockeFileData.BattleContext`
    - [x] `start_battle` borra contexto anterior y crea uno nuevo (requisito del sprint)
    - [x] `add_battle_log` incrementa turno y registra evento
    - [x] `end_battle` limpia el contexto de batalla
    - [x] 3 nuevas `ToolDefinition` registradas en `AllTools` (8 tools total para el agente)
    - [x] 3 nuevos handlers en `ToolExecutor` con arg classes
    - [x] 3 nuevos `[McpServerTool]` en `NuzlockeStateTools`
    - [x] 3 nuevos `[KernelFunction]` en `NuzlockePlugin`
    - [x] `NuzlockeAgent` inyecta `BattleContext` en el prompt (`BuildBattleContextString`)
    - [x] System prompt actualizado con instrucciones de battle context
    - [x] `StreamAdviceInternalAsync` también inyecta battle context
    - [x] 7 tests en `StateManagerTests` (start, get, addLog, end, overwrite, edge cases)
    - [x] 3 tests en `ToolExecutorTests` (start_battle, add_battle_log, end_battle)

  - Estado: 59/59 tests passed, 0 errors, 0 warnings

### Aprobación Sprint review
- [x] Compilación sin errores (0 errors, 0 warnings)
- [x] Tests unitarios e integración relevantes pasan en verde (59/59 passed)
- [ ] `NuzlockeAgent` responde de forma estable con y sin tools

### Riesgos
- Ninguno identificado

### Fallos
- Ninguno

### Sugerencias para el próximo Sprint
- [ ] **Retry/backoff para LLM**: Implementar política de reintentos con backoff exponencial en proveedores AI
- [ ] **NuzlockeAgent tests avanzados**: Tests de ciclo completo (con/sin tools), manejo de errores y timeouts
- [ ] **Calculadora de daño Gen 1**: Implementar como nuevo MCP tool
- [ ] **Team analysis tool**: Analizar debilidades del equipo y sugerir capturas
- [ ] **Métricas de reducción de tokens**: Instrumentar métricas para validar la optimización del subset (>= 40%)