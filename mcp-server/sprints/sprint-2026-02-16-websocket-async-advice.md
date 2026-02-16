## Sprint Review [16-02-2026-WebSocket-Async-Advice]

### Objetivos
- [x] Separar respuesta HTTP (determinista) de respuesta LLM (async via WebSocket)
    - [x] `WebSockets/AdviceWebSocketMessages.cs` — DTOs: AdviceStartMessage, AdviceChunkMessage, AdviceEndMessage, AdviceErrorMessage, AdviceDispatchRequest
    - [x] `WebSockets/IAdviceConnectionManager.cs` + `AdviceConnectionManager.cs` — Singleton, gestiona conexiones WebSocket por sessionId con ConcurrentDictionary + SemaphoreSlim por sesion
    - [x] `WebSockets/IAdviceDispatcher.cs` + `AdviceBackgroundDispatcher.cs` — Fire-and-forget, usa IServiceScopeFactory para resolver IAiProvider scoped, streaming via StreamCompletionAsync
    - [x] `Workflows/WorkflowModels.cs` — DeterministicResult + CorrelationId en WorkflowResult
    - [x] `Workflows/IWorkflow.cs` — ExecuteDeterministicAsync
    - [x] Refactor `Workflows/WorkflowBase.cs`:
        - BuildContextAsync (virtual, extrae preamble compartido)
        - ExecuteDeterministicAsync (Validate -> FetchData -> MutateState -> devuelve prompts sin LLM)
        - ExecuteAsync refactorizado para usar BuildContextAsync
    - [x] Adaptar `InitNuzlockeWorkflow` — override BuildContextAsync (skip state loading)
    - [x] Adaptar `CapturePokemonWorkflow` — override BuildContextAsync (nuzlocke_id resolution)
    - [x] `Workflows/WorkflowEngine.cs` — ExecuteWithAsyncAdviceAsync con fallback sincrono
    - [x] `Program.cs`:
        - DI: IAdviceConnectionManager + IAdviceDispatcher como singletons
        - app.UseWebSockets()
        - Endpoint `ws/advice?sessionId=XXX` — acepta WebSocket, mantiene read-loop
        - POST /nuzlocke/workflow usa ExecuteWithAsyncAdviceAsync
- [x] Backward compatibility: sin WebSocket conectado -> ejecucion sincrona como antes
- [x] Tests unitarios:
    - [x] `WebSockets/AdviceConnectionManagerTests.cs` (8 tests): Add/Remove/HasConnection, SendAsync ok/sin conexion/socket cerrado
    - [x] `WebSockets/AdviceBackgroundDispatcherTests.cs` (4 tests): happy path, AI error, sin WebSocket, disconnect mid-stream
    - [x] `Workflows/WorkflowBaseDeterministicTests.cs` (5 tests): validation fail, success returns prompts, runs fetch+mutate, exception handling, ExecuteAsync still works
    - [x] `Workflows/WorkflowEngineTests.cs` (4 tests nuevos): fallback sync, async dispatch, unknown workflow, deterministic fails

### Aprobacion Sprint review
- [x] 172/172 tests passing (151 existentes + 21 nuevos)

### Riesgos
- [x] IAiProvider es scoped — resuelto: AdviceBackgroundDispatcher crea IServiceScope via IServiceScopeFactory
- [x] Sends concurrentes al mismo WebSocket — resuelto: SemaphoreSlim(1,1) por sesion
- [x] WebSocket desconecta mid-stream — resuelto: SendAsync captura excepciones, remueve conexion, streaming para
- [x] Task.Run fire-and-forget — resuelto: todo el body en try/catch con logging + advice_error

### Fallos
- Ninguno

### Sugerencias para el proximo Sprint
- [ ] **Workflow `route_encounter`**: Consejo sobre que capturar en una ruta
- [ ] **Workflow `manage_moves`**: Gestion de movimientos con analisis
- [ ] **Workflow `evolution`**: Evolucion de pokemon con analisis de nuevas capacidades
- [ ] **Workflow `next_battle`**: Preparacion pre-batalla
- [ ] **Workflow `start_battle`**: Inicio de batalla con analisis estrategico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo tactico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
- [ ] **Workflow `item_obtained`**: Registro de objetos con consejo de uso
