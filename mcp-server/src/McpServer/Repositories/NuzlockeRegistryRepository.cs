using es.vargontoc.nuzlocke.ai.Data;
using Microsoft.EntityFrameworkCore;

namespace es.vargontoc.nuzlocke.ai.Repositories;

public class NuzlockeRegistryRepository : INuzlockeRegistryRepository
{
    private readonly PokeDbContext _context;

    public NuzlockeRegistryRepository(PokeDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetPathAsync(string nuzlockeId)
    {
        var entry = await _context.NuzlockeRegistries
            .FirstOrDefaultAsync(r => r.NuzlockeId == nuzlockeId);
        return entry?.Path;
    }

    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        return await _context.NuzlockeRegistries
            .ToDictionaryAsync(r => r.NuzlockeId, r => r.Path);
    }

    public async Task RegisterAsync(string nuzlockeId, string path)
    {
        var existing = await _context.NuzlockeRegistries
            .FirstOrDefaultAsync(r => r.NuzlockeId == nuzlockeId);

        if (existing != null)
        {
            existing.Path = path;
        }
        else
        {
            _context.NuzlockeRegistries.Add(new NuzlockeRegistry
            {
                NuzlockeId = nuzlockeId,
                Path = path,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(string nuzlockeId)
    {
        var existing = await _context.NuzlockeRegistries
            .FirstOrDefaultAsync(r => r.NuzlockeId == nuzlockeId);

        if (existing != null)
        {
            _context.NuzlockeRegistries.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }
}
