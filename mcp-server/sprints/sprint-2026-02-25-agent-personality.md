## Sprint Review [23-02-2026-Agent-Personality]

### Objetivos
- [x] **Personalidad dinámica del agente**
    - [x] Definir enum/constante `AgentPersonality`: `Technical`, `Friendly`, `Cynical`, `Sarcastic`, `Joker`, `Sensual`, `Depressed`, `Enthusiastic`
    - [x] Añadir `Personality` al estado de sesión (configurable por sesión, no por nuzlocke)
    - [x] Crear `IPersonalityPromptProvider` con método `GetPersonalityBlock(AgentPersonality)` → texto de instrucción para el system prompt
    - [x] Implementar `PersonalityPromptProvider` con bloque de instrucción para cada personalidad (EN + ES)
    - [x] Integrar en `NuzlockeAgent.SystemPrompt`: inyectar bloque de personalidad al construir el prompt (dinámico por sesión)
    - [x] Exponer endpoint o workflow para que el usuario configure la personalidad: `set_personality` (parámetros: `nuzlocke_id`, `personality`)
    - [x] Tests unitarios:
        - [x] Cada personalidad genera un bloque de instrucción no vacío y distinto al resto
        - [x] Sin personalidad configurada, el agente usa `Technical` (default)
        - [x] El system prompt inyectado contiene el bloque de personalidad correcto

### Aprobación Sprint review
- [x] Tests passing (240: 237 existentes + 2 PersonalityProvider + 1 integración agente)

### Riesgos
- [x] El bloque de personalidad puede sesgar demasiado las respuestas técnicas → **Mitigación propuesta**: inyectar personalidad solo en el estilo de respuesta, no en las instrucciones de workflow ni reglas Nuzlocke.

### Fallos
- []

### Sugerencias para el próximo Sprint
- [] **Workflow `manage_moves`**: Gestión de movimientos con análisis. Tiene dos vertientes, el usuario avisa que movimientos tiene el pokemon capturado lo cual seria solo mutable o que movimiento va aprender tanto por nivel o mt/mo. Mostrando toda la informacion de cada movimiento (tipo, categoria, potencia, precision, ...)
- [] **Workflow `use_item`**: El usuario indica que ha usado un objeto, se resta en inventario se elimina si es 0 (salvo objetos clave o no consumibles). Y si conoce el efecto ejecuta dicho efecto.
- [] **Workflow `evolution`**: Evolución de pokemon con análisis de nuevas capacidades
- [] **Workflow `next_battle`**: Preparación pre-batalla
- [] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
