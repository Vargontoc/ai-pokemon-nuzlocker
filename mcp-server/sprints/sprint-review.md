## Sprint Review [17-02-2026-Workflow-Item-Obtained]

### Objetivos
- [ ] Implementar workflow `item_obtained`
    - [ ] `Workflows/Gameplay/ItemObtainedWorkflow.cs`
        - Input: `nuzlocke_id` (string), `item_name` (string), `quantity` (int, default 1), `category` (string: "pokeball", "potion", "battle", "key", "tm", "other"), `location` (string, opcional — dónde se obtuvo)
        - Validación: nuzlocke_id, item_name y category requeridos, nuzlocke_id debe existir
        - FetchData: obtener datos del item via `IPokeApiConnector` si existe en PokeAPI (descripción, efecto). Si no existe (item custom), continuar sin datos externos
        - MutateState: `StateManager.AddInventoryItemAsync(nuzlocke_id, item_name, quantity, category)`
        - GenerateAdvice: LLM aconseja cuándo y cómo usar el item considerando:
            - Equipo actual y sus niveles/HP
            - Inventario existente (no malgastar si ya hay muchos)
            - Items clave para próximos retos (guardar pociones para gimnasios, etc.)
        - Result.Data: item info (nombre, cantidad, categoría), inventario actualizado
    - [ ] Tests: `ItemObtainedWorkflowTests.cs`
        - [ ] Validación: nuzlocke_id, item_name, category requeridos
        - [ ] FetchData: llama a PokeAPI si item existe, no falla si no existe
        - [ ] MutateState: llama a AddInventoryItemAsync con los parámetros correctos
        - [ ] MutateState: quantity por defecto es 1
        - [ ] GenerateAdvice: prompt contiene equipo actual + inventario + item obtenido
        - [ ] Result.Data contiene item info e inventario
    - [ ] Registrar `ItemObtainedWorkflow` en DI (`Program.cs`)
    - [ ] Actualizar `app/workflow.md` con documentación del nuevo workflow
    - [ ] Tests manuales:
        - [ ] `curl POST /nuzlocke/workflow` con `item_obtained` — item nuevo añadido al inventario
        - [ ] `curl POST /nuzlocke/workflow` con `item_obtained` — item existente incrementa cantidad

### Aprobación Sprint review
- [ ] Tests passing (186 existentes + nuevos)

### Riesgos
- [ ] Item no existe en PokeAPI (items custom del jugador) — manejar sin datos externos, advice genérico
- [ ] Categoría inválida — decidir si validar contra lista fija o aceptar cualquier string

### Fallos
-

### Sugerencias para el próximo Sprint
- [ ] **Workflow `manage_moves`**: Gestión de movimientos con análisis
- [ ] **Workflow `evolution`**: Evolución de pokemon con análisis de nuevas capacidades
- [ ] **Workflow `next_battle`**: Preparación pre-batalla
- [ ] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
