## Sprint Review [2026-03-xx-Chat-Interface]

### Objetivos
- [x] Implementar la **Interfaz de Chat Interactiva** en el panel lateral derecho (`aside`) de la vista principal `NuzlockeView.vue`.
- [x] Construir un componente de caja de texto o `ChatInput` funcional (poder escribir prompts/mensajes y enviarlos a la IA a través de la función `sendMessage` del WebSocket).
- [x] Habilitar un área superior/listado dentro del panel derecho para renderizar la conversación entre el Jugador y el "Maestro IA" (burbujas de chat separadas visualmente).
- [x] Implementar **Auto-Scroll** al fondo de la conversación cuando llegan nuevos mensajes.
- [x] Extender el *composable* `useNuzlockeSocket` para que exponga de forma reactiva el stack de mensajes (`ref<Message[]>`) al atrapar eventos `onmessage`.
- [x] Cabecera del chat interactivo tiene un pequeño desplegable con selección de idiomas de como quiere la respuesta de la IA, los idiomas disponibles son: es-ES, en-US, pt-BR, en-GB, it-IT, fr-FR (a ser posible con nicono de banderas)
- [x] En la misma cabecera también hay una flag para indicar si la rom está en modo exploración o batalla. Esto será dos chats independientes. El chat de exploración será el principal y el de batalla será un chat secundario que se mostrará cuando el usuario decida entrar en batalla. Esto será automatico, cuando reciba el evento correspondiente de entrar en batalla, no puede cambiarse al chat de exploración hasta que reciba el evento de salir de batalla. Y lo mismo cuando refresca la ventana tendra que saber en que estado esta la rom.
- [x] El input del mensaje de envio por parte del usuario, escribirá y se bloqueara hasta recibir el evento de respuesta final, o reciba un error tanto http o de evento.
- [x] Cada evento tiene un identificador, preparara el componente de renderizado para hacerlo customizable por ejemplo que pueda mostrar un oicono o que tenga un sombreado especifico.  Esta implementación se ira haciendo identificador a identificador.
- [x] Comunicación: 
 - [x] Envio del mensaje es con el endpoint `/nuzlocke/advice`  que contiene como parametrica el nuzlockeid y como opcional el idioma.
 - [x] Se pinta en el chat lo que escribe el usuario, con su propio estilo para diferenciarlo.
 - [x] Desde backend mandara eventos al websocket con el identificador correspondiente y el contenido, y es lo que ira pintando en el chat.


### Aprobación Sprint review
- [x] Test unitarios validando la emisión del input text hacia el composable `useNuzlockeSocket`.
- [x] Test unitarios comprobando la renderización de nuevos nodos del DOM al recibir payloads remotos del servidor emulados en el cliente.

### Riesgos
- [ ] Lógica compleja de scrolling automatizado cuando se agolpan mensajes rápidos de la IA y el usuario, evitando tirones en la interfaz.

### Sugerencias para iteraciones futuras
- [ ] Renderizar correctamente los eventos en el chat.
- [ ] Vía Emulador Web Integrado (EmulatorJS) inyectado en la Game Window.
- [ ] TTS activo y reactivo leyendo los mensajes de la IA por voz en el navegador.
