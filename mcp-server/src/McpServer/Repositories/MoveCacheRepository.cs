using es.vargontoc.nuzlocke.ai.Data;
using Microsoft.EntityFrameworkCore;

namespace es.vargontoc.nuzlocke.ai.Repositories;

public class MoveCacheRepository : ICacheRepository<CachedMove>
{
    private readonly PokeDbContext _context;

    public MoveCacheRepository(PokeDbContext context)
    {
        _context = context;
    }

    public async Task<CachedMove?> GetByNameOrIdAsync(string nameOrId)
    {
        return await _context.CachedMoves
            .FirstOrDefaultAsync(m => m.NameOrId.ToLower() == nameOrId.ToLower());
    }

    public async Task AddAsync(string nameOrId, string jsonData)
    {
        var cached = new CachedMove
        {
            NameOrId = nameOrId.ToLower(),
            JsonData = jsonData,
            CachedAt = DateTime.UtcNow
        };

        _context.CachedMoves.Add(cached);
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
            _context.CachedMoves.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string nameOrId)
    {
        return await _context.CachedMoves
            .AnyAsync(m => m.NameOrId.ToLower() == nameOrId.ToLower());
    }
}
