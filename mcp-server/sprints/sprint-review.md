## Sprint Review [15-02-2026-Workflow-Capture-Pokemon]

### Objetivos
- [ ] Implementar workflow `capture_pokemon`
    - [ ] `Workflows/Gameplay/CapturePokemonWorkflow.cs`
        - Input: `nuzlocke_id` (string), `species` (string), `nickname` (string), `location` (string), `level` (int)
        - Validación: todos los parámetros requeridos, nuzlocke_id debe existir
        - FetchData: obtener datos del pokemon via `IPokeApiConnector` (CachedPokeApiConnector: L1 memory → L2 SQLite → PokeAPI). Stats, tipos, movimientos iniciales
        - MutateState:
            - `RecordEncounterAsync(location, species, nickname)` — regla de 1 captura por ruta
            - Si equipo < 6: `AddToTeamAsync(TeamMember)`
            - Si equipo = 6: `MoveToPCAsync` automático (añadir al PC directamente)
            - Guardar estado via `INuzlockeFileManager`
        - GenerateAdvice: LLM analiza el capturado vs equipo actual (tipos, coberturas, debilidades)
        - Result.Data: datos del pokemon capturado (stats, tipos), destino ("team" o "pc")
    - [ ] Adaptar `WorkflowBase` para que workflows de gameplay usen `nuzlocke_id` como sessionId
        - El `nuzlocke_id` del request se usa para cargar/guardar estado via `INuzlockeFileManager` → `StateManager`
    - [ ] Tests: `CapturePokemonWorkflowTests.cs`
        - [ ] Validación: species, nickname, location, level, nuzlocke_id requeridos
        - [ ] FetchData: llama a `IPokeApiConnector.GetPokemonAsync(species)` (mock en tests)
        - [ ] MutateState: record_encounter + add_to_team cuando equipo < 6
        - [ ] MutateState: record_encounter + move_to_pc cuando equipo = 6
        - [ ] MutateState: falla si location ya tiene encounter (regla nuzlocke)
        - [ ] GenerateAdvice: prompt contiene equipo actual + datos del capturado
        - [ ] Result.Data contiene pokemon info y destino
    - [ ] Registrar `CapturePokemonWorkflow` en DI (`Program.cs`)
    - [ ] Actualizar `app/workflow.md` con documentación del nuevo workflow
    - [ ] Tests manuales:
        - [ ] `curl POST /nuzlocke/workflow` con `capture_pokemon` — captura exitosa (equipo vacío, va al team)
        - [ ] `curl POST /nuzlocke/workflow` con `capture_pokemon` — captura con equipo lleno (va al PC)
        - [ ] `curl POST /nuzlocke/workflow` con `capture_pokemon` — location duplicada (error regla nuzlocke)

### Aprobación Sprint review
- [ ]

### Riesgos
- [ ] `IPokeApiConnector.GetPokemonAsync` puede devolver null si la species no existe — manejar con error descriptivo en el workflow
- [ ] La regla de 1 captura por ruta depende de `RecordEncounterAsync` que ya valida duplicados — verificar que el error se propaga correctamente al WorkflowResult
- [ ] El workflow necesita que `nuzlocke_id` esté en el path cache de `NuzlockeFileManager` — puede requerir `ListNuzlockesAsync` previo si el servidor se reinicia

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
- [ ] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
- [ ] **Workflow `route_encounter`**: Consejo sobre qué capturar en una ruta
- [ ] **Workflow `manage_moves`**: Gestión de movimientos con análisis
- [ ] **Workflow `evolution`**: Evolución de pokemon con análisis de nuevas capacidades
- [ ] **Workflow `item_obtained`**: Registro de objetos con consejo de uso
- [ ] **Workflow `next_battle`**: Preparación pre-batalla
