## Sprint Review [Calculadora de Daño Pokemon]

### Objetivos
- [ ] Crear modelo para DamageCalculation con resultado detallado (damage, effectiveness, isCritical)
- [ ] Implementar DamageCalculator service con fórmula Gen 1-5 de Pokemon
- [ ] Agregar MCP Tool: CalculateDamage - Calcular daño entre dos Pokemon
- [ ] Implementar cálculo de efectividad de tipos (type chart)
- [ ] Agregar soporte para movimientos físicos y especiales
- [ ] Considerar stats (Attack, Defense, Sp.Atk, Sp.Def) en cálculo
- [ ] Agregar tests unitarios para DamageCalculator (mínimo 5 escenarios)
- [ ] Integrar con PokeApiConnector para obtener datos de moves y Pokemon

### Aprobación Sprint review
- [ ] Fórmula de daño implementada correctamente según mecánicas Pokemon
- [ ] Type effectiveness chart completo (18 tipos)
- [ ] MCP Tool CalculateDamage funciona con datos reales de PokeAPI
- [ ] Tests cubren casos: super efectivo, no muy efectivo, inmune, crítico
- [ ] Damage calculator considera nivel, stats base y movimiento usado
- [ ] Todos los tests pasan (objetivo: 41+ tests)

### Riesgos
- Complejidad de la fórmula de daño Pokemon (múltiples generaciones con diferencias)
- Necesidad de stats base precisos desde PokeAPI para cálculos correctos
- Type chart puede tener edge cases (tipos duales, habilidades que modifican tipos)

### Fallos
- Ninguno (sprint aún no iniciado)

### Sugerencias para el próximo Sprint
- **Team analyzer tool**: Analizar fortalezas/debilidades del equipo completo
- **Battle simulator**: Simular combate completo turno por turno
- **Swap Pokemon tool**: Intercambiar Pokemon entre equipo y PC
- **Move recommender**: Sugerir mejores movimientos según matchup
