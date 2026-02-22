## Sprint Review [22-02-2026-Event-System]

### Objetivos
- [x] Implementar Event System para workflows
    - [x] Definir DTO `WorkflowEventMessage` (hereda `AdviceWebSocketMessage`) con WorkflowId, Success, Mutations, Data, Errors
    - [x] Emisión de eventos en `WorkflowEngine` (método privado `EmitWorkflowEventAsync`) — sin nueva interface, aprovecha `IAdviceConnectionManager` ya inyectado
    - [x] Hook en `ExecuteAsync`: emite evento tras ejecución completa (éxito o error)
    - [x] Hook en `ExecuteWithAsyncAdviceAsync`: emite evento tras resultado determinista (éxito o error), antes del dispatch de advice
    - [x] Emitir eventos via WebSocket al frontend: `workflow_event` message type
    - [x] Tests unitarios (6 nuevos en `WorkflowEngineTests.cs`):
        - [x] ExecuteAsync con WebSocket emite WorkflowEventMessage con mutations correctas
        - [x] ExecuteAsync sin WebSocket no emite evento
        - [x] ExecuteAsync con workflow fallido emite evento con Success=false y Errors
        - [x] ExecuteWithAsyncAdvice emite evento Y dispatcha advice
        - [x] SendAsync falla → no lanza excepción
        - [x] Sin CorrelationId → genera uno automáticamente
    - [x] Sin cambios en DI (Program.cs) — WorkflowEngine ya tiene IAdviceConnectionManager
    - [x] Sin cambios en WorkflowBase ni en ningún workflow existente

### Aprobación Sprint review
- [x] Tests passing (220: 214 existentes + 6 event system)

### Riesgos
- [ ]

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
- [ ] **Game State en Agent**: Inyectar estado actual del nuzlocke como contexto del agente
- [ ] **Conversation Memory**: Persistir historial de conversación en {nuzlocke}/memory/
- [ ] **Workflow `level_up`**: Subida de nivel de un pokemon, sin LLM pues solo es mutacion
- [ ] **Stats calculate**: Cálculo de stats según nivel y tipo de crecimiento (Gen 1). Esta operacion se realizaria tanto en captura como level_up mutando al pokemon dicho por el usuario.
- [ ] **Workflow `manage_moves`**: Gestión de movimientos con análisis. Tiene dos vertientes, el usuario avisa que movimientos tiene el pokemon capturado lo cual seria solo mutable o que movimiento va aprender tanto por nivel o mt/mo. Mostrando toda la informacion de cada movimiento (tipo, categoria, potencia, precision, ...)
- [ ] **Workflow `use_item`**: El usuario indica que ha usado un objeto, se resta en inventario se elimina si es 0 (salvo objetos clave o no consumibles). Y si conoce el efecto ejecuta dicho efecto.
- [ ] **Workflow `evolution`**: Evolución de pokemon con análisis de nuevas capacidades
- [ ] **Workflow `next_battle`**: Preparación pre-batalla
- [ ] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
- [ ] Implementar personalidad al agente
- [ ] Generación de audio como salida
