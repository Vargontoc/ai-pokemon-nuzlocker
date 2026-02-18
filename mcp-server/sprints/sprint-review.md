## Sprint Review [17-02-2026-Agent-Workflow-Bridge]

### Objetivos
- [x] Implementar Agent Workflow Bridge
    - [x] Añadir tool `execute_workflow` a `ToolDefinitions.cs`
        - Parámetros: `workflowId` (string), `parameters` (JSON string con los parámetros del workflow)
        - Descripción detallada para el LLM con los workflows disponibles y sus parámetros
    - [x] Implementar `ExecuteWorkflowAsync` en `ToolExecutor.cs`
        - Inyectar `IWorkflowEngine` en ToolExecutor
        - Deserializar los parámetros JSON del tool call → `WorkflowRequest`
        - Ejecutar via `IWorkflowEngine.ExecuteAsync`
        - Devolver el `WorkflowResult` serializado al LLM
    - [x] Actualizar system prompt de `NuzlockeAgent.cs`
        - Instruir al agente que ante acciones del jugador (captura, item, encounter) use `execute_workflow`
        - Ejemplos de mapeo: "Capturé un Weedle..." → `capture_pokemon`, "Encontré una poción..." → `item_obtained`
    - [x] Tests: `AgentWorkflowBridgeTests.cs` (8 tests)
        - [x] ToolExecutor con `execute_workflow` llama a IWorkflowEngine.ExecuteAsync
        - [x] Parámetros JSON se mapean correctamente al WorkflowRequest
        - [x] workflowId inválido → error descriptivo
        - [x] Parámetros inválidos → se devuelve el error del workflow
        - [x] sessionId se inyecta automáticamente desde nuzlocke_id
        - [x] JSON inválido en parameters → error descriptivo
        - [x] workflowId vacío → error
        - [x] parameters vacío → default a objeto vacío
    - [x] Fix: Async agent advice via WebSocket (evita timeout HTTP con Ollama)
        - `NuzlockeController.GetAdvice` detecta si hay WebSocket conectado para el sessionId
        - Si hay WebSocket → `DispatchAgentAdvice` fire-and-forget, HTTP devuelve `{ correlationId, advice: null }` inmediatamente
        - Si no hay WebSocket → fallback síncrono (backward compatible)
        - `AdviceBackgroundDispatcher.DispatchAgentAdvice` resuelve `NuzlockeAgent` en scope, ejecuta `GetAdviceAsync` completo (con tool calling), envía resultado via WebSocket (`advice_start` + `advice_end`)
        - 3 tests nuevos en `AdviceBackgroundDispatcherTests.cs`
    - [x] Tests manuales:
        - [x] Conectar WebSocket a `ws://server/ws/advice?sessionId=<id>`, luego `POST /nuzlocke/advice` con "Capturé un Pikachu nivel 5 en Viridian Forest y lo llamé Sparky" → HTTP devuelve inmediato con correlationId, WebSocket recibe advice
        - [x] Sin WebSocket: `POST /nuzlocke/advice` con "Encontré 3 pociones en Ciudad Verde" → respuesta síncrona (backward compat)

### Aprobación Sprint review
- [x] Tests passing (213: 202 existentes + 8 bridge + 3 async agent)

### Riesgos
- [ ] LLM no detecta la intención → depende de la calidad del system prompt y del modelo
- [ ] LLM envía parámetros incorrectos → el workflow devuelve errores que el LLM puede corregir en el siguiente ciclo

### Fallos
- [x] `POST /nuzlocke/advice` timeout con Ollama → Solucionado con async WebSocket pattern
- [] Si no encuentra el nuzlocke_id de la session que simplemente cancele la operación lanzando un error que se enviara tambien por el websocket.

### Sugerencias para el próximo Sprint
- [ ] **Event System**: Emitir eventos cuando un workflow muta estado tanto exito como errores
- [ ] **Conversation Memory**: Persistir historial de conversación en {nuzlocke}/memory/
- [ ] **Game State en Agent**: Inyectar estado actual del nuzlocke como contexto del agente
- [ ] **Workflow `level_up`**: Subida de nivel de un pokemon, sin LLM pues solo es mutacion
- [ ] **Stats calculate**: Cálculo de stats según nivel y tipo de crecimiento (Gen 1). Esta operacion se realizaria tanto en captura como level_up mutando al pokemon.
- [ ] **Workflow `manage_moves`**: Gestión de movimientos con análisis. Tiene dos vertientes, el usuario avisa que movimientos tiene el pokemon capturado o que movimiento va aprender tanto por nivel o mt/mo
- [ ] **Workflow `evolution`**: Evolución de pokemon con análisis de nuevas capacidades
- [ ] **Workflow `next_battle`**: Preparación pre-batalla
- [ ] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
- [ ] Implementar personalidad al agente
- [ ] Generación de audio como salida.  
