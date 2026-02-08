## Sprint Review [Configuración de Entorno]

### Objetivos
- [ ] Crear appsettings.json con configuración base (ConnectionStrings, Logging, PokeApi URL)
- [ ] Implementar appsettings.Development.json y appsettings.Production.json
- [ ] Agregar soporte para variables de entorno (.env) usando dotenv o similar
- [ ] Configurar diferentes cadenas de conexión para Development/Production
- [ ] Actualizar Program.cs para leer configuración desde appsettings
- [ ] Actualizar tests para usar configuración de test independiente

### Aprobación Sprint review
- [ ] Aplicación lee configuración correctamente desde appsettings.json
- [ ] Variables de entorno sobrescriben valores de appsettings cuando están presentes
- [ ] Tests pasan con configuración independiente
- [ ] Documentación actualizada con instrucciones de configuración

### Riesgos
- Exponer secretos en archivos de configuración
- Conflictos entre diferentes fuentes de configuración (appsettings vs env vars)

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
