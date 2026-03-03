using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
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
}
