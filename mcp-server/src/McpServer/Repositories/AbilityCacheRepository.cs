using es.vargontoc.nuzlocke.ai.Data;
using Microsoft.EntityFrameworkCore;

namespace es.vargontoc.nuzlocke.ai.Repositories;

public class AbilityCacheRepository : ICacheRepository<CachedAbility>
{
    private readonly PokeDbContext _context;

    public AbilityCacheRepository(PokeDbContext context)
    {
        _context = context;
    }

    public async Task<CachedAbility?> GetByNameOrIdAsync(string nameOrId)
    {
        return await _context.CachedAbilities
            .FirstOrDefaultAsync(a => a.NameOrId.ToLower() == nameOrId.ToLower());
    }

    public async Task AddAsync(string nameOrId, string jsonData)
    {
        var cached = new CachedAbility
        {
            NameOrId = nameOrId.ToLower(),
            JsonData = jsonData,
            CachedAt = DateTime.UtcNow
        };

        _context.CachedAbilities.Add(cached);
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
            _context.CachedAbilities.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string nameOrId)
    {
        return await _context.CachedAbilities
            .AnyAsync(a => a.NameOrId.ToLower() == nameOrId.ToLower());
    }
}
