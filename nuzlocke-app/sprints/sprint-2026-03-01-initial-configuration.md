## Sprint Review [2026-03-01-Initial-configuration]

### Objetivos
- [x] Agregar fichero de configuración para el proyecto. Se configurara por ejemplo ruta servidor.
- [x] Tambien posibilidad de configurar .env
- [x] Implementar sistema toogle theme (dark/light) 
- [x] Implementar internacionalizacion (Español/Ingles)
- [x] ChackHealth del servidor, con interceptores:
  - Si no hay conexión con el servidor llevara a una vista de error con un loader. Internamente guardara el estado anterior.
  - Si hay conexión con el servidor llevara a la vista anterior. Si no hubiera almacenada seria la vista dashboard de nuzlockes.

### Aprobación Sprint review
- [x] Test unitarios pasan con exito

### Riesgos

### Fallos

### Sugerencias para el próximo Sprint
- [x] Creación de vista Nuzlocke Dashboard.
- [x] Creación de componente para listado de nuzlockes activos/finalizados.
- [x] Integración con el sistema de rutas de Vue Router.
- [x] Sugerencia 1: Refactorización posterior si el dashboard crece.
