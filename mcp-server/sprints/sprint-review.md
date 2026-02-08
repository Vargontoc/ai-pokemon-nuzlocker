## Sprint Review [Logging Estructurado con Serilog]

### Objetivos
- [ ] Agregar paquete Serilog y extensiones (Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.File)
- [ ] Configurar Serilog en Program.cs con WriteTo Console y File
- [ ] Implementar structured logging en PokeApiConnector con propiedades contextuales
- [ ] Agregar enrichers de contexto (Machine, Environment, Request)
- [ ] Configurar diferentes niveles de log por entorno (Development vs Production)
- [ ] Actualizar appsettings para configuración de Serilog

### Aprobación Sprint review
- [ ] Logs se escriben en formato estructurado JSON
- [ ] Console sink muestra logs en desarrollo
- [ ] File sink escribe logs a archivo rotativo en producción
- [ ] Logs incluyen propiedades contextuales (Timestamp, Level, Properties)
- [ ] Configuración separada por entorno (appsettings.Development.json vs Production)
- [ ] Tests pasan sin regresiones

### Riesgos
- Archivos de log pueden crecer y llenar disco
- Logs excesivos pueden impactar rendimiento
- Información sensible en logs (tokens, passwords)

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
