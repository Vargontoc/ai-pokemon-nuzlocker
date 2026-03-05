## Sprint Review [2026-03-03-Chat-Interface]

### Objetivos
- [x] Renderizar siguiente sección, party.
- [x] Se dispondrá debajo de la ventana del juego. Se renderizarán 6 criculos en forma de pokeball.
- [x] Estos circulos estaran alineados a la posición y anchura de la ventana de renderización de la rom.
- [x] En cada uno de estos circulos se renderizará la imagen del pokemon y su nombre debajo de la imagen del pokemon.
- [x] El circulo que contiene el pokemon tendra animaciones y distintos colores segun el estado del Pokemon. Si esta envenenado sera morado, quemado rojo, paralizado amarillo, dormido azul, congelado blanco. En caso de derrotado sera gris y la imagen del pokemon translucida.
- [x] En cada imagen del pokemon habra un hover que mostrara las estadisticas en forma de barras del pokemon, con colores segun el valor de la estadistica (verde, amarillo, naranja,rojo).
- [x] En ese pequeño modal hover aparecera su vida y nivel. A futuro tendra informacion de la naturaleza y la habilidad. En estta primera versión solo lo descrito.  
- [x] La llamada a la API para obtener la información del equipo se hará la cuando se conecte al websocket (conexiçon con exito).
- [x] Si no hubiera miembros en la party, los circulos se renderizan igual solo que con un fondo gris y un texto "-"

### Aprobación Sprint review
- [ ] Test unitarios validando la emisión del input text hacia el composable `useNuzlockeSocket`.
- [ ] Test unitarios comprobando la renderización de nuevos nodos del DOM al recibir payloads remotos del servidor emulados en el cliente.

### Riesgos
- [ ] Lógica compleja de scrolling automatizado cuando se agolpan mensajes rápidos de la IA y el usuario, evitando tirones en la interfaz.

### Sugerencias para iteraciones futuras
- [ ] Implementar la **Interfaz de Chat Interactiva** en el panel lateral derecho (`aside`) de la vista principal `NuzlockeView.vue`.
- [ ] Construir un componente de caja de texto o `ChatInput` funcional (poder escribir prompts/mensajes y enviarlos a la IA a través de la función `sendMessage` del WebSocket).
- [ ] Habilitar un área superior/listado dentro del panel derecho para renderizar la conversación entre el Jugador y el "Maestro IA" (burbujas de chat separadas visualmente).
- [ ] Implementar **Auto-Scroll** al fondo de la conversación cuando llegan nuevos mensajes.
- [ ] Extender el *composable* `useNuzlockeSocket` para que exponga de forma reactiva el stack de mensajes (`ref<Message[]>`) al atrapar eventos `onmessage`.
- [ ] Panel Lateral / Pestañas (Party, PC, Graveyard) mediante un estado global como Pinia (ahora mismo usando Axios directos al Backend REST temporalmente, convendría centralizar).
- [ ] Vía Emulador Web Integrado (EmulatorJS) inyectado en la Game Window.
- [ ] TTS activo y reactivo leyendo los mensajes de la IA por voz en el navegador.
