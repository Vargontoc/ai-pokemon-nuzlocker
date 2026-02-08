using es.vargontoc.nuzlocke.ai.Data;
using Microsoft.EntityFrameworkCore;

namespace es.vargontoc.nuzlocke.ai.Repositories;

public class PokemonCacheRepository : ICacheRepository<CachedPokemon>
{
    private readonly PokeDbContext _context;

    public PokemonCacheRepository(PokeDbContext context)
    {
        _context = context;
    }

    public async Task<CachedPokemon?> GetByNameOrIdAsync(string nameOrId)
    {
        return await _context.CachedPokemons
            .FirstOrDefaultAsync(p => p.NameOrId.ToLower() == nameOrId.ToLower());
    }

    public async Task AddAsync(string nameOrId, string jsonData)
    {
        var cached = new CachedPokemon
        {
            NameOrId = nameOrId.ToLower(),
            JsonData = jsonData,
            CachedAt = DateTime.UtcNow
        };

        _context.CachedPokemons.Add(cached);
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
            _context.CachedPokemons.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string nameOrId)
    {
        return await _context.CachedPokemons
            .AnyAsync(p => p.NameOrId.ToLower() == nameOrId.ToLower());
    }
}
