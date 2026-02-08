## Sprint Review [Function Calling - Agente con MCP Tools]

### Objetivos
- [ ] Implementar function calling en ClaudeAiProvider (tool use API)
- [ ] Implementar function calling en OpenAiProvider (function calling API)
- [ ] Crear ToolDefinition para cada MCP tool disponible
- [ ] Actualizar NuzlockeAgent para usar function calling
- [ ] Permitir que el agente ejecute AddToTeam, MarkAsDead, MoveToPC, RecordEncounter
- [ ] Implementar ciclo de conversación multi-turno (tool call → execute → continue)
- [ ] Agregar tests para agente con function calling
- [ ] Documentar ejemplos de uso con tools

### Aprobación Sprint review
- [ ] Agente puede decidir cuándo usar cada tool basado en contexto
- [ ] Function calling funciona correctamente con Claude
- [ ] Function calling funciona correctamente con OpenAI
- [ ] El agente ejecuta tools y continúa la conversación con resultados
- [ ] Logs muestran claramente qué tools se ejecutaron
- [ ] Tests verifican ejecución correcta de tools
- [ ] Documentación incluye ejemplos de conversaciones con tool usage

### Riesgos
- Function calling requiere modelos específicos (Claude 3+, GPT-4+)
- Ollama puede no soportar function calling de forma estándar
- Complejidad del ciclo multi-turno (tool call → execute → response)
- Costos aumentan con múltiples tool calls por conversación

### Fallos
- Ninguno (sprint aún no iniciado)

### Sugerencias para el próximo Sprint
- **Context memory**: Mantener historial completo de conversación
- **Tool result formatting**: Mejorar formato de resultados de tools para mejor comprensión de la IA
- **Calculadora de daño Gen 1**: Implementar como nuevo tool
- **Batch tool calling**: Permitir ejecutar múltiples tools en paralelo
