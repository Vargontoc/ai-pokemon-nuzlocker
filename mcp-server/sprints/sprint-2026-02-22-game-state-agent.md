## Sprint Review [22-02-2026-Game-State-Agent]

### Objetivos
- [x] Inyectar estado actual del nuzlocke como contexto del agente
    - [x] Leer `NuzlockeState` y `BattleContext` del sessionId en `NuzlockeAgent.GetAdviceAsync`
    - [x] Serializar el estado a texto legible para el LLM (equipo, pc, bajas, inventario, encuentros)
    - [x] Incluir el estado serializado en el user message del agente
    - [x] Serializar el estado en formato compacto de texto plano (no JSON): una línea por sección — `TEAM`, `PC`, `DEATHS`, `ITEMS`, `LOCATION`, `BATTLE`
    - [x] Si el estado está vacío o no existe, indicarlo explícitamente al LLM (`none` / `unknown`)
    - [x] Tests unitarios:
        - [x] Con estado cargado el prompt incluye equipo, PC, deaths, items y location en formato compacto
        - [x] Con estado vacío el prompt indica `none` para todas las secciones y `BATTLE: none`
        - [x] Con BattleContext activo el prompt refleja la batalla en curso (oponente, tipo, turn, leading)

### Aprobación Sprint review
- [x] Tests passing (223: 220 existentes + 3 state context)

### Riesgos
- [x] El estado serializado puede ser demasiado largo y consumir demasiado contexto del LLM → **Mitigación**: formato compacto en texto plano (una línea por sección: TEAM, PC, DEATHS, ITEMS, LOCATION, BATTLE). Sin JSON, sin campos redundantes.

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
