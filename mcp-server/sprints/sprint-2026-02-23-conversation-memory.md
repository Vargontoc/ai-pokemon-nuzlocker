## Sprint Review [22-02-2026-Conversation-Memory]

### Objetivos
- [x] **Conversation Memory**: Persistir historial de conversación por sesión en `{nuzlocke}/memory/`
    - [x] Definir formato de fichero de memoria: `{nuzlockeId}/memory/conversation.json` — lista de `{role, content, timestamp}`
    - [x] Crear `IConversationMemoryStore` con métodos `LoadAsync(sessionId)` y `AppendAsync(sessionId, entry)`
    - [x] Implementar `FileConversationMemoryStore` usando `INuzlockeFileManager` para resolución de rutas
    - [x] Integrar en `NuzlockeAgent.GetAdviceAsync`: cargar historial previo e inyectarlo en el prompt antes de la pregunta actual
    - [x] Persistir la nueva conversación (pregunta + respuesta) al final de cada `GetAdviceAsync`
    - [x] Limitar el historial inyectado al LLM (máx. 10 turnos = 20 entradas) para no consumir contexto en exceso
    - [x] Integrar en `StreamAdviceAsync` de igual forma (carga + inyección de historial)
    - [x] Tests unitarios:
        - [x] Sin historial previo el prompt no incluye sección `HISTORY`
        - [x] Con historial previo el prompt incluye `HISTORY:` con los últimos N turnos
        - [x] Tras `GetAdviceAsync` se persiste la nueva entrada en memoria
        - [x] El límite de contexto se respeta (no se inyectan más de 20 entradas / 10 turnos)
        - [x] Sin sessionId no se llama al store (ni load ni append)

### Aprobación Sprint review
- [x] Tests passing (228: 223 existentes + 5 conversation memory)

### Riesgos
- [x] El historial puede crecer indefinidamente consumiendo contexto → **Mitigación**: ventana deslizante de 10 turnos (20 entradas) implementada en `BuildHistoryContext`
- [x] La lectura/escritura de fichero en cada request puede ser lenta → **Mitigación**: operaciones async, no bloquean el streaming

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
- [ ] **Workflow `level_up`**: Subida de nivel de un pokemon, sin LLM pues solo es mutacion
- [ ] **Stats calculate**: Cálculo de stats según nivel y tipo de crecimiento (Gen 1). Esta operacion se realizaria tanto en captura como level_up mutando al pokemon dicho por el usuario.
- [ ] **Workflow `manage_moves`**: Gestión de movimientos con análisis. Tiene dos vertientes, el usuario avisa que movimientos tiene el pokemon capturado lo cual seria solo mutable o que movimiento va aprender tanto por nivel o mt/mo. Mostrando toda la informacion de cada movimiento (tipo, categoria, potencia, precision, ...)
- [ ] **Workflow `use_item`**: El usuario indica que ha usado un objeto, se resta en inventario se elimina si es 0 (salvo objetos clave o no consumibles). Y si conoce el efecto ejecuta dicho efecto.
- [ ] **Workflow `evolution`**: Evolución de pokemon con análisis de nuevas capacidades
- [ ] **Workflow `next_battle`**: Preparación pre-batalla
- [ ] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
