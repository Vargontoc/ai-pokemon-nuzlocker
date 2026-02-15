## Sprint Review [15-02-2026-Workflow-Init-Nuzlocke]

### Objetivos
- [ ] Implementar workflow `init_nuzlocke` (Setup one-shot)
    - [ ] `Workflows/Setup/InitNuzlockeWorkflow.cs`: Workflow de inicialización de partida Nuzlocke
        - Input: `generation` (int, solo 1 por ahora), `locke_type` (string: "standard", "hardcore", etc.), `base_path` (string, obligatorio desde front)
        - Validación: generation requerido (solo Gen 1), base_path requerido
        - NuzlockeId: GUID + fecha de creación (ej: `a3f1b2c4_2026-02-15`)
        - Crea estructura de carpetas:
            ```
            {base_path}/
              └── {nuzlockeId}/
                    ├── .nuzlocke          ← metadata (id, generation, locke_type, created_at)
                    ├── game_state.json    ← NuzlockeState
                    ├── memory/            ← contexto de memoria del agente
                    └── results/           ← BattleRecords individuales (battle_001_vs_brock.json)
            ```
            - `battle_state.json` es temporal, solo existe durante batalla activa, se elimina al finalizar
        - GenerateAdvice: LLM da tips de inicio (starter, early game survival)
    - [ ] Refactorizar sistema de persistencia para nueva estructura
        - [ ] `INuzlockeFileManager` (nuevo): interfaz para operaciones de fichero por nuzlocke
            - CreateNuzlocke(basePath, generation, lockeType) → NuzlockeId + estructura creada
            - LoadGameState(nuzlockeId) / SaveGameState(nuzlockeId, state)
            - LoadBattleState(nuzlockeId) / SaveBattleState(nuzlockeId, battle) / DeleteBattleState(nuzlockeId)
            - SaveBattleRecord(nuzlockeId, BattleRecord) → results/battle_NNN_vs_opponent.json
            - ListNuzlockes(basePath) → lista de nuzlockes activos (leyendo .nuzlocke de cada carpeta)
            - GetNuzlockeMetadata(nuzlockeId) → metadata desde .nuzlocke
        - [ ] `NuzlockeFileManager`: implementación con ficheros JSON separados
        - [ ] Adaptar `IStateManager` / `StateManager` para delegar en `INuzlockeFileManager`
        - [ ] Mantener compatibilidad: endpoints de sesión existentes siguen funcionando
    - [ ] Registrar `InitNuzlockeWorkflow` + `INuzlockeFileManager` en DI (`Program.cs`)
    - [ ] Tests: `InitNuzlockeWorkflowTests.cs`
        - [ ] Validación: generation requerido, gen != 1 rechazada, base_path requerido
        - [ ] Estructura de carpetas creada correctamente
        - [ ] .nuzlocke contiene metadata correcta (id, gen, type, fecha)
        - [ ] game_state.json inicializado con Generation y LockeType
        - [ ] GenerateAdvice: prompt contiene generación y tipo de locke
    - [ ] Tests: `NuzlockeFileManagerTests.cs`
        - [ ] CreateNuzlocke crea estructura completa
        - [ ] ListNuzlockes descubre nuzlockes existentes
        - [ ] Load/Save GameState + BattleState
        - [ ] SaveBattleRecord genera fichero individual en results/
        - [ ] DeleteBattleState limpia temporal
    - [ ] Test manual: `curl POST /nuzlocke/workflow` con `init_nuzlocke`
    - [ ] En {workspace}/app estaria el cliente web que consumiria estos workflows. Agregar o crear si no existiera un fichero llamado workflow.md que describa el workflow creado, los parametros que necesita y la respuesta que espera. Sin grandes añadidos es simplemente para que el equipo de front pueda implementarlo. 

### Aprobación Sprint review
- [ ]

### Riesgos
- [ ] Refactorizar persistencia puede romper tests existentes que usan `InMemoryStateManager` — mitigado: adaptar stubs
- [ ] El front DEBE configurar `base_path` antes de poder operar — si no lo hace, ningún workflow funciona
- [ ] Concurrencia en ficheros — reutilizar patrón `SemaphoreSlim` por nuzlockeId (como el session manager actual)

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
- [ ] **Workflow `capture_pokemon`**: Captura + record encounter + add to team/PC + análisis estratégico
- [ ] **Workflow `start_battle` + `next_turn` + `end_battle`**: Ciclo completo de batalla con BattleRecord
- [ ] **Workflow Fase 3 — Remaining**: `route_encounter`, `manage_moves`, `evolution`, `item_obtained`, `next_battle`
- [ ] **NuzlockeAgent tests avanzados**: Tests de ciclo completo (con/sin tools), manejo de errores y timeouts
- [ ] **Calculadora de daño Gen 1**: Implementar como nuevo MCP tool
- [ ] **Streaming retry con RetryHelper**: Unificar `StreamWithRetriesAsync` de Ollama/OpenAI con el `RetryHelper` compartido
