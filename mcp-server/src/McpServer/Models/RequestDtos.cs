namespace es.vargontoc.nuzlocke.ai.Models;

public record AdviceRequest(string Question, string? NuzlockeId = null, string? Language = "en-US");

public record CreateSessionRequest(
    string Name,
    LockeType LockeType = LockeType.Standard,
    int Generation = 1,
    string? Descripcion = null);

public record UpdateNuzlockeStateRequest(NuzlockeStatus Status);
