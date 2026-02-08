namespace es.vargontoc.nuzlocke.ai.Repositories;

public interface ICacheRepository<T> where T : class
{
    Task<T?> GetByNameOrIdAsync(string nameOrId);
    Task AddAsync(string nameOrId, string jsonData);
    Task UpdateAsync(string nameOrId, string jsonData);
    Task DeleteAsync(string nameOrId);
    Task<bool> ExistsAsync(string nameOrId);
}
