using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

public interface INuzlockeSessionManager
{
    Task<NuzlockeSessionInfo> CreateSessionAsync(string name, string directoryPath);
    Task<NuzlockeSessionInfo?> GetSessionAsync(string sessionId);
    Task<List<NuzlockeSessionInfo>> ListSessionsAsync();
    Task<bool> DeleteSessionAsync(string sessionId);
    Task<NuzlockeFileData> LoadSessionDataAsync(string sessionId);
    Task SaveSessionDataAsync(string sessionId, NuzlockeFileData data);
}
