## Sprint Review [03-03-2026-refactor-nuzlockes-manager]

### Objetivos
- [x] Refactorización de gestión de nuzlockes.
- [x] La ruta de directorio de los lockes se configurara por variable de entorno o configuración settings
- [x] Se almacenará en base datos SQLite
- [x] Agregar endpoints necesarios
    - [x] POST /nuzlocke
        - [x] Entidad de base datos -> NuzlockeMetadata
            - Id: Guid auto generado
            - Name: Nombre o titulo que le da el usuario (no permite vacio y más 50 caracteres)
            - Descripcion: Opcional, de máximo 100 caracteres
            - BasePath: (deprecated) se usa la ruta de configuración
            - Generation: (1-10), por defecto 1 que es la actual implementación
            - LockeType: Tipo de lock, por defecto standard, convertido en enum `LockeType { Standard, Hardcore }`
            - IsInitialized: comprueba que la estructura del locke se ha generado
            - Status: Estado del locke, agregado Building para cuando no esté inicializado
            - CreatedAt: Fecha de creación
            - LastUpdated: Fecha de ultima actualización
    - [x] GET /nuzlocke
        - [x] Retorna lista NuzlockeMetadata
    - [x] GET /nuzlocke/{id}
        - [x] Retorna NuzlockeMetadata
    - [x] PUT /nuzlocke/{id}/state
        - [x] Actualiza el estado del locke. Es el único campo que se puede modificar.
    - [x] DELETE /nuzlocke/{id}
        - [x] Borra tanto el registro como la estructura de directorios del locke.

- [x] Refactorización conexión websocket
    - [x] Refactorizar endpoint /ws/advice?nuzlockeId
        - [x] InitNuzlockeWorkflow como workflow de mutación pura (sin llamada al LLM)
        - [x] Estructura de directorios: {NuzlockeBasePath}/{nuzlockeId}/.nuzlocke, game_state.json, memory/agent.json

### Aprobación Sprint review
- [x] 224 tests pasan en verde correctamente

### Riesgos
- Ninguno

### Fallos
- Ninguno

### Sugerencias para el próximo Sprint
- [x] **Workflow `use_item`**: El usuario indica que ha usado un objeto, se resta en inventario, se elimina si cantidad = 0 (salvo objetos clave o no consumibles). Si el objeto tiene efecto conocido, lo aplica.
- [x] **Workflow `next_battle`**: Preparación pre-batalla
- [x] **Workflow `start_battle`**: Inicio de batalla con análisis estratégico del oponente
- [x] **Workflow `next_turn`**: Turno de batalla con log y consejo táctico
- [x] **Workflow `end_battle`**: Cierre de batalla con BattleRecord + bajas
