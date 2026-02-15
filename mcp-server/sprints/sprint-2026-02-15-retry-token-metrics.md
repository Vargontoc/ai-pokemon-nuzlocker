## Sprint Review [15-02-2026-Retry-TokenMetrics]

### Objetivos
- [x] **Retry/backoff para LLM**: Implementar política de reintentos con backoff exponencial en proveedores AI
  - `RetryHelper` compartido con backoff exponencial + jitter (`Providers/RetryHelper.cs`)
  - Integrado en Claude, OpenAI y Ollama (refactorizado de privado a compartido)
  - Config: `AiOptions.MaxRetries` (default 3), `AiOptions.TimeoutSeconds` (default 120)
  - 8 tests unitarios (`RetryHelperTests.cs`)
- [x] **Métricas de reducción de tokens**: Instrumentar métricas para validar la optimización del subset (>= 40%)
  - `PokeApiTools.GetPokemon` y `PokeApiPlugin.GetPokemonAsync` usan `GetPokemonSubsetAsync`
  - Logging `TokenMetrics` en `CachedPokeApiConnector` y `PokeApiConnector` (full vs subset bytes + %)
  - 3 tests unitarios (`TokenMetricsTests.cs`) — validación >= 40% reducción con fixture Pikachu

### Aprobación Sprint review
- [x] Compilación sin errores (0 errores, 2 warnings preexistentes)
- [x] Tests unitarios e integración relevantes pasan en verde (70/70)
- [ ] `NuzlockeAgent` responde de forma estable con y sin tools

### Riesgos
- [ ] El subset pierde abilities, height, weight, baseExperience — puede afectar respuestas del agente si necesita esos datos

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
- [ ] **NuzlockeAgent tests avanzados**: Tests de ciclo completo (con/sin tools), manejo de errores y timeouts
- [ ] **Calculadora de daño Gen 1**: Implementar como nuevo MCP tool
- [ ] **Team analysis tool**: Analizar debilidades del equipo y sugerir capturas
