namespace es.vargontoc.nuzlocke.ai.Models;

public record AdviceRequest(string Question, string? SessionId = null, string? Language = "en-US");
public record CreateSessionRequest(string Name, string DirectoryPath);
