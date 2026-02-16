## Sprint Review [16-02-2026-Workflow-Route-Encounter]

### Objetivos
- [ ] Implementar workflow `route_encounter`
    - [ ] `Workflows/Gameplay/RouteEncounterWorkflow.cs`
        - Input: `nuzlocke_id` (string), `route_name` (string), `available_pokemon` (string[], lista de especies posibles en la ruta)
        - Validación: nuzlocke_id y route_name requeridos, nuzlocke_id debe existir, ruta no debe tener encounter previo
        - FetchData: obtener datos de todos los pokemon disponibles via `IPokeApiConnector.GetPokemonSubsetAsync` (tipos, stats, movimientos iniciales)
        - MutateState: ninguna mutación — es un workflow de consulta pre-captura
        - GenerateAdvice: LLM analiza qué pokemon conviene capturar considerando:
            - Equipo actual (tipos, coberturas, debilidades)
            - Pokemon disponibles en la ruta (tipos, stats comparativas)
            - Huecos de cobertura que se podrían llenar
            - Próximos gimnasios/rivales de la generación
        - Result.Data: datos de cada pokemon disponible (stats, tipos), estado del equipo actual
    - [ ] Tests: `RouteEncounterWorkflowTests.cs`
        - [ ] Validación: nuzlocke_id, route_name requeridos
        - [ ] Validación: falla si ruta ya tiene encounter registrado
        - [ ] FetchData: llama a GetPokemonSubsetAsync para cada pokemon disponible
        - [ ] MutateState: no aplica mutaciones
        - [ ] GenerateAdvice: prompt contiene equipo actual + pokemon disponibles
        - [ ] Result.Data contiene datos de pokemon disponibles
    - [ ] Registrar `RouteEncounterWorkflow` en DI (`Program.cs`)
    - [ ] Actualizar `app/workflow.md` con documentación del nuevo workflow
    - [ ] Tests manuales:
        - [ ] `curl POST /nuzlocke/workflow` con `route_encounter` — consulta exitosa con pokemon disponibles
        - [ ] `curl POST /nuzlocke/workflow` con `route_encounter` — ruta ya usada (error)

### Aprobación Sprint review
- [ ] Tests passing (172 existentes + nuevos)

### Riesgos
- [ ] Múltiples llamadas a PokeAPI en paralelo — considerar `Task.WhenAll` para eficiencia
- [ ] Lista de `available_pokemon` vacía — manejar como caso válido con consejo genérico
- [ ] Pokemon no encontrado en PokeAPI — manejar error individual sin fallar todo el workflow

### Fallos
-

### Sugerencias para el próximo Sprint
- [ ] **Workflow `item_obtained`**: Registro de objetos con consejo de uso
- [ ] **Workflow `manage_moves`**: Gestión de movimientos con análisis
- [ ] **Workflow `evolution`**: Evolución de pokemon con análisis de nuevas capacidades
- [ ] **Workflow `next_battle`**: Preparación pre-batalla
- [ ] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
