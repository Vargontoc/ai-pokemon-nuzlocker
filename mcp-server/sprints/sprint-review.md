## Sprint Review [DD-MM-YYYY-battle-workflows]

### Objetivos

- [ ] **Workflow `use_item`**
    - El usuario indica que ha usado un objeto en su inventario
    - Se resta la cantidad del inventario; si llega a 0 se elimina la entrada (salvo objetos clave o no consumibles)
    - Si el objeto tiene efecto conocido (poción, antídoto, etc.), se aplica al Pokémon indicado
    - Genera consejo del LLM sobre el uso del objeto en contexto

- [ ] **Workflow `next_battle`**
    - Preparación pre-batalla: el usuario indica el próximo oponente (entrenador o Pokémon salvaje)
    - Analiza el equipo actual vs el oponente con datos de PokeAPI (tipos, movimientos)
    - Crea el `BattleContext` con la información del oponente
    - Genera consejo estratégico del LLM antes de entrar al combate

- [ ] **Workflow `start_battle`**
    - Inicio formal de la batalla (cuando empieza el primer turno)
    - Fija el Pokémon activo del jugador y del oponente
    - Genera análisis táctico inicial del LLM (matchup, ventajas/desventajas)

- [ ] **Workflow `next_turn`**
    - Registro de un turno de batalla: movimiento usado, daño, efectos de estado
    - Actualiza el `BattleContext` (HP aproximado, estados, turnos transcurridos)
    - Genera consejo táctico del LLM para el siguiente turno

- [ ] **Workflow `end_battle`**
    - Cierre de batalla: resultado (victoria/derrota/huida), bajas sufridas
    - Si hay Pokémon muertos, los marca como `fainted` en el equipo y los mueve al cementerio
    - Crea un `BattleRecord` en `results/`
    - Limpia el `BattleContext` (borra `battle_state.json`)
    - Genera resumen narrativo del LLM

### Aprobación Sprint review
- [ ] Tests pasan en verde correctamente

### Riesgos
- [ ]

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
- [ ]
