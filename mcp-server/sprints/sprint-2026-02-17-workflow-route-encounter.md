## Sprint Review [16-02-2026-Workflow-Route-Encounter]

### Objetivos
- [x] Implementar workflow `route_encounter`
    - [x] `Workflows/Gameplay/RouteEncounterWorkflow.cs`
        - Input: `nuzlocke_id` (string), `route_name` (string), `available_pokemon` (string[], lista de especies posibles en la ruta)
        - Validación: nuzlocke_id y route_name requeridos, nuzlocke_id debe existir, ruta no debe tener encounter previo
        - FetchData: obtener datos de todos los pokemon disponibles via `IPokeApiConnector.GetPokemonSubsetAsync` (tipos, stats, movimientos iniciales)
        - MutateState: ninguna mutación — es un workflow de consulta pre-captura
        - GenerateAdvice: LLM analiza qué pokemon conviene capturar considerando:
            - Equipo actual (tipos, coberturas, debilidades)
            - Pokemon disponibles en la ruta (tipos, stats comparativas)
            - Pokemon disponibles en el pc (tipos, stats comparativas)
            - Huecos de cobertura que se podrían llenar
        - Result.Data: datos de cada pokemon disponible (stats, tipos), estado del equipo actual
    - [x] Tests: `RouteEncounterWorkflowTests.cs`
        - [x] Validación: nuzlocke_id, route_name requeridos
        - [x] Validación: falla si ruta ya tiene encounter registrado
        - [x] FetchData: llama a GetPokemonSubsetAsync para cada pokemon disponible
        - [x] FetchData: lista vacía de pokemon sigue funcionando
        - [x] FetchData: pokemon no encontrado en PokeAPI se omite sin fallar
        - [x] MutateState: no aplica mutaciones
        - [x] GenerateAdvice: prompt contiene equipo actual + pokemon disponibles + PC
        - [x] Result.Data contiene datos de pokemon disponibles y route_name
        - [x] ExecuteDeterministicAsync devuelve prompts sin llamar al LLM
    - [x] Registrar `RouteEncounterWorkflow` en DI (`Program.cs`)
    - [x] Actualizar `app/workflow.md` con documentación del nuevo workflow
    - [x] Tests manuales:
        - [x] `curl POST /nuzlocke/workflow` con `route_encounter` — consulta exitosa con pokemon disponibles
        - [x] `curl POST /nuzlocke/workflow` con `route_encounter` — ruta ya usada (error)

### Aprobación Sprint review
- [x] 186/186 tests passing (172 existentes + 14 nuevos)

### Riesgos
- [x] Múltiples llamadas a PokeAPI — resuelto: secuencial (DbContext no es thread-safe), cache L1/L2 minimiza latencia
- [x] Lista de `available_pokemon` vacía — resuelto: se trata como caso válido, IA da consejo genérico
- [x] Pokemon no encontrado en PokeAPI — resuelto: se omite con warning log sin fallar el workflow

### Fallos
- Ninguno

### Sugerencias para el próximo Sprint
- [ ] **Workflow `item_obtained`**: Registro de objetos con consejo de uso
- [ ] **Workflow `manage_moves`**: Gestión de movimientos con análisis
- [ ] **Workflow `evolution`**: Evolución de pokemon con análisis de nuevas capacidades
- [ ] **Workflow `next_battle`**: Preparación pre-batalla
- [ ] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
