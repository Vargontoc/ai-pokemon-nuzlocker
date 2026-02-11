# Manual Testing Guide - Nuzlocke AI Agent

Esta guía describe cómo realizar pruebas manuales del agente de IA para verificar que funciona correctamente con Ollama (local), Claude API (cloud) u OpenAI (cloud).

## Pre-requisitos

### Para Ollama (Local)
1. Instalar Ollama: https://ollama.ai/
2. Descargar un modelo:
   ```bash
   ollama pull llama3.2
   ```
3. Verificar que Ollama está corriendo:
   ```bash
   ollama list
   ```

### Para Claude API (Cloud)
1. Obtener una API key de Anthropic: https://console.anthropic.com/
2. Configurar la API key en `appsettings.Development.json` o `appsettings.Production.json`

### Para OpenAI (Cloud)
1. Obtener una API key de OpenAI: https://platform.openai.com/api-keys
2. Configurar la API key en los archivos de configuración

## Configuración

### Configurar para Ollama (Local)

En `appsettings.json` o `appsettings.Development.json`:
```json
{
  "AiProvider": {
    "Provider": "Ollama",
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.2",
    "MaxTokens": 1024,
    "Temperature": 0.7
  }
}
```

### Configurar para Claude API (Cloud)

En `appsettings.Production.json`:
```json
{
  "AiProvider": {
    "Provider": "Claude",
    "BaseUrl": "https://api.anthropic.com/v1",
    "ApiKey": "sk-ant-api03-...",
    "Model": "claude-3-5-sonnet-20241022",
    "MaxTokens": 2048,
    "Temperature": 0.7
  }
}
```

### Configurar para OpenAI (Cloud)

En `appsettings.json`:
```json
{
  "AiProvider": {
    "Provider": "OpenAI",
    "ApiKey": "sk-proj-...",
    "Model": "gpt-4o",
    "MaxTokens": 2048,
    "Temperature": 0.7
  }
}
```

**Modelos recomendados**:
- `gpt-4o` - Más capaz, más caro
- `gpt-4o-mini` - Más rápido, más económico
- `gpt-3.5-turbo` - Más barato, menos capaz

## Iniciar el Servidor

```bash
cd mcp-server/src/McpServer
dotnet run
```

El servidor iniciará en: `http://localhost:5000` (o el puerto configurado)

## Escenarios de Prueba

### Escenario 1: Estado Vacío - Primera Captura

**Objetivo**: Verificar que el agente da consejos apropiados cuando no hay Pokemon en el equipo.

```bash
# POST http://localhost:5000/agent/advice
curl -X POST http://localhost:5000/agent/advice \
  -H "Content-Type: application/json" \
  -d '{
    "question": "Acabo de empezar mi Nuzlocke de Pokemon Rojo. ¿Qué Pokemon debería elegir como inicial?"
  }'
```

**Resultado Esperado**:
- El agente debe reconocer que el equipo está vacío
- Debe dar consejos sobre ventajas/desventajas de los iniciales (Bulbasaur, Charmander, Squirtle)
- Debe mencionar las reglas Nuzlocke básicas

### Escenario 2: Con Equipo Activo - Estrategia

**Preparación**:
```bash
# Agregar Pokemon al equipo usando MCP tools
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "AddToTeam",
      "arguments": {
        "nickname": "Sparky",
        "species": "pikachu",
        "level": 15,
        "caughtAt": "Viridian Forest",
        "currentHP": 40,
        "maxHP": 50,
        "moves": "thundershock,quick-attack,thunder-wave"
      }
    },
    "id": 1
  }'

curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "AddToTeam",
      "arguments": {
        "nickname": "Rocky",
        "species": "geodude",
        "level": 12,
        "caughtAt": "Mt. Moon",
        "currentHP": 35,
        "maxHP": 35,
        "moves": "tackle,defense-curl,rock-throw"
      }
    },
    "id": 2
  }'
```

**Prueba**:
```bash
curl -X POST http://localhost:5000/agent/advice \
  -H "Content-Type: application/json" \
  -d '{
    "question": "Voy a enfrentar a Misty en el gimnasio de Cerulean. ¿Qué estrategia debería usar con mi equipo actual?"
  }'
```

**Resultado Esperado**:
- El agente debe mencionar a Sparky (Pikachu) y Rocky (Geodude)
- Debe advertir sobre la ventaja de tipo Agua contra Geodude
- Debe recomendar usar Pikachu con movimientos eléctricos
- Debe advertir sobre el riesgo de perder Pokemon

### Escenario 3: Después de Muerte - Gestión Emocional

**Preparación**:
```bash
# Marcar un Pokemon como muerto
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "MarkAsDead",
      "arguments": {
        "nickname": "Rocky",
        "deathLocation": "Cerulean Gym",
        "causeOfDeath": "Defeated by Misty'\''s Starmie using Water Pulse"
      }
    },
    "id": 3
  }'
```

**Prueba**:
```bash
curl -X POST http://localhost:5000/agent/advice \
  -H "Content-Type: application/json" \
  -d '{
    "question": "Acabo de perder a Rocky contra Misty. ¿Qué debería hacer ahora?"
  }'
```

**Resultado Esperado**:
- El agente debe reconocer la muerte de Rocky
- Debe dar consejos para recomponer el equipo
- Debe sugerir tipos de Pokemon para capturar como reemplazo
- Debe ser empático pero estratégico

### Escenario 4: Análisis de Equipo Completo

**Preparación**: Agregar 4 Pokemon más al equipo (total 6)

**Prueba**:
```bash
curl -X POST http://localhost:5000/agent/advice \
  -H "Content-Type: application/json" \
  -d '{
    "question": "¿Mi equipo actual está bien balanceado? ¿Qué debilidades tengo?"
  }'
```

**Resultado Esperado**:
- Análisis de tipos del equipo
- Identificación de debilidades de tipo
- Sugerencias de cobertura de movimientos
- Recomendaciones estratégicas

### Escenario 5: Consulta de Encuentros

**Preparación**:
```bash
# Registrar encuentros
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "RecordEncounter",
      "arguments": {
        "location": "Route 1",
        "capturedSpecies": "pidgey",
        "capturedNickname": "Birdy"
      }
    },
    "id": 4
  }'
```

**Prueba**:
```bash
curl -X POST http://localhost:5000/agent/advice \
  -H "Content-Type: application/json" \
  -d '{
    "question": "Ya usé mi encuentro en Route 1. ¿Qué Pokemon debería buscar en Route 3?"
  }'
```

**Resultado Esperado**:
- El agente debe reconocer que Route 1 ya fue usado
- Debe sugerir Pokemon disponibles en Route 3
- Debe considerar las necesidades actuales del equipo

## Verificación de Funcionalidad

### Checklist - Ollama (Local)

- [ ] Servidor inicia correctamente con provider "Ollama"
- [ ] Endpoint `/agent/advice` responde (no error 500)
- [ ] Respuestas son coherentes con el contexto de Nuzlocke
- [ ] El agente menciona Pokemon específicos del estado actual
- [ ] Latencia aceptable (< 30 segundos para modelo pequeño)

### Checklist - Claude API (Cloud)

- [ ] Servidor inicia correctamente con provider "Claude"
- [ ] API key es válida y autenticación funciona
- [ ] Endpoint `/agent/advice` responde
- [ ] Respuestas son coherentes y más detalladas que Ollama
- [ ] El agente considera reglas Nuzlocke correctamente
- [ ] Latencia aceptable (< 5 segundos típicamente)

### Checklist - OpenAI (Cloud)

- [ ] Servidor inicia correctamente con provider "OpenAI"
- [ ] API key es válida y autenticación funciona
- [ ] Endpoint `/agent/advice` responde
- [ ] Respuestas son coherentes y estratégicas
- [ ] El agente menciona Pokemon y tipos específicos
- [ ] Latencia aceptable (< 10 segundos con GPT-4, < 3 segundos con GPT-3.5)

## Troubleshooting

### Ollama no responde
```bash
# Verificar que Ollama está corriendo
curl http://localhost:11434/api/tags

# Si no responde, iniciar Ollama
ollama serve
```

### Claude API - Error 401
- Verificar que la API key es válida
- Verificar que no expiró
- Verificar formato: debe empezar con "sk-ant-api03-"

### OpenAI - Error 401
- Verificar que la API key es válida
- Verificar que no expiró
- Verificar formato: debe empezar con "sk-proj-" o "sk-"
- Verificar que tienes créditos disponibles en tu cuenta

### OpenAI - Error 429 (Rate Limit)
- Has excedido el límite de requests por minuto
- Espera unos segundos antes de reintentar
- Considera actualizar tu plan de OpenAI

### Error 500 - Invalid Operation
- Verificar logs del servidor
- Revisar que el provider configurado existe ("Claude" o "Ollama")
- Verificar que todos los servicios se registraron correctamente

## Ejemplo de Respuesta Exitosa

```json
{
  "question": "¿Qué Pokemon debería elegir como inicial?",
  "advice": "For a Pokemon Red Nuzlocke run, here's my strategic advice on choosing your starter:\n\n**Bulbasaur (Grass/Poison)** - RECOMMENDED for beginners:\n- Easy mode for first two gyms (Brock's Rock types, Misty's Water types)\n- Learns Razor Leaf early, giving reliable damage\n- Poison type helps against later opponents\n- Best survivability in early game\n\n**Squirtle (Water)**:\n- Good middle ground difficulty\n- Advantage against Brock, neutral against Misty\n- Water types are reliable throughout the game\n- Evolves into powerful Blastoise\n\n**Charmander (Fire)** - Hard mode:\n- Struggles heavily against Brock and Misty\n- Fire types are rare in Kanto, making it valuable\n- Becomes very powerful late game\n- High risk of early death - NOT recommended for first Nuzlocke\n\n**My recommendation**: Start with Bulbasaur. In a Nuzlocke, survival is paramount, and losing your starter early can cripple your entire run. Bulbasaur gives you the best chance to build a strong team foundation.\n\nRemember: Give it a meaningful nickname - it creates emotional attachment and makes the Nuzlocke experience more impactful!"
}
```

## Notas Adicionales

- **Costos**: Claude API cobra por tokens. Pruebas extensivas pueden generar costos.
- **Velocidad**: Ollama es más lento pero gratuito. Claude es rápido pero de pago.
- **Calidad**: Claude generalmente da respuestas más detalladas y precisas.
- **Privacidad**: Ollama es completamente local. Claude envía datos a Anthropic.

## Próximos Pasos

Después de validar manualmente:
1. Considerar tests automatizados end-to-end
2. Agregar métricas de calidad de respuestas
3. Implementar caché de respuestas frecuentes
4. Agregar historial de conversación (context memory)
