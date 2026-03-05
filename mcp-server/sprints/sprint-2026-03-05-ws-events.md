## Sprint Review [05-03-2026-ws-events]

### Objetivos
- [x] Mejorar envío de eventos al WebSocket según el nuzlockeId
    - [x] Enviar `workflow_start` antes de ejecutar cada workflow
    - [x] Enviar `agent_tool_call` cuando el agente invoca una herramienta (fuente: database/pokeapi/workflow)
    - [x] Documentar en Swagger todos los esquemas de eventos WS (`/ws/events/*`)

### Aprobación Sprint review
- [x] Tests pasan en verde (244/244)

### Fallos resueltos
- [x] Bug: `/nuzlocke/advice` llamaba a `init_nuzlocke` workflow — eliminado de ToolDefinitions y system prompt del agente

### Cambios implementados
- `ToolDefinitions.cs`: eliminado `init_nuzlocke` de la descripción y ejemplos del tool `execute_workflow`
- `NuzlockeAgent.cs`: eliminado ejemplo de `init_nuzlocke` del system prompt
- Nuevos mensajes WS: `WorkflowStartMessage` + `AgentToolCallMessage`
- `WorkflowEngine`: emite `workflow_start` antes de ejecutar (ambos paths)
- `AdviceBackgroundDispatcher`: pasa callback al agente para emitir `agent_tool_call` por WS
- `WsEventsSchemaController` (`/ws/events/*`): Swagger documenta todos los eventos

### Sugerencias para el próximo Sprint
- [ ] Análisis de rendimiento y mejora de tiempos de respuesta del agente
