## Project

Proyecto de IA para jugar Pokémon Nuzlocke. Desarrollar un ecosistema de herramientas basadas en el protocolo MCP para asistir en tiempo real durante un reto Nuzlocke de Pokémon, integrando datos de PokeApi y un estado de partida local para ofrecer asesoramiento táctico en streaming.

## Tech Stack

- C#
- MCP
- PokeApi
- Streamlit

## Scope

### Fase 1: MCP Core
- Conector PokeApi para obtener datos de Pokémon, movimientos, tipos, etc.
- Gestor de estado de partida local. Un sistema CRUD sobre un Json que guarde el equipo actual, Pokemon muertos, Pokemon capturados guardados en el PC.
- Por cada nuevo pokemon, habilidad, movimiento que aparezca se guardará en una base de datos SQLLite para una mayor rapidez de busqueda por parte de la IA. Por tanto priorizamos la busqueda de la informacion en local y si no existe buscar en PokeApi. Y si no existe en ninguna de las dos partes preguntar al usuario para que de esa información.
- Calculadora de daño. Una calculadora que permita calcular el daño que recibe un Pokémon de otro Pokémon.

### Fase 2: IA Integradora
- Prompt Engineer, definición de la personalidad de la IA (Juez de Nuzlocke con aversión al riesgo)
- Context Window: sistema de actualización de turno a turno mediante inputs manuales

## Tools

- **PokeApi**: Para obtener datos de Pokémon, movimientos, tipos, etc.
- **Streamlit**: Para crear la interfaz de usuario y mostrar el asesoramiento táctico en tiempo real.
- **MCP**: Para comunicar el estado del juego y recibir asesoramiento táctico.


## Requisitos técnicos

### Backend
- Protocolo: MCP
- Lenguaje: C#
- Framework: .NET 8
- API: PokeApi
- Modelos IA: 
    - Local: Ollama (Llama 3.3) para narrativa y comandos rápidos
    - Cloud: OpenAI (GPT-4o) para análisis complejos y recomendaciones tácticas

### Datos y apis
- PokeApi: Para obtener datos de Pokémon, movimientos, tipos, etc.
- Persistencia: session_state.json para el progeso del nuzlocke

## Workflow
1. Input: El usuario comunica un suceso.
2. Procesamiento: El servidor MCP gestiona el estado del juego, actualiza el JSON y genera una respuesta.
3. Analisis: La IA evalua riesgos y oportunidades.
4. Output: La IA sugiere una acción basada en la regla de oro la supervivencia del equipo.


Estado actual del backend
Lo que tenemos implementado
3 capas de interacción con el usuario:

Capa	Endpoint	Cómo funciona
MCP Tools	/mcp	13 herramientas (8 estado + 5 PokeAPI) para clientes MCP
Agente NuzlockeAgent	/nuzlocke/advice + /nuzlocke/advice/stream	Lenguaje natural → tool-calling loop (hasta 10 ciclos) → respuesta
Workflows	/nuzlocke/workflow	Acciones estructuradas (validate→fetch→mutate→advice) con WebSocket async
Infraestructura sólida:

Cache L1 (memory) → L2 (SQLite) → L3 (PokeAPI) para datos pokemon
3 proveedores IA intercambiables: Ollama (local), OpenAI, Claude
Persistencia por nuzlocke: carpetas con game_state.json, .nuzlocke metadata, registro SQLite
WebSocket para streaming de advice async (evita timeouts HTTP con LLMs lentos)
186 tests automatizados
El problema que señalas:

Los workflows y el agente son mundos separados. El agente (/nuzlocke/advice) tiene tool-calling pero no conoce los workflows. Los workflows (/nuzlocke/workflow) son estructurados pero requieren que el front construya el JSON manualmente. No hay puente entre ellos.

Features que faltan (aparte de workflows)
1. Integración Agente ↔ Workflows (lo que propones)

El usuario escribe:

"Capturé un Weedle en la Ruta 2 y lo llamé Gusanito, nivel 3"

El agente:

Detecta intención → capture_pokemon
Extrae parámetros: species=weedle, location=Route 2, nickname=Gusanito, level=3
Ejecuta el workflow internamente
Devuelve el resultado + advice por streaming
Esto convertiría al agente en la interfaz principal — el front solo necesita un chat + WebSocket.

2. Gestión de equipo interactiva

swap_team_pc: intercambiar pokemon entre equipo y PC (el usuario dice "saca a Gusanito y mete a Sparky")
update_level: subir nivel de un pokemon ("Sparky subió al nivel 12")
3. Estado de la partida enriquecido

Tracking de badges/gimnasios completados
Ubicación actual del jugador (para contextualizar consejos)
Historial de decisiones (para que la IA recuerde "te recomendé X y funcionó")
4. Resumen/Estado por streaming

Un endpoint tipo "dame un resumen de cómo va mi partida" que el agente responda con contexto completo: equipo, muertes, próximo reto, riesgos.

Sobre la integración Agente ↔ Workflows
La idea es registrar los workflows como tools del agente. El NuzlockeAgent ya tiene un loop de tool-calling con ToolExecutor. Habría que:

Registrar cada workflow como tool en ToolDefinitions (con sus parámetros como schema)
Añadir al ToolExecutor la capacidad de ejecutar workflows via IWorkflowEngine
El agente extrae parámetros del lenguaje natural → llama al workflow como tool → devuelve resultado + advice
Todo por el streaming existente (/nuzlocke/advice/stream)
El flujo sería:


Chat: "Capturé un Weedle nivel 3 en Ruta 2, lo llamé Gusanito"
  → NuzlockeAgent detecta tool_call: capture_pokemon({species:"weedle", ...})
  → ToolExecutor delega a WorkflowEngine.ExecuteAsync()
  → Resultado + advice se streaman al usuario
Esto es relativamente limpio porque la infraestructura ya existe en ambos lados. El puente es ToolExecutor + ToolDefinitions.

Quieres que planifique la implementación de esta integración?





