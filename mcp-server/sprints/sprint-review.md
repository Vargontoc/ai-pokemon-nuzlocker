## Sprint Review [26-02-2026-Workflow-Evolution]

### Objetivos
- [ ] **Workflow `evolution`** — Evolución de un pokemon con análisis LLM de nuevas capacidades
    - [ ] Parámetros:
        - [ ] `nuzlocke_id` (req)
        - [ ] `nickname` (req) — nickname del pokemon que evoluciona
        - [ ] `evolved_species` (req) — nombre de la nueva especie (ej: `"raichu"`)
        - [ ] `evolution_trigger` (opcional: `"level"` / `"stone"` / `"trade"` / `"other"`)
    - [ ] `FetchDataAsync`: obtener datos de la nueva especie desde PokeAPI (`tipos`, `estadísticas base`)
    - [ ] `MutateStateAsync`:
        - [ ] Actualizar `Species` del pokemon al nuevo nombre
        - [ ] Actualizar `Types` con los nuevos tipos
        - [ ] Recalcular estadísticas con la nueva base (mismo nivel, DVs y StatExp del pokemon)
        - [ ] Actualizar `MaxHP` con la nueva HP calculada
        - [ ] Guardar estado
    - [ ] `GenerateAdviceAsync`: análisis LLM de las nuevas capacidades (cambio de tipo, ganancias de estadísticas, sinergias con el equipo)
    - [ ] Registrar en `Program.cs`
    - [ ] Añadir ejemplo en `NuzlockeAgent.SystemPrompt`
    - [ ] Tests unitarios:
        - [ ] Evolución de pokemon en equipo: actualiza species, tipos y stats
        - [ ] Evolución de pokemon en PC: funciona correctamente
        - [ ] Pokemon no encontrado: devuelve error
        - [ ] PokeAPI no reconoce la especie: devuelve error
        - [ ] Stats recalculadas con nueva base y los DVs/StatExp existentes del pokemon
        - [ ] Genera advice con datos comparativos

### Aprobación Sprint review
- [ ] Tests passing (246 existentes + ~6 nuevos de evolution)

### Riesgos
- [ ] El usuario puede introducir el nombre de la especie con espacios o capitalización → **Mitigación**: normalizar igual que en `manage_moves` (lowercase + hyphens)
- [ ] Los DVs/StatExp del pokemon deben conservarse tras la evolución (no se resetean) → **Mitigación**: reutilizar los campos existentes del pokemon
- [ ] Un pokemon puede aprender un movimiento al evolucionar → **Mitigación**: fuera de scope de este sprint; el usuario puede llamar a `manage_moves` después

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
- [ ] **Workflow `use_item`**: El usuario indica que ha usado un objeto, se resta en inventario, se elimina si cantidad = 0 (salvo objetos clave o no consumibles). Si el objeto tiene efecto conocido, lo aplica.
- [ ] **Workflow `next_battle`**: Preparación pre-batalla
- [ ] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
