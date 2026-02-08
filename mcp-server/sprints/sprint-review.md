## Sprint Review [10-02-2026-sqlite-cache]

### Objetivos
- [ ] Diseñar esquema de base de datos SQLite para caché
- [ ] Implementar repositorio SQLite para almacenar Pokémon
- [ ] Implementar repositorio SQLite para almacenar Movimientos
- [ ] Implementar repositorio SQLite para almacenar Tipos
- [ ] Implementar repositorio SQLite para almacenar Habilidades
- [ ] Integrar caché con PokeApiConnector (priorizar SQLite sobre PokeApi)
- [ ] Agregar estrategia de invalidación/actualización de caché

### Aprobación Sprint review
- [ ] Pasar tests de integración con SQLite
- [ ] Validar priorización: SQLite → PokeApi → User input
- [ ] Verificar reducción de llamadas a PokeApi

### Riesgos
- Complejidad del esquema de base de datos para datos anidados de PokeApi
- Sincronización entre caché y PokeApi

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
