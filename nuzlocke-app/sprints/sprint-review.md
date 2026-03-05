## Sprint Review [2026-03-xx-Chat-Events]

### Objetivos
- [ ] Implementar la renderización dinámica y estilizada de los diferentes **Eventos** del Nuzlocke en el componente de Chat (`ChatMessageBubble.vue` u otros dedicados).
- [ ] Mapear los identificadores únicos de evento que enviará el backend (ej. inicio de batalla, uso de objeto, captura, muerte de un Pokémon, etc.) a plantillas visuales específicas dentro del historial de mensajes (ej. colores de fondo diferenciados, iconos representativos, avatares o sprites si procede).
- [ ] Asegurarse de que el renderizado de estos eventos interactúe armónicamente con la burbuja de texto genérica del Maestro IA y del Usuario.
- [ ] Documentar o definir la lista de **eventIds** principales soportados por el frontend en este sprint, alineado con el motor del emulador.

### Documentacion
- [ ] En localhost:5000/swagger se pueden encontrar los distintos eventos que puede recibir.

### Aprobación Sprint review
- [ ] Verificación visual de los componentes de eventos mutando mediante test controlados o inyectando los WS payloads manualmente.

### Riesgos
- [ ] La variedad de eventos y payloads puede complicar el parseo si la estructura de datos que viaja por el socket desde el agente de PokeAPI varía considerablemente entre un evento y otro.

### Sugerencias para iteraciones futuras
- [ ] Panel Lateral / Pestañas (Party, PC, Graveyard) mediante un estado global como Pinia (ahora mismo usando Axios directos al Backend REST temporalmente, convendría centralizar).
- [ ] Vía Emulador Web Integrado (EmulatorJS) inyectado en la Game Window.
- [ ] TTS activo y reactivo leyendo los mensajes de la IA por voz en el navegador.
