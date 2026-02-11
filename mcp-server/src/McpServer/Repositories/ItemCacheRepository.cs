using es.vargontoc.nuzlocke.ai.Data;
using Microsoft.EntityFrameworkCore;

namespace es.vargontoc.nuzlocke.ai.Repositories;

public class ItemCacheRepository : ICacheRepository<CachedItem>
{
    private readonly PokeDbContext _context;

    public ItemCacheRepository(PokeDbContext context)
    {
        _context = context;
    }

    public async Task<CachedItem?> GetByNameOrIdAsync(string nameOrId)
    {
        return await _context.CachedItems
            .FirstOrDefaultAsync(i => i.NameOrId.ToLower() == nameOrId.ToLower());
    }

    public async Task AddAsync(string nameOrId, string jsonData)
    {
        var cached = new CachedItem
        {
            NameOrId = nameOrId.ToLower(),
            JsonData = jsonData,
            CachedAt = DateTime.UtcNow
        };

        _context.CachedItems.Add(cached);
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
            _context.CachedItems.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string nameOrId)
    {
        return await _context.CachedItems
            .AnyAsync(i => i.NameOrId.ToLower() == nameOrId.ToLower());
    }
}
