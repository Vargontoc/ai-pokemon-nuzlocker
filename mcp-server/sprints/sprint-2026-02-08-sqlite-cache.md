## Sprint Review [08-02-2026-sqlite-cache]

### Objetivos
- [x] Diseñar esquema de base de datos SQLite para caché
- [x] Implementar repositorio SQLite para almacenar Pokémon
- [x] Implementar repositorio SQLite para almacenar Movimientos
- [x] Implementar repositorio SQLite para almacenar Tipos
- [x] Implementar repositorio SQLite para almacenar Habilidades
- [x] Integrar caché con PokeApiConnector (priorizar SQLite sobre PokeApi)
- [x] Agregar estrategia de invalidación/actualización de caché

### Aprobación Sprint review
- [x] Pasar tests de integración con SQLite (22/22 tests pasando)
- [x] Validar priorización: SQLite → PokeApi → User input (implementado en CachedPokeApiConnector)
- [x] Verificar reducción de llamadas a PokeApi (caché funcional, consultas subsecuentes no llaman a API)

### Riesgos
- Resuelto: Usamos JSON para almacenar datos complejos, simplificando el esquema

### Fallos
- Ninguno

### Sugerencias para el próximo Sprint
- **CRUD Estado de Partida:** Desarrollar sistema de gestión de estado del juego (JSON) para tracking de equipo, Pokémon capturados y muertos
- **Calculadora de Daño:** Implementar calculadora de daño considerando tipos, stats, movimientos y modificadores
- **Migrations:** Agregar sistema de migraciones EF Core para actualizaciones de esquema
- **Cache Expiration:** Implementar expiración temporal de caché basada en TTL (Time To Live)
