namespace es.vargontoc.nuzlocke.ai.Repositories;

public interface INuzlockeRegistryRepository
{
    Task<string?> GetPathAsync(string nuzlockeId);
    Task<Dictionary<string, string>> GetAllAsync();
    Task RegisterAsync(string nuzlockeId, string path);
    Task RemoveAsync(string nuzlockeId);
}
