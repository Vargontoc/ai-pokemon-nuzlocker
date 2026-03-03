using System.Collections.Concurrent;
using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Models;
using Microsoft.EntityFrameworkCore;

namespace es.vargontoc.nuzlocke.ai.Services;

public class NuzlockeRepository : INuzlockeRepository
{
    private readonly IDbContextFactory<PokeDbContext> _dbFactory;
    private readonly ILogger<NuzlockeRepository> _logger;
    private readonly string _basePath;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private const string MetadataFileName = ".nuzlocke";
    private const string GameStateFileName = "game_state.json";
    private const string BattleStateFileName = "battle_state.json";
    private const string AgentMemoryFileName = "agent.json";
    private const string MemoryDirName = "memory";
    private const string ResultsDirName = "results";

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NuzlockeRepository(
        IDbContextFactory<PokeDbContext> dbFactory,
        IConfiguration configuration,
        ILogger<NuzlockeRepository> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _basePath = configuration["NuzlockeBasePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "nuzlockes");
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    public async Task<NuzlockeMetadata> CreateAsync(
        string name,
        LockeType lockeType = LockeType.Standard,
        int generation = 1,
        string? descripcion = null)
    {
        var id = Guid.NewGuid().ToString();
        var nuzlockePath = Path.Combine(_basePath, id);

        Directory.CreateDirectory(nuzlockePath);
        Directory.CreateDirectory(Path.Combine(nuzlockePath, MemoryDirName));
        Directory.CreateDirectory(Path.Combine(nuzlockePath, ResultsDirName));

        var metadata = new NuzlockeMetadata
        {
            Id = id,
            Name = name,
            Descripcion = descripcion,
            Generation = generation,
            LockeType = lockeType,
            IsInitialized = false,
            Status = NuzlockeStatus.Building,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        await WriteJsonAsync(Path.Combine(nuzlockePath, MetadataFileName), metadata);

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.NuzlockeMetadatas.Add(metadata);
        await db.SaveChangesAsync();

        _logger.LogInformation("Created nuzlocke {Id} ({Name}) at {Path}", id, name, nuzlockePath);
        return metadata;
    }

    public async Task<NuzlockeMetadata?> GetMetadataAsync(string nuzlockeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NuzlockeMetadatas.FindAsync(nuzlockeId);
    }

    public async Task SaveMetadataAsync(NuzlockeMetadata metadata)
    {
        metadata.LastUpdated = DateTime.UtcNow;

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.NuzlockeMetadatas.Update(metadata);
        await db.SaveChangesAsync();

        var nuzlockePath = Path.Combine(_basePath, metadata.Id);
        if (Directory.Exists(nuzlockePath))
            await WriteJsonAsync(Path.Combine(nuzlockePath, MetadataFileName), metadata);

        _logger.LogInformation("Saved metadata for nuzlocke {Id}", metadata.Id);
    }

    public async Task<List<NuzlockeMetadata>> ListAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NuzlockeMetadatas.ToListAsync();
    }

    public async Task<bool> DeleteAsync(string nuzlockeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entry = await db.NuzlockeMetadatas.FindAsync(nuzlockeId);
        if (entry == null) return false;

        db.NuzlockeMetadatas.Remove(entry);
        await db.SaveChangesAsync();

        var nuzlockePath = Path.Combine(_basePath, nuzlockeId);
        if (Directory.Exists(nuzlockePath))
        {
            Directory.Delete(nuzlockePath, recursive: true);
            _logger.LogInformation("Deleted nuzlocke directory {Path}", nuzlockePath);
        }

        _logger.LogInformation("Deleted nuzlocke {Id}", nuzlockeId);
        return true;
    }

    public async Task<bool> UpdateStatusAsync(string nuzlockeId, NuzlockeStatus status)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entry = await db.NuzlockeMetadatas.FindAsync(nuzlockeId);
        if (entry == null) return false;

        entry.Status = status;
        entry.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var nuzlockePath = Path.Combine(_basePath, nuzlockeId);
        if (Directory.Exists(nuzlockePath))
            await WriteJsonAsync(Path.Combine(nuzlockePath, MetadataFileName), entry);

        _logger.LogInformation("Updated status for nuzlocke {Id} to {Status}", nuzlockeId, status);
        return true;
    }

    // ── Path resolution ────────────────────────────────────────────────────

    public async Task<string?> GetNuzlockePathAsync(string nuzlockeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var exists = await db.NuzlockeMetadatas.AnyAsync(m => m.Id == nuzlockeId);
        return exists ? Path.Combine(_basePath, nuzlockeId) : null;
    }

    // ── Game state ─────────────────────────────────────────────────────────

    public async Task<NuzlockeState> GetGameStateAsync(string nuzlockeId)
    {
        var path = await ResolvePath(nuzlockeId, GameStateFileName);
        var sem = GetLock(nuzlockeId);
        await sem.WaitAsync();
        try
        {
            return await ReadJsonAsync<NuzlockeState>(path) ?? new NuzlockeState();
        }
        finally { sem.Release(); }
    }

    public async Task SaveGameStateAsync(string nuzlockeId, NuzlockeState state)
    {
        var path = await ResolvePath(nuzlockeId, GameStateFileName);
        var sem = GetLock(nuzlockeId);
        await sem.WaitAsync();
        try
        {
            state.LastUpdated = DateTime.UtcNow;
            await WriteJsonAsync(path, state);
        }
        finally { sem.Release(); }
    }

    // ── Battle state ───────────────────────────────────────────────────────

    public async Task<BattleContext?> GetBattleStateAsync(string nuzlockeId)
    {
        var path = await ResolvePath(nuzlockeId, BattleStateFileName);
        var sem = GetLock(nuzlockeId);
        await sem.WaitAsync();
        try
        {
            if (!File.Exists(path)) return null;
            return await ReadJsonAsync<BattleContext>(path);
        }
        finally { sem.Release(); }
    }

    public async Task SaveBattleStateAsync(string nuzlockeId, BattleContext context)
    {
        var path = await ResolvePath(nuzlockeId, BattleStateFileName);
        var sem = GetLock(nuzlockeId);
        await sem.WaitAsync();
        try
        {
            await WriteJsonAsync(path, context);
        }
        finally { sem.Release(); }
    }

    public async Task DeleteBattleStateAsync(string nuzlockeId)
    {
        var path = await ResolvePath(nuzlockeId, BattleStateFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            _logger.LogInformation("Deleted battle state for {Id}", nuzlockeId);
        }
    }

    // ── Agent memory ───────────────────────────────────────────────────────

    public async Task<AgentMemory> GetAgentMemoryAsync(string nuzlockeId)
    {
        var path = await ResolvePath(nuzlockeId, Path.Combine(MemoryDirName, AgentMemoryFileName));
        var sem = GetLock(nuzlockeId + "_agent");
        await sem.WaitAsync();
        try
        {
            return await ReadJsonAsync<AgentMemory>(path) ?? new AgentMemory();
        }
        finally { sem.Release(); }
    }

    public async Task SaveAgentMemoryAsync(string nuzlockeId, AgentMemory memory)
    {
        var path = await ResolvePath(nuzlockeId, Path.Combine(MemoryDirName, AgentMemoryFileName));
        var sem = GetLock(nuzlockeId + "_agent");
        await sem.WaitAsync();
        try
        {
            memory.LastUpdated = DateTime.UtcNow;
            await WriteJsonAsync(path, memory);
        }
        finally { sem.Release(); }
    }

    // ── Battle records ─────────────────────────────────────────────────────

    public async Task SaveBattleRecordAsync(string nuzlockeId, BattleRecord record)
    {
        var resultsDir = await ResolvePath(nuzlockeId, ResultsDirName);
        if (!Directory.Exists(resultsDir))
            Directory.CreateDirectory(resultsDir);

        var existingFiles = Directory.GetFiles(resultsDir, "battle_*.json");
        var nextNumber = existingFiles.Length + 1;
        var opponentSafe = SanitizeFileName(record.OpponentName);
        var fileName = $"battle_{nextNumber:D3}_vs_{opponentSafe}.json";

        await WriteJsonAsync(Path.Combine(resultsDir, fileName), record);
        _logger.LogInformation("Saved battle record {FileName} for {Id}", fileName, nuzlockeId);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private SemaphoreSlim GetLock(string key) =>
        _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

    private async Task<string> ResolvePath(string nuzlockeId, string relativePath)
    {
        var basePath = await GetNuzlockePathAsync(nuzlockeId)
            ?? throw new KeyNotFoundException($"Nuzlocke not found: {nuzlockeId}");
        return Path.Combine(basePath, relativePath);
    }

    private static async Task WriteJsonAsync<T>(string path, T data)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, _writeOptions));
    }

    private static async Task<T?> ReadJsonAsync<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<T>(json, _readOptions);
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name
            .ToLowerInvariant()
            .Replace(' ', '_')
            .Where(c => !invalidChars.Contains(c))
            .ToArray());
        return string.IsNullOrEmpty(sanitized) ? "unknown" : sanitized;
    }
}
