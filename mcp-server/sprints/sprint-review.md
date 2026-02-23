## Sprint Review [23-02-2026-LevelUp-StatsCalculator]

### Objetivos

- [x] **Stats Calculator Gen 1**
    - [x] Añadir campos a `TeamMember` y `StoredPokemon`: `PokemonStats Stats`, `int[] DVs` (0-15, default 8), `int[] StatExp` (0-65535, default 0)
    - [x] Crear clase `PokemonStats` con campos: `HP`, `Attack`, `Defense`, `Speed`, `Special`
    - [x] Crear `IStatsCalculator` con método `Calculate(PokemonBaseStats, int[] dvs, int[] statExp, int level, float nature = 1.0f)`
    - [x] Implementar `Gen1StatsCalculator`:
        - [x] HP:  `floor(((Base + DV) × 2 + floor(ceil(sqrt(StatExp)) / 4)) × Level / 100) + Level + 10`
        - [x] Resto: `floor((floor(((Base + DV) × 2 + floor(ceil(sqrt(StatExp)) / 4)) × Level / 100) + 5) × nature)`
        - [x] Nature es un parámetro float (default 1.0f); en Gen 1 siempre es 1.0 pero se deja preparado para Gen 3+
    - [x] Registrar `IStatsCalculator` como `Scoped` en `Program.cs`
    - [x] Integrar en `capture_pokemon`: calcular stats al crear `TeamMember`/`StoredPokemon`

- [x] **Workflow `level_up`** (mutación pura — el agente lo dispara, no genera advice propio)
    - [x] Flujo: usuario dice "Sparky subió al nivel 25" → agente llama `execute_workflow(level_up)` → workflow muta estado → agente redacta confirmación con el resultado del tool
    - [x] Añadir ejemplo en `NuzlockeAgent.SystemPrompt`: `"Sparky subió al nivel 25" → execute_workflow workflowId="level_up", params={nuzlocke_id, nickname, new_level}`
    - [x] Parámetros: `nuzlocke_id` (req), `nickname` (req), `new_level` (opcional; si no se da: nivel actual + 1)
    - [x] Buscar el pokemon por `nickname` en equipo y PC (equipo primero)
    - [x] Incrementar nivel y recalcular stats con `IStatsCalculator`
    - [x] Guardar estado mutado
    - [x] Devolver `WorkflowResult` con `mutation` descriptiva y el nuevo nivel/stats en `Data`
    - [x] `GetSystemPrompt` y `BuildUserMessage` devuelven `string.Empty` → `WorkflowBase` omite la fase de advice automáticamente
    - [x] Registrar en `Program.cs`
    - [x] Tests unitarios:
        - [x] Sube de nivel y recalcula stats correctamente (team)
        - [x] Sube de nivel en PC también funciona
        - [x] Si `new_level` se pasa explícitamente, lo usa en lugar de nivel+1
        - [x] Falla si no se encuentra el pokemon por nickname
        - [x] Falla si `new_level` <= nivel actual

- [x] **Tests unitarios Stats Calculator**
    - [x] HP calculado correctamente para nivel 50, DV=8, StatExp=0, base=45 (Pikachu HP Gen 1)
    - [x] Stat no-HP calculado correctamente
    - [x] Nature multiplier aplicado correctamente (0.9, 1.1, 1.0)
    - [x] DV=15 y StatExp=65535 no producen overflow

### Aprobación Sprint review
- [x] Tests passing (237: 228 existentes + 4 StatsCalc + 5 LevelUp)

### Riesgos
- [x] Cambiar `TeamMember`/`StoredPokemon` puede romper tests existentes → **Mitigación aplicada**: campos nuevos con valores por defecto; `CapturePokemonWorkflowTests` actualizado con mock `IStatsCalculator`

### Fallos
- [x]

### Sugerencias para el próximo Sprint
- [x] **Workflow `manage_moves`**: Gestión de movimientos con análisis. Tiene dos vertientes, el usuario avisa que movimientos tiene el pokemon capturado lo cual seria solo mutable o que movimiento va aprender tanto por nivel o mt/mo. Mostrando toda la informacion de cada movimiento (tipo, categoria, potencia, precision, ...)
- [x] **Workflow `use_item`**: El usuario indica que ha usado un objeto, se resta en inventario se elimina si es 0 (salvo objetos clave o no consumibles). Y si conoce el efecto ejecuta dicho efecto.
- [x] **Workflow `evolution`**: Evolución de pokemon con análisis de nuevas capacidades
- [x] **Workflow `next_battle`**: Preparación pre-batalla
- [x] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [x] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [x] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
- [x] Implementar personalidad al agente
- [x] Generación de audio como salida
