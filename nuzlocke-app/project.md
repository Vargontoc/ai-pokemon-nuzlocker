## Project AI Nuzlocke App

# Descipcion
Proyecto componente web bajo framework Vue 3 + TypeScript + TailwindCSS. Es un aplicativo que se conectará a un servidor que contiene un agente de AI que ayudará al jugador a tomar decisiones durante su partida de Pokemon. El servidor se comunica con el frontend mediante WebSocket y REST API. El proyecto es ligero de facil usabilidad. A continuación describiremos los siguiente apartados del proyecto.

# Tecnologias
- Vue 3 + TypeScript + TailwindCSS
- WebSocket
- REST API

# Documentación 
- API Doc:  http://localhost:8000/swagger 

# Views: 
- Dashboard Nuzlockes:
  - Vista en la cual se listará los nuzlockes activos y finalizados.
  - Botón para crear un nuevo nuzlocke.
    - Al crear permite al usuario configurar la ruta donde se guardará el nuzlocke, la generación y el tipo de nuzlocke. Tambien Podra poner un nombre y descripcion al nuzlocke.
  - Al entrar en un nuzlocke se creará una nueva sesión de websocket y se navegará a la vista de Nuzlocke AI
  - Al entrar en la sesión debe matar las existentes de ese nuzlocke y quedando la seleccionada por el usuario activa.
  - Boton para borrar nuzlockes.
- Nuzlocke AI:
  - Vista donde se renderizará el nuzlocke activo, consta de varias partes.
  - En la ruta donde configurao el usuario que guardaba el nuzlocke, estara el fichero game_state.json donde vendra toda la información del nuzlocke. 
  - Diviremos la ventana en dos pestañas principales ambas visibles
    - Pestaña de juego
      - Cuadro con marco con un estilo moderno futurista con tonalidades electricas y animaciones, son fondo transparente donde se mostraá el juego que esta haciendo nuzlocke el usuario.
      - En un lateral del cuadro tendra un desplegable con tres secciones (PC, Inventario, Cementerio). Al pulsar en cada uno de ellos se abrira un popup con la informacion correspondiente.
      - Debajo del cuadro de juego estará la sección del equipo actual que tiene el usuario. Se representará por 6 circulos renderizados como si fueran pokeballs con la imagen del pokemon y su nombre debajo.
      - Al hacer hover sobre cada circulo se mostrara la visa y estadisticas del pokemon, su nivel, objeto equipado y estado. Las estadisticas se renderizará con una barra de progreso de colores segun el valor de la estadistica. (rojo, amarillo, naranja, verde)
    - Pestaña de AI
      - En esta sección se mostrará la conversación entre el usuario y la IA.
      - Es un componente chat donde recibira los eventos del websocket y mostrará la conversación que proporcione la IA.
      - En la cabecera del chat habra un desplegable con idiomas el cual el usaurio podra seleccionar la respuesta del agente.
      - Como el tiempo de respuesta de la IA puede ser largo, se bloqueará hasta que reciba el evento advice_end.
      - Durante este proceso recibirá eventos de que workflows y tools está usando el agente. Recibira el evento de usar el workflow con su id y el resultado tanto exito como error al hacer el workflow.
      - Se propone para una mayor visualización que los mensajes con workflow tengan un icono delante del mensaje y que el mensaje tenga un color de fondo diferente.
      - Los mensajes con tools tendrán otro icono y color de fondo diferente.
      - Los mensajes con advice tendrán otro icono y color de fondo diferente.
      - Los mensajes con advice_end tendrán otro icono y color de fondo diferente.

# Flow

Flujo del sistema — AI Pokemon Nuzlocker
1. Conexión WebSocket (hacer primero, una sola vez)

WS ws://localhost:5000/ws/advice?sessionId={nuzlockeId}
Mantener la conexión abierta durante toda la sesión. El nuzlockeId es el identificador del nuzlocke activo.

2. Iniciar un Nuzlocke

POST /nuzlocke/workflow
{
  "workflowId": "init_nuzlocke",
  "sessionId": "cualquier-id",
  "basePath": "/ruta/donde/guardar",
  "generation": 1,
  "lockeType": "standard"
}
Respuesta HTTP:


{
  "success": true,
  "workflowId": "init_nuzlocke",
  "data": { "nuzlockeId": "abc123_2026-02-22" },
  "mutations": [...],
  "errors": []
}
El nuzlockeId devuelto en data es el identificador permanente que el frontend debe guardar y usar en todas las llamadas posteriores.

WebSocket recibe simultáneamente:


{ "type": "workflow_event", "workflowId": "init_nuzlocke", "success": true, "mutations": [...], "data": {...} }
3. Enviar comandos (workflows)
Todos siguen el mismo patrón con POST /nuzlocke/workflow:

workflowId	Cuándo usarlo	Parámetros clave
capture_pokemon	El jugador captura un Pokémon	nuzlocke_id, species, nickname, location, level
route_encounter	El jugador entra a una ruta nueva	nuzlocke_id, route, species
item_obtained	El jugador recibe un objeto	nuzlocke_id, item, quantity
Ejemplo — captura:


POST /nuzlocke/workflow
{
  "workflowId": "capture_pokemon",
  "sessionId": "abc123_2026-02-22",
  "nuzlocke_id": "abc123_2026-02-22",
  "species": "pikachu",
  "nickname": "Sparky",
  "location": "Viridian Forest",
  "level": 5
}
Respuesta HTTP — inmediata con el resultado de la mutación + correlationId para el advice:


{
  "success": true,
  "workflowId": "capture_pokemon",
  "correlationId": "f3a9...",
  "mutations": [{ "type": "added_to_team", "description": "Sparky added to team" }],
  "data": { "pokemon": {...}, "destination": "team" },
  "errors": []
}
WebSocket recibe (en orden):


// 1. Evento de estado mutado
{ "type": "workflow_event", "workflowId": "capture_pokemon", "success": true, "mutations": [...], "correlationId": "f3a9..." }

// 2. Inicio del advice del LLM
{ "type": "advice_start", "workflowId": "capture_pokemon", "correlationId": "f3a9..." }

// 3. Chunks de texto mientras el LLM genera
{ "type": "advice_chunk", "content": "¡Excelente captura!", "correlationId": "f3a9..." }
{ "type": "advice_chunk", "content": " Pikachu es una buena opción...", "correlationId": "f3a9..." }

// 4. Fin del advice con el texto completo
{ "type": "advice_end", "fullAdvice": "¡Excelente captura! Pikachu es una buena opción...", "correlationId": "f3a9..." }
4. Preguntas libres al agente
Para preguntas en lenguaje natural fuera del flujo de workflows:


POST /nuzlocke/advice
{
  "question": "¿Qué Pokémon me recomiendas para el gimnasio de Brock?",
  "sessionId": "abc123_2026-02-22",
  "language": "es-ES"
}
Respuesta HTTP — inmediata:


{ "question": "...", "correlationId": "d7b2..." }
WebSocket recibe — igual que en workflows: advice_start → advice_chunk* → advice_end

5. Gestión de errores
Si algo falla (nuzlocke_id inválido, parámetros incorrectos, timeout LLM):


// En HTTP response:
{ "success": false, "errors": ["Nuzlocke not found: abc123"] }

// O en WebSocket:
{ "type": "advice_error", "correlationId": "...", "error": "Nuzlocke not found: abc123. Ensure init_nuzlocke was called first." }
Diagrama de flujo resumido

Frontend                          Server
   |                                 |
   |-- WS connect (?sessionId) ----->|  (mantener abierto)
   |                                 |
   |-- POST /workflow init_nuzlocke->|
   |<-- HTTP: { nuzlockeId } --------|
   |<-- WS: workflow_event ----------|
   |                                 |
   |-- POST /workflow capture ------>|
   |<-- HTTP: { success, mutations } |  (inmediato)
   |<-- WS: workflow_event ----------|  (estado actualizado)
   |<-- WS: advice_start ------------|
   |<-- WS: advice_chunk x N --------|  (streaming LLM)
   |<-- WS: advice_end --------------|
El correlationId vincula la respuesta HTTP con los mensajes WebSocket del mismo workflow.