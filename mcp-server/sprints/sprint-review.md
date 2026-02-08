## Sprint Review [09-02-2026-ef-migrations]

### Objetivos
- [ ] Agregar paquete EF Core Design tools
- [ ] Crear migración inicial para el esquema actual de la base de datos
- [ ] Actualizar Program.cs para usar Migrate() en lugar de EnsureCreated()
- [ ] Crear script SQL de migración para producción
- [ ] Documentar proceso de migraciones para futuros cambios de esquema

### Aprobación Sprint review
- [ ] Migraciones aplicadas exitosamente en base de datos limpia
- [ ] Tests pasan con el nuevo sistema de migraciones
- [ ] Documentación de migraciones completa

### Riesgos
- Conflictos con base de datos existente creada con EnsureCreated()
- Pérdida de datos durante migración

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
