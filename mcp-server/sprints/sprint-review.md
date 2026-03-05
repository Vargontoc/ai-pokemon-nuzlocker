## Sprint Review [05-03-2026-performance]

### Análisis de rendimiento

Tras revisar el código, se identifican **4 cuellos de botella** en el path de `POST /nuzlocke/advice`:

---

#### Problema 1 — Prefetch redundante de PokeApiAgent [IMPACTO ALTO]

**Ubicación**: `NuzlockeAgent.cs:174-195`

```csharp
// Si la pregunta contiene "move/pokemon/type/ability/item" → llama a PokeApiAgent completo
var pokeInfo = await _pokeApiAgent.GetResponse(userQuestion, cancellationToken);
fullMessage += $"\n\nEXTERNAL_POKEAPI_INFO:\n{pokeInfo}";
```

Esto lanza un agente LLM secundario (con su propio ciclo de tool-calling) **antes** del ciclo principal, en casi todas las preguntas (las palabras clave aplican a casi cualquier consulta Pokémon). El agente ya dispone de los tools `get_pokemon`, `get_move`, `get_type` para consultar PokeAPI on-demand. Este prefetch es **redundante y añade una llamada LLM completa** innecesaria.

**Fix**: Eliminar el bloque de prefetch. El agente llamará a los tools de PokeAPI solo cuando los necesite.

---

#### Problema 2 — Carga secuencial de estado inicial [IMPACTO MEDIO]

**Ubicación**: `NuzlockeAgent.cs:144-162`

```csharp
var state = await _stateManager.GetStateAsync(sessionId);           // disco
var battleContext = await _stateManager.GetBattleContextAsync(sessionId); // disco
var history = await _memoryStore.LoadAsync(sessionId);               // disco
```

Tres lecturas de fichero independientes ejecutadas en secuencia. Pueden paralelizarse con `Task.WhenAll`.

**Fix**: Paralelizar las tres cargas iniciales.

---

#### Problema 3 — Ejecución secuencial de tool calls por ciclo [IMPACTO MEDIO]

**Ubicación**: `NuzlockeAgent.cs:252-302`

```csharp
foreach (var toolCall in response.ToolCalls)
{
    var result = await _toolExecutor.ExecuteAsync(toolCall);
    toolResults.Add(result);
}
```

Cuando el LLM devuelve múltiples tool calls en un ciclo (p.ej. `get_game_state` + `get_pokemon`), se ejecutan en serie. Son independientes entre sí.

**Fix**: Paralelizar con `Task.WhenAll`, preservando el orden de resultados para el LLM.

---

#### Problema 4 — MaxToolCycles demasiado alto [IMPACTO BAJO-MEDIO]

**Ubicación**: `NuzlockeAgent.cs:24`

```csharp
private const int MaxToolCycles = 10;
```

En la práctica, el 95% de las respuestas se resuelven en 1-3 ciclos. 10 ciclos = 10 llamadas LLM en el peor caso.

**Fix**: Reducir a 5 ciclos como límite razonable.

---

### Objetivos del Sprint

- [ ] **Fix 1**: Eliminar el prefetch redundante de `PokeApiAgent` en `NuzlockeAgent.GetAdviceAsync`
- [ ] **Fix 2**: Paralelizar la carga inicial de estado/battleContext/historial con `Task.WhenAll`
- [ ] **Fix 3**: Paralelizar la ejecución de tool calls dentro de cada ciclo con `Task.WhenAll`
- [ ] **Fix 4**: Reducir `MaxToolCycles` de 10 a 5

### Aprobación Sprint review
- [ ] Tests pasan en verde correctamente
- [ ] Reducción de latencia medible en logs (comparar tiempos de ciclos)

### Riesgos
- [ ] Fix 3 (paralelismo en tools): el `onToolCall` callback envía eventos WS — asegurarse de que el orden de los eventos WS no importe (los eventos son independientes por `CorrelationId`)
- [ ] Fix 1: algunos tests de `NuzlockeAgentFunctionCallingTests` pueden verificar la llamada a `PokeApiAgent` — revisar y actualizar

### Fallos
- [ ]

### Sugerencias para el próximo Sprint
- [ ] Streaming del último ciclo del agente: emitir `advice_chunk` durante la generación de la respuesta final en lugar de esperar `advice_end`
- [ ] Workflows de batalla: `next_battle`, `start_battle`, `next_turn`, `end_battle`
