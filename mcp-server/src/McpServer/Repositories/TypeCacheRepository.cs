using es.vargontoc.nuzlocke.ai.Data;
using Microsoft.EntityFrameworkCore;

namespace es.vargontoc.nuzlocke.ai.Repositories;

public class TypeCacheRepository : ICacheRepository<CachedType>
{
    private readonly PokeDbContext _context;

    public TypeCacheRepository(PokeDbContext context)
    {
        _context = context;
    }

    public async Task<CachedType?> GetByNameOrIdAsync(string nameOrId)
    {
        return await _context.CachedTypes
            .FirstOrDefaultAsync(t => t.NameOrId.ToLower() == nameOrId.ToLower());
    }

    public async Task AddAsync(string nameOrId, string jsonData)
    {
        var cached = new CachedType
        {
            NameOrId = nameOrId.ToLower(),
            JsonData = jsonData,
            CachedAt = DateTime.UtcNow
        };

        _context.CachedTypes.Add(cached);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(string nameOrId, string jsonData)
    {
        var existing = await GetByNameOrIdAsync(nameOrId);
        if (existing != null)
        {
            existing.JsonData = jsonData;
            existing.CachedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(string nameOrId)
    {
        var existing = await GetByNameOrIdAsync(nameOrId);
        if (existing != null)
        {
            _context.CachedTypes.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string nameOrId)
    {
        return await _context.CachedTypes
            .AnyAsync(t => t.NameOrId.ToLower() == nameOrId.ToLower());
    }
}
