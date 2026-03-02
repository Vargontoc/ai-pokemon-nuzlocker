## Sprint Review [2026-03-01-Nuzlocke-Dashboard]

### Objetivos
- [x] Revisar la documentación de la API. (Lectura completada y schemas base analizados vía puerto 5000)
- [x] Analizar y verificar el diseño segun lo descrito en project.md
- [x] Creación de vista Nuzlocke Dashboard (`DashboardView.vue` con Tailwind CSS y listado dinámico)
- [x] Creación de componente para listado de nuzlockes activos/finalizados (`NuzlockeCard.vue` con decoraciones visuales)
- [x] Revisión e integración con el sistema de rutas de Vue Router. (Ruta raíz funcionando y apuntando al Dashboard)
- [x] Implementar la abstracción de la información para la lista de nuzlockes (`nuzlockeService.ts`).

### Documentación
- [x] Documentación de la API. http://localhost:5000/swagger

### Aprobación Sprint review
- [x] Test unitarios pasan con exito

### Riesgos
- [x] Diseño adaptable para la lista de tarjetas (responsive) conseguido con `grid-cols-1 md:grid-cols-2 lg:grid-cols-3`

### Fallos
- Ninguno destacado. Las API subyacentes operan asincronamente sin bloquear la main thread del frontend. Se incluyó un Loading Spinner para mejorar respuesta en red lenta.

### Sugerencias para el próximo Sprint
- [x] Creación de la página principal Nuzlocke AI (Chat y Layout).
- [x] Integración de la pestaña y WebSocket.
