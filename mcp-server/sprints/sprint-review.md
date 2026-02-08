## Sprint Review [Health Checks Avanzados]

### Objetivos
- [ ] Agregar paquete Microsoft.Extensions.Diagnostics.HealthChecks
- [ ] Implementar health check para base de datos SQLite
- [ ] Implementar health check para conectividad con PokeApi
- [ ] Crear endpoint /health/ready para readiness checks
- [ ] Crear endpoint /health/live para liveness checks
- [ ] Agregar tests para los health checks implementados

### Aprobación Sprint review
- [ ] Endpoint /health retorna estado agregado de todos los checks
- [ ] Endpoint /health/ready verifica que DB y PokeApi están disponibles
- [ ] Endpoint /health/live verifica que la aplicación está corriendo
- [ ] Tests verifican comportamiento de health checks (healthy/unhealthy)
- [ ] Respuestas JSON incluyen detalles de cada check individual

### Riesgos
- Health checks muy frecuentes pueden impactar rendimiento
- Timeouts de checks pueden causar falsos negativos
- Exponer información sensible en respuestas de health checks

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
