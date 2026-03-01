## Sprint Review [DD-MM-YYYY-Initial-configuration]

### Objetivos
- [] Agregar fichero de configuración para el proyecto. Se configurara por ejemplo ruta servidor.
- [] Tambien posibilidad de configurar .env
- [] Implementar sistema toogle theme (dark/light) 
- [] Implementar internacionalizacion (Español/Ingles)
- [] ChackHealth del servidor, con interceptores:
  - Si no hay conexión con el servidor llevara a una vista de error con un loader. Internamente guardara el estado anterior.
  - Si hay conexión con el servidor llevara a la vista anterior. Si no hubiera almacenada seria la vista dashboard de nuzlockes.

### Aprobación Sprint review
- [ ] Test unitarios pasan con exito

### Riesgos

### Fallos

### Sugerencias para el próximo Sprint
- [ ] Creación de vista Nuzlocke Dashboard.
- [ ] Creación de componente para listado de nuzlockes activos/finalizados.
- [ ] Integración con el sistema de rutas de Vue Router.
- [ ] Sugerencia 1: Refactorización posterior si el dashboard crece.
