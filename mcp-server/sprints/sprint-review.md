## Sprint Review [22-02-2026-Game-State-Agent]

### Objetivos
- [ ] Inyectar estado actual del nuzlocke como contexto del agente
    - [ ] Leer `NuzlockeState` y `BattleContext` del sessionId en `NuzlockeAgent.GetAdviceAsync`
    - [ ] Serializar el estado a texto legible para el LLM (equipo, pc, bajas, inventario, encuentros)
    - [ ] Incluir el estado serializado en el system prompt o user message del agente
    - [ ] Si el estado está vacío o no existe, indicarlo explícitamente al LLM (no ignorar)
    - [ ] Tests unitarios:
        - [ ] Con estado cargado el prompt incluye equipo y contexto de batalla
        - [ ] Con estado vacío el prompt indica que no hay datos aún
        - [ ] Con BattleContext activo el prompt refleja la batalla en curso

### Aprobación Sprint review
- [ ] Tests passing

### Riesgos
- [ ] El estado serializado puede ser demasiado largo y consumir demasiado contexto del LLM

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
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
