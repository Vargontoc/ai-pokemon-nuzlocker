## Sprint Review [25-02-2026-Workflow-ManageMoves]

### Objetivos
- [x] **Workflow `manage_moves`** — dos vertientes en un mismo workflow
    - [x] **Vertiente A — Registro de movimientos actuales** (mutación pura, sin LLM):
        - [x] Parámetros: `nuzlocke_id` (req), `nickname` (req), `moves` (req, lista de 1-4 nombres de movimiento)
        - [x] Buscar el pokemon por `nickname` en equipo y PC
        - [x] Actualizar la lista de movimientos en el estado y guardar
        - [x] Devolver `WorkflowResult` con la mutación descriptiva
    - [x] **Vertiente B — Aprender un nuevo movimiento** (con análisis LLM):
        - [x] Parámetros adicionales: `learn_move` (nombre del movimiento a aprender), `forget_move` (opcional, el que se olvida), `learn_source` (opcional: `"level"` / `"tm"` / `"hm"`)
        - [x] `FetchDataAsync`: obtener datos del movimiento nuevo desde PokeAPI (`tipo`, `categoría`, `potencia`, `precisión`, `PP`, `efecto`)
        - [x] Si se indica `forget_move`, también obtener sus datos para comparativa
        - [x] `MutateStateAsync`: aplicar el cambio de movimiento en el pokemon
        - [x] `GenerateAdviceAsync`: análisis del movimiento aprendido en el contexto del equipo actual
    - [x] El workflow detecta automáticamente la vertiente: si viene `learn_move` → Vertiente B; si solo viene `moves` → Vertiente A
    - [x] Registrar en `Program.cs`
    - [x] Añadir ejemplo en `NuzlockeAgent.SystemPrompt`
    - [x] Tests unitarios:
        - [x] Vertiente A: actualiza movimientos correctamente (team y PC)
        - [x] Vertiente A: falla si el pokemon no se encuentra
        - [x] Vertiente A: falla si `moves` está vacío o supera 4
        - [x] Vertiente B: obtiene datos del movimiento y muta el estado
        - [x] Vertiente B: con `forget_move` substituye correctamente
        - [x] Vertiente B: genera advice con los datos del movimiento

### Aprobación Sprint review
- [x] Tests passing (240 existentes + 6 nuevos de manage_moves = **246 tests** ✓)

### Riesgos
- [x] PokeAPI puede no reconocer nombres de movimiento con espacios o caracteres especiales → `NormalizeName` normaliza a lowercase con guiones (`"thunder shock"` → `"thunder-shock"`)
- [x] Un pokemon en PC no tiene sentido aprender un movimiento por combate → Operación permitida igualmente (tracker manual)

### Fallos
- Ninguno

### Sugerencias para el próximo Sprint
- [ ] **Workflow `use_item`**: El usuario indica que ha usado un objeto, se resta en inventario se elimina si es 0 (salvo objetos clave o no consumibles). Y si conoce el efecto ejecuta dicho efecto.
- [ ] **Workflow `evolution`**: Evolución de pokemon con análisis de nuevas capacidades
- [ ] **Workflow `next_battle`**: Preparación pre-batalla
- [ ] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [ ] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [ ] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
