## Sprint Review [23-02-2026-LevelUp-StatsCalculator]

### Objetivos

- [ ] **Stats Calculator Gen 1**
    - [ ] Añadir campos a `TeamMember` y `StoredPokemon`: `PokemonStats Stats`, `int[] DVs` (0-15, default 8), `int[] StatExp` (0-65535, default 0)
    - [ ] Crear clase `PokemonStats` con campos: `HP`, `Attack`, `Defense`, `Speed`, `Special`
    - [ ] Crear `IStatsCalculator` con método `Calculate(PokemonBaseStats, int[] dvs, int[] statExp, int level, float nature = 1.0f)`
    - [ ] Implementar `Gen1StatsCalculator`:
        - [ ] HP:  `floor(((Base + DV) × 2 + floor(ceil(sqrt(StatExp)) / 4)) × Level / 100) + Level + 10`
        - [ ] Resto: `floor((floor(((Base + DV) × 2 + floor(ceil(sqrt(StatExp)) / 4)) × Level / 100) + 5) × nature)`
        - [ ] Nature es un parámetro float (default 1.0f); en Gen 1 siempre es 1.0 pero se deja preparado para Gen 3+
    - [ ] Registrar `IStatsCalculator` como `Scoped` en `Program.cs`
    - [ ] Integrar en `capture_pokemon`: calcular stats al crear `TeamMember`/`StoredPokemon`

- [ ] **Workflow `level_up`** (mutación pura — el agente lo dispara, no genera advice propio)
    - [ ] Flujo: usuario dice "Sparky subió al nivel 25" → agente llama `execute_workflow(level_up)` → workflow muta estado → agente redacta confirmación con el resultado del tool
    - [ ] Añadir ejemplo en `NuzlockeAgent.SystemPrompt`: `"Sparky subió al nivel 25" → execute_workflow workflowId="level_up", params={nuzlocke_id, nickname, new_level}`
    - [ ] Parámetros: `nuzlocke_id` (req), `nickname` (req), `new_level` (opcional; si no se da: nivel actual + 1)
    - [ ] Buscar el pokemon por `nickname` en equipo y PC (equipo primero)
    - [ ] Incrementar nivel y recalcular stats con `IStatsCalculator`
    - [ ] Guardar estado mutado
    - [ ] Devolver `WorkflowResult` con `mutation` descriptiva y el nuevo nivel/stats en `Data`
    - [ ] `GetSystemPrompt` y `BuildUserMessage` devuelven `string.Empty` → `WorkflowBase` omite la fase de advice automáticamente
    - [ ] Registrar en `Program.cs`
    - [ ] Tests unitarios:
        - [ ] Sube de nivel y recalcula stats correctamente (team)
        - [ ] Sube de nivel en PC también funciona
        - [ ] Si `new_level` se pasa explícitamente, lo usa en lugar de nivel+1
        - [ ] Falla si no se encuentra el pokemon por nickname
        - [ ] Falla si `new_level` <= nivel actual

- [ ] **Tests unitarios Stats Calculator**
    - [ ] HP calculado correctamente para nivel 50, DV=8, StatExp=0, base=45 (Pikachu HP Gen 1)
    - [ ] Stat no-HP calculado correctamente
    - [ ] Nature multiplier aplicado correctamente (0.9, 1.1, 1.0)
    - [ ] DV=15 y StatExp=65535 no producen overflow

### Aprobación Sprint review
- [ ] Tests passing (228 existentes + ~7 nuevos)

### Riesgos
- [ ] Cambiar `TeamMember`/`StoredPokemon` puede romper tests existentes que construyen esos objetos → **Mitigación**: campos nuevos tienen valores por defecto, retrocompatibilidad garantizada con JSON camelCase

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
- [ ] **Workflow `manage_moves`**: Gestión de movimientos con análisis. Tiene dos vertientes, el usuario avisa que movimientos tiene el pokemon capturado lo cual seria solo mutable o que movimiento va aprender tanto por nivel o mt/mo. Mostrando toda la informacion de cada movimiento (tipo, categoria, potencia, precision, ...)
- [ ] **Workflow `use_item`**: El usuario indica que ha usado un objeto, se resta en inventario se elimina si es 0 (salvo objetos clave o no consumibles). Y si conoce el efecto ejecuta dicho efecto.
- [ ] **Workflow `evolution`**: Evolución de pokemon con análisis de nuevas capacidades
- [ ] **Workflow `next_battle`**: Preparación pre-batalla
- [ ] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
- [ ] Implementar personalidad al agente
- [ ] Generación de audio como salida
