# MCP Server (AI Pokémon Nuzlocker)

Proyecto backend minimal para el servidor MCP (Web API) responsable de gestionar el estado de partida y ofrecer endpoints básicos.

Estructura creada:

- `src/McpServer` - proyecto Web API (.NET 8)
- `tests/McpServer.Tests` - proyecto de tests xUnit
- `session_state.json` - fichero de estado de sesión inicial

Instrucciones rápidas:

Restaurar dependencias y ejecutar tests:

```powershell
dotnet test mcp-server/tests/McpServer.Tests/McpServer.Tests.csproj
```
