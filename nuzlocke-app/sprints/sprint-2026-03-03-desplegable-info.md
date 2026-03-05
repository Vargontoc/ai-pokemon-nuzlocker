## Sprint Review [2026-03-03-Desplegable-Info]

### Objetivos
- [ ] La linea de separación de la  seccion de juego y la del chart que pueda ser desplazada por el usuario.
- [ ] Desplegable junto al recuadro Game Window
- [ ] El desplegable sera un panel pequeño con tres opciones (PC, Inventario, Graveyard)
- [ ] Al pulsar sobre una de ellas obtendra la información requerida llamando a los endpoints correspondientes
- [ ] Al obtener la informacion se renderizaran en un modal pop-up en diseño de grilla.
    - [ ] IconCard para el inventario, con el nombre del item y la cantidad. 
    - [ ] PokemonCard para el PC y Graveyard. Se mostrará el especie, nombre, nivel y una imagen del pokemon. 

### Aprobación Sprint review
- [ ] Test unitarios validando la emisión del chat usando la UI.
- [ ] Test unitarios comprobando la renderización de mensajes desde el servidor al cliente.

### Riesgos
- [ ] Lógica compleja de scrolling en el historial del chat al agolparse mensajes rápidos.
- [ ] Dependencia no instalada de Pinia si se elige esa ruta.

### Sugerencias para iteraciones futuras
- [ ] Implementar la Interfaz de Chat interactiva en la parte inferior derecha:
    - [ ] Caja de texto `ChatInput` funcional (poder escribir mensajes y enviarlos a través del WebSocket `sendMessage`).
    - [ ] Listado/Área superior del chat con Auto-Scroll para renderizar la conversación entre el Jugador y el "Maestro IA".
- [ ] Parsear mensajes de WebSockets (DTO `onmessage`) como eventos de Frontend e inyectarlos en la UI de la lista de Chat.
- [ ] Panel Lateral / Pestañas (Party, PC, Graveyard):
    - [ ] Configurar un sistema de State Management (ej. `Pinia`) o Composition Local para el estado Global de los Pokémon.
    - [ ] Componente visual de lista o Grid para mostrar equipos simulados.
- [ ] Vía Emulador Web Integrado (EmulatorJS).
- [ ] Vía Desktop App (Electron.js / Tauri) permitiendo `transparent: true`.
- [ ] TTS interactivo leyendo los mensajes en voz alta.
