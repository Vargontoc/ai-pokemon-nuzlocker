## Sprint Review [03-03-2026-endpoint]

### Objetivos
- [x] Nuevos endpoints
    - [x] GET `/nuzlocke/{id}/pc` — devuelve `PCStorage` (List<StoredPokemon>)
    - [x] GET `/nuzlocke/{id}/inventory` — devuelve `Inventory` (List<InventoryItem>)
    - [x] GET `/nuzlocke/{id}/graveyard` — devuelve `DeadPokemon` (List<DeadPokemon>)
    - [x] Los tres leen `game_state.json` via `GetGameStateAsync`
    - [x] 404 si el nuzlocke no existe
    - [x] 409 Conflict si el nuzlocke no está inicializado (IsInitialized: false)

### Aprobación Sprint review
- [x] 233 tests pasan en verde correctamente (9 tests nuevos en NuzlockeControllerTests)

### Riesgos
- Ninguno

### Fallos
- Ninguno

### Sugerencias para el próximo Sprint
- [ ] **Workflow `use_item`**
- [ ] **Workflow `next_battle`**
- [ ] **Workflow `start_battle`**
- [ ] **Workflow `next_turn`**
- [ ] **Workflow `end_battle`**
