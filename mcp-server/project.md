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







