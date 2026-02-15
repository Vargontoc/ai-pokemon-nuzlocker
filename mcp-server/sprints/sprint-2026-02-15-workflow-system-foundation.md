## Sprint Review [15-02-2026-Workflow-System-Foundation]

### Objetivos
- [x] Implementar Workflow System — Fase 1: Foundation
    - [x] Core abstractions: `IWorkflow`, `WorkflowBase` (template method: FetchData → MutateState → GenerateAdvice)
    - [x] `WorkflowEngine` con auto-discovery vía DI (`IEnumerable<IWorkflow>`)
    - [x] `WorkflowModels`: `WorkflowRequest`, `WorkflowResult`, `WorkflowParameters` (Dictionary<string, JsonElement>), `StateMutation`
    - [x] `WorkflowParametersConverter` (JsonConverter para deserialización genérica)
    - [x] Nuevos modelos de datos:
        - [x] `InventoryItem` (Name, Quantity, Category)
        - [x] `BattleRecord` (OpponentName, BattleType, TurnCount, Outcome, PokemonUsed[], PokemonLost[], AdviceGiven, BattleLog[], Timestamp) — schema para futuro ML
    - [x] Modificar `NuzlockeState`: añadir `Inventory`, `BattleHistory`, `Generation`, `LockeType`
    - [x] Modificar `IStateManager` / `StateManager`: métodos de inventario y update moves
    - [x] Endpoints: `POST /nuzlocke/workflow` + `GET /nuzlocke/workflows`
    - [x] Tests: `WorkflowEngineTests`, `WorkflowParametersTests`, `WorkflowBaseTests`

### Aprobación Sprint review
- [x] 102/102 tests passing (75 existentes + 27 nuevos)

### Riesgos
- [x] El `WorkflowParameters` basado en Dictionary pierde validación en tiempo de deserialización — mitigado por `Validate()` en cada workflow
- [x] Añadir propiedades a `NuzlockeState` puede romper ficheros `.nuzlocke` existentes — mitigado por defaults en las nuevas propiedades
- [x] `JsonElement.TryGetInt32` lanza excepción en tipos no numéricos — corregido con guard `ValueKind == Number`

### Fallos
- [x] `InMemoryStateManager` en `StreamingTests.cs` no implementaba los nuevos métodos de `IStateManager` — corregido añadiendo stubs

### Sugerencias para el próximo Sprint
- [ ] **Workflow Fase 2 — Core Workflows**: `init_nuzlocke`, `capture_pokemon`, `start_battle`, `next_turn`, `end_battle` + tests
- [ ] **Workflow Fase 3 — Remaining Workflows**: `route_encounter`, `manage_moves`, `evolution`, `item_obtained`, `next_battle` + tests
- [ ] **NuzlockeAgent tests avanzados**: Tests de ciclo completo (con/sin tools), manejo de errores y timeouts
- [ ] **Calculadora de daño Gen 1**: Implementar como nuevo MCP tool
- [ ] **Streaming retry con RetryHelper**: Unificar `StreamWithRetriesAsync` de Ollama/OpenAI con el `RetryHelper` compartido
