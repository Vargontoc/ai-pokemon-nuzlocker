## Sprint Review [2026-03-02-Nuzlocke-AI-Screen] (Completed)

### Objetivos Completados
- [x] Creación de vista Nuzlocke AI (Esqueleto UI inicial `NuzlockeView.vue`).
- [x] En este sprint solo la vista vacía indicando que faltan componentes.
- [x] Recuperación real de datos de la sesión mapeando la ID desde la ruta URL parametrizada `vue-router`.
- [x] Creación de `useNuzlockeSocket.ts` abstraiendo el manejo seguro de WebSockets nativo ligando su ciclo de vida al onMounted y onUnmounted de Vue 3.
- [x] Eventos de conexión mostrados mediante alertas visuales de `vue3-toastify`.

### Aprobación Sprint review
- [x] Test unitarios de `DashboardView` y el propio WebSockets (`useNuzlockeSocket.spec.ts`) validados pasando con éxito la test suite simulando Handshakes.

### Sugerencias para el próximo Sprint
- [ ] Creación de Layout divisor en el `NuzlockeView` (Pantalla del Juego y Pantalla del Chat).
- [ ] Creación de componentes hijo (PC, Inventario, Cementerio) estáticos consumiendo Pinia Store.
- [ ] Lógica específica para procesar mensajes de Chat desde la UI y desde el evento interno `socket.onmessage`.
- [ ] Interfaz estilo "Visual Novel" del juego (Diálogos simulados) o Log de combate.
