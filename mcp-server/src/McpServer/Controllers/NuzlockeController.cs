using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

/// <summary>
/// Gestión de partidas Nuzlocke (crear, listar, obtener, actualizar estado, eliminar).
/// </summary>
[ApiController]
[Route("nuzlocke")]
[Produces("application/json")]
public class NuzlockeController : ControllerBase
{
    /// <summary>
    /// Crear una nueva partida Nuzlocke.
    /// </summary>
    /// <remarks>
    /// Crea una partida con UUID, estructura de directorios y metadata .nuzlocke.
    /// La partida queda en estado Building hasta la primera conexión WebSocket (init_nuzlocke).
    ///
    /// Ejemplo:
    ///
    ///     POST /nuzlocke
    ///     {
    ///       "name": "Mi FireRed Run",
    ///       "lockeType": "Standard",
    ///       "generation": 1,
    ///       "descripcion": "Intento serio"
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(NuzlockeMetadata), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSessionRequest request,
        [FromServices] INuzlockeRepository repository)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "El campo 'name' es obligatorio." });

        if (request.Name.Length > 50)
            return BadRequest(new { error = "El campo 'name' no puede superar los 50 caracteres." });

        if (request.Descripcion?.Length > 100)
            return BadRequest(new { error = "El campo 'descripcion' no puede superar los 100 caracteres." });

        try
        {
            var metadata = await repository.CreateAsync(
                request.Name,
                request.LockeType,
                request.Generation,
                request.Descripcion);

            return Created($"/nuzlocke/{metadata.Id}", metadata);
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, statusCode: 400);
        }
    }

    /// <summary>
    /// Listar todas las partidas Nuzlocke existentes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<NuzlockeMetadata>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromServices] INuzlockeRepository repository)
    {
        return Ok(await repository.ListAsync());
    }

    /// <summary>
    /// Obtener una partida Nuzlocke por ID.
    /// </summary>
    /// <param name="id">UUID de la partida.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NuzlockeMetadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        string id,
        [FromServices] INuzlockeRepository repository)
    {
        var metadata = await repository.GetMetadataAsync(id);
        return metadata == null ? NotFound() : Ok(metadata);
    }

    /// <summary>
    /// Actualizar el estado de una partida Nuzlocke.
    /// </summary>
    /// <remarks>
    /// Único campo modificable por el usuario. Valores válidos: Active, Finished, GameOver, Abandoned
    ///
    ///     PUT /nuzlocke/abc123/state
    ///     { "status": "Finished" }
    /// </remarks>
    /// <param name="id">UUID de la partida.</param>
    [HttpPut("{id}/state")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateState(
        string id,
        [FromBody] UpdateNuzlockeStateRequest request,
        [FromServices] INuzlockeRepository repository)
    {
        var updated = await repository.UpdateStatusAsync(id, request.Status);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>
    /// Obtener el equipo activo (party) de una partida.
    /// </summary>
    /// <param name="id">UUID de la partida.</param>
    [HttpGet("{id}/party")]
    [ProducesResponseType(typeof(List<TeamMember>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetParty(
        string id,
        [FromServices] INuzlockeRepository repository)
    {
        var metadata = await repository.GetMetadataAsync(id);
        if (metadata == null) return NotFound();
        if (!metadata.IsInitialized)
            return Conflict(new { error = "La partida no está inicializada. Conéctate primero via WebSocket (/ws/advice)." });

        var state = await repository.GetGameStateAsync(id);
        return Ok(state.Team);
    }

    /// <summary>
    /// Obtener los Pokémon almacenados en el PC de una partida.
    /// </summary>
    /// <param name="id">UUID de la partida.</param>
    [HttpGet("{id}/pc")]
    [ProducesResponseType(typeof(List<StoredPokemon>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetPc(
        string id,
        [FromServices] INuzlockeRepository repository)
    {
        var metadata = await repository.GetMetadataAsync(id);
        if (metadata == null) return NotFound();
        if (!metadata.IsInitialized)
            return Conflict(new { error = "La partida no está inicializada. Conéctate primero via WebSocket (/ws/advice)." });

        var state = await repository.GetGameStateAsync(id);
        return Ok(state.PCStorage);
    }

    /// <summary>
    /// Obtener el inventario de objetos de una partida.
    /// </summary>
    /// <param name="id">UUID de la partida.</param>
    [HttpGet("{id}/inventory")]
    [ProducesResponseType(typeof(List<InventoryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetInventory(
        string id,
        [FromServices] INuzlockeRepository repository)
    {
        var metadata = await repository.GetMetadataAsync(id);
        if (metadata == null) return NotFound();
        if (!metadata.IsInitialized)
            return Conflict(new { error = "La partida no está inicializada. Conéctate primero via WebSocket (/ws/advice)." });

        var state = await repository.GetGameStateAsync(id);
        return Ok(state.Inventory);
    }

    /// <summary>
    /// Obtener los Pokémon muertos (cementerio Nuzlocke) de una partida.
    /// </summary>
    /// <param name="id">UUID de la partida.</param>
    [HttpGet("{id}/graveyard")]
    [ProducesResponseType(typeof(List<DeadPokemon>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetGraveyard(
        string id,
        [FromServices] INuzlockeRepository repository)
    {
        var metadata = await repository.GetMetadataAsync(id);
        if (metadata == null) return NotFound();
        if (!metadata.IsInitialized)
            return Conflict(new { error = "La partida no está inicializada. Conéctate primero via WebSocket (/ws/advice)." });

        var state = await repository.GetGameStateAsync(id);
        return Ok(state.DeadPokemon);
    }

    /// <summary>
    /// Eliminar una partida Nuzlocke y toda su estructura de datos.
    /// </summary>
    /// <remarks>
    /// Borra el registro de la base de datos y elimina el directorio de archivos. Operación irreversible.
    /// </remarks>
    /// <param name="id">UUID de la partida.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        string id,
        [FromServices] INuzlockeRepository repository)
    {
        var ok = await repository.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }

     private readonly ILogger<NuzlockeController> _logger;

    public NuzlockeController(ILogger<NuzlockeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Despacha una petición de consejo asíncrono. La respuesta del LLM se envía por WebSocket.
    /// </summary>
    /// <remarks>
    /// Valida que el nuzlocke existe, está inicializado y tiene una sesión WebSocket activa.
    /// Devuelve un `correlationId` inmediatamente. La respuesta llega via WebSocket (`ws://host/ws/advice?nuzlockeId=id`).
    ///
    /// Ejemplo:
    ///
    ///     POST /nuzlocke/advice
    ///     {
    ///       "question": "¿Uso a Sparky contra Misty?",
    ///       "nuzlockeId": "abc-123",
    ///       "language": "es-ES"
    ///     }
    /// </remarks>
    [HttpPost("advice")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAdvice(
        [FromBody] AdviceRequest request,
        [FromServices] IAdviceDispatcher dispatcher,
        [FromServices] INuzlockeRepository repository,
        [FromServices] IAdviceConnectionManager connectionManager)
    {
        if (string.IsNullOrWhiteSpace(request.NuzlockeId))
            return BadRequest(new { error = "El campo 'nuzlockeId' es obligatorio." });

        var metadata = await repository.GetMetadataAsync(request.NuzlockeId);
        if (metadata == null)
            return NotFound(new { error = $"Nuzlocke no encontrado: '{request.NuzlockeId}'." });

        if (!metadata.IsInitialized)
            return Conflict(new { error = "La partida no está inicializada. Conéctate primero via WebSocket (/ws/advice)." });

        if (!connectionManager.HasConnection(request.NuzlockeId))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "No hay sesión WebSocket activa para este nuzlocke. Conéctate a /ws/advice primero." });

        var correlationId = Guid.NewGuid().ToString("N");
        var lng = request.Language ?? "en-US";

        _logger.LogInformation(
            "Dispatching async agent advice for nuzlocke {NuzlockeId}, correlationId {CorrelationId}",
            request.NuzlockeId, correlationId);

        dispatcher.DispatchAgentAdvice(new AgentAdviceDispatchRequest
        {
            CorrelationId = correlationId,
            NuzlockeId = request.NuzlockeId,
            Question = request.Question,
            Language = lng
        });

        return Accepted(new { question = request.Question, correlationId });
    }

    /// <summary>
    /// Streaming de consejo de IA como Server-Sent Events (SSE).
    /// </summary>
    /// <remarks>
    /// Valida que el nuzlocke existe y está inicializado antes de abrir el stream.
    /// Devuelve `Content-Type: text/event-stream`. Cada chunk: `data: {text}\n\n`.
    /// Finaliza con `event: end\ndata: [DONE]\n\n`.
    ///
    /// Ejemplo:
    ///
    ///     POST /nuzlocke/advice/stream
    ///     {
    ///       "question": "¿Cuál es el mejor movimiento contra Brock?",
    ///       "nuzlockeId": "abc-123",
    ///       "language": "es-ES"
    ///     }
    /// </remarks>
    [HttpPost("advice/stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task StreamAdvice(
        [FromBody] AdviceRequest request,
        [FromServices] NuzlockeAgent agent,
        [FromServices] INuzlockeRepository repository,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NuzlockeId))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "El campo 'nuzlockeId' es obligatorio." }, ct);
            return;
        }

        var metadata = await repository.GetMetadataAsync(request.NuzlockeId);
        if (metadata == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(new { error = $"Nuzlocke no encontrado: '{request.NuzlockeId}'." }, ct);
            return;
        }

        if (!metadata.IsInitialized)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            await Response.WriteAsJsonAsync(new { error = "La partida no está inicializada. Conéctate primero via WebSocket (/ws/advice)." }, ct);
            return;
        }

        _logger.LogInformation("POST /nuzlocke/advice/stream: {Question}, nuzlockeId={NuzlockeId}",
            request.Question, request.NuzlockeId);

        Response.Headers["Cache-Control"] = "no-cache";
        Response.ContentType = "text/event-stream";

        try
        {
            await foreach (var chunk in agent.StreamAdviceAsync(request.Question, ct, request.NuzlockeId))
            {
                if (ct.IsCancellationRequested) break;
                await Response.WriteAsync($"data: {chunk.Replace("\n", "\\n")}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            await Response.WriteAsync("event: end\ndata: [DONE]\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Client cancelled — no response needed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while streaming advice for nuzlocke {NuzlockeId}", request.NuzlockeId);
        }
    }
}
