## Sprint Review [26-02-2026-Workflow-Evolution]

### Objetivos
- [x] **Workflow `evolution`** — Evolución de un pokemon con análisis LLM de nuevas capacidades
    - [x] Parámetros:
        - [x] `nuzlocke_id` (req)
        - [x] `nickname` (req) — nickname del pokemon que evoluciona
        - [x] `evolved_species` (req) — nombre de la nueva especie (ej: `"raichu"`)
        - [x] `evolution_trigger` (opcional: `"level"` / `"stone"` / `"trade"` / `"other"`)
    - [x] `FetchDataAsync`: obtener datos de la nueva especie desde PokeAPI (`tipos`, `estadísticas base`)
    - [x] `MutateStateAsync`:
        - [x] Actualizar `Species` del pokemon al nuevo nombre
        - [x] Actualizar `Types` con los nuevos tipos (campo añadido a `TeamMember` y `StoredPokemon`)
        - [x] Recalcular estadísticas con la nueva base (mismo nivel, DVs y StatExp del pokemon)
        - [x] Actualizar `MaxHP` con la nueva HP calculada
        - [x] Guardar estado
    - [x] `GenerateAdviceAsync`: análisis LLM de las nuevas capacidades (cambio de tipo, ganancias de estadísticas, sinergias con el equipo)
    - [x] Registrar en `Program.cs`
    - [x] Añadir ejemplo en `NuzlockeAgent.SystemPrompt`
    - [x] Tests unitarios:
        - [x] Evolución de pokemon en equipo: actualiza species, tipos y stats
        - [x] Evolución de pokemon en PC: funciona correctamente
        - [x] Pokemon no encontrado: devuelve error
        - [x] PokeAPI no reconoce la especie: devuelve error
        - [x] Stats recalculadas con nueva base y los DVs/StatExp existentes del pokemon
        - [x] Genera advice con datos comparativos

### Aprobación Sprint review
- [x] Tests passing (246 existentes + 6 nuevos de evolution = **252 tests** ✓)

### Riesgos
- [x] El usuario puede introducir el nombre de la especie con espacios o capitalización → `NormalizeName` resuelto (mismo patrón que `manage_moves`)
- [x] Los DVs/StatExp del pokemon se conservan tras la evolución (se reutilizan del pokemon existente)
- [x] Un pokemon puede aprender un movimiento al evolucionar → fuera de scope; el usuario llama a `manage_moves` después

### Fallos
- Ninguno

### Sugerencias para el próximo Sprint
- [ ] **Workflow `use_item`**: El usuario indica que ha usado un objeto, se resta en inventario, se elimina si cantidad = 0 (salvo objetos clave o no consumibles). Si el objeto tiene efecto conocido, lo aplica.
- [ ] **Workflow `next_battle`**: Preparación pre-batalla
- [ ] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
