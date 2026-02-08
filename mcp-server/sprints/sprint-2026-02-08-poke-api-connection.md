## Sprint Review [08-02-2026-poke-api-connection]

### Objetivos
- [x] Conexión connector con PokeApi

### Aprobación Sprint review
- [x] Pasar tests (10/10 tests pasando)

### Riesgos
- Ninguno identificado

### Fallos
- Problema inicial con HttpClient.BaseAddress corregido (usando URLs completas en su lugar)

### Sugerencias para el próximo Sprint
- **Cache SQLite:** Implementar sistema de caché local en SQLite para almacenar datos de Pokémon, movimientos, tipos y habilidades consultados
- **CRUD Estado de Partida:** Desarrollar sistema de gestión de estado del juego (JSON) con operaciones CRUD para equipo actual, Pokémon capturados y Pokémon muertos
- **Calculadora de Daño:** Implementar calculadora que permita calcular el daño entre Pokémon considerando tipos, movimientos y estadísticas
- **Endpoints MCP:** Crear endpoints MCP para exponer las funcionalidades del conector y caché a clientes externos