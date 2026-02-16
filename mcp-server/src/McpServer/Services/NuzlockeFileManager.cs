using System.Collections.Concurrent;
using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

public class NuzlockeFileManager : INuzlockeFileManager
{
    private readonly ILogger<NuzlockeFileManager> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, string> _pathCache = new();

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string MetadataFileName = ".nuzlocke";
    private const string GameStateFileName = "game_state.json";
    private const string BattleStateFileName = "battle_state.json";
    private const string MemoryDirName = "memory";
    private const string ResultsDirName = "results";

    public NuzlockeFileManager(ILogger<NuzlockeFileManager> logger)
    {
        _logger = logger;
    }

    public async Task<string> CreateNuzlockeAsync(string basePath, int generation, string lockeType)
    {
        var shortGuid = Guid.NewGuid().ToString("N")[..8];
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var nuzlockeId = $"{shortGuid}_{date}";

        var nuzlockePath = Path.Combine(basePath, nuzlockeId);

        // Create directory structure
        Directory.CreateDirectory(nuzlockePath);
        Directory.CreateDirectory(Path.Combine(nuzlockePath, MemoryDirName));
        Directory.CreateDirectory(Path.Combine(nuzlockePath, ResultsDirName));

        // Write .nuzlocke metadata
        var metadata = new NuzlockeMetadata
        {
            NuzlockeId = nuzlockeId,
            Generation = generation,
            LockeType = lockeType,
            CreatedAt = DateTime.UtcNow,
            BasePath = basePath
        };

        await WriteJsonAsync(Path.Combine(nuzlockePath, MetadataFileName), metadata);

        // Write initial game_state.json
        var initialState = new NuzlockeState
        {
            Generation = generation,
            LockeType = lockeType,
            StartDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        await WriteJsonAsync(Path.Combine(nuzlockePath, GameStateFileName), initialState);

        // Cache the path
        _pathCache[nuzlockeId] = nuzlockePath;

        _logger.LogInformation("Created nuzlocke {NuzlockeId} at {Path}", nuzlockeId, nuzlockePath);
        return nuzlockeId;
    }

    public async Task<NuzlockeState> LoadGameStateAsync(string nuzlockeId)
    {
        var path = ResolvePath(nuzlockeId, GameStateFileName);
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
        var path = ResolvePath(nuzlockeId, GameStateFileName);
        var sem = GetLock(nuzlockeId);
        await sem.WaitAsync();
        try
        {
            state.LastUpdated = DateTime.UtcNow;
            await WriteJsonAsync(path, state);
        }
        finally { sem.Release(); }
    }

    public async Task<BattleContext?> LoadBattleStateAsync(string nuzlockeId)
    {
        var path = ResolvePath(nuzlockeId, BattleStateFileName);
        var sem = GetLock(nuzlockeId);
        await sem.WaitAsync();
        try
        {
            if (!File.Exists(path)) return null;
            return await ReadJsonAsync<BattleContext>(path);
        }
        finally { sem.Release(); }
    }

    public async Task SaveBattleStateAsync(string nuzlockeId, BattleContext battle)
    {
        var path = ResolvePath(nuzlockeId, BattleStateFileName);
        var sem = GetLock(nuzlockeId);
        await sem.WaitAsync();
        try
        {
            await WriteJsonAsync(path, battle);
        }
        finally { sem.Release(); }
    }

    public Task DeleteBattleStateAsync(string nuzlockeId)
    {
        var path = ResolvePath(nuzlockeId, BattleStateFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            _logger.LogInformation("Deleted battle state for {NuzlockeId}", nuzlockeId);
        }
        return Task.CompletedTask;
    }

    public async Task SaveBattleRecordAsync(string nuzlockeId, BattleRecord record)
    {
        var resultsDir = ResolvePath(nuzlockeId, ResultsDirName);
        if (!Directory.Exists(resultsDir))
            Directory.CreateDirectory(resultsDir);

        // Count existing records to generate sequential name
        var existingFiles = Directory.GetFiles(resultsDir, "battle_*.json");
        var nextNumber = existingFiles.Length + 1;

        // Sanitize opponent name for filename
        var opponentSafe = SanitizeFileName(record.OpponentName);
        var fileName = $"battle_{nextNumber:D3}_vs_{opponentSafe}.json";

        await WriteJsonAsync(Path.Combine(resultsDir, fileName), record);
        _logger.LogInformation("Saved battle record {FileName} for {NuzlockeId}", fileName, nuzlockeId);
    }

    public async Task<List<NuzlockeMetadata>> ListNuzlockesAsync(string basePath)
    {
        var result = new List<NuzlockeMetadata>();

        if (!Directory.Exists(basePath))
            return result;

        foreach (var dir in Directory.GetDirectories(basePath))
        {
            var metadataPath = Path.Combine(dir, MetadataFileName);
            if (!File.Exists(metadataPath)) continue;

            try
            {
                var metadata = await ReadJsonAsync<NuzlockeMetadata>(metadataPath);
                if (metadata != null)
                {
                    // Ensure basePath is populated
                    metadata.BasePath = basePath;
                    // Cache the path for future operations
                    _pathCache[metadata.NuzlockeId] = dir;
                    result.Add(metadata);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read metadata from {Path}", metadataPath);
            }
        }

        return result;
    }

    public async Task<NuzlockeMetadata?> GetNuzlockeMetadataAsync(string nuzlockeId)
    {
        var nuzlockePath = GetNuzlockePath(nuzlockeId);
        if (nuzlockePath == null) return null;

        var metadataPath = Path.Combine(nuzlockePath, MetadataFileName);
        if (!File.Exists(metadataPath)) return null;

        return await ReadJsonAsync<NuzlockeMetadata>(metadataPath);
    }

    public string? GetNuzlockePath(string nuzlockeId)
    {
        if (_pathCache.TryGetValue(nuzlockeId, out var path))
            return path;

        return null;
    }

    // --- Private helpers ---

    private SemaphoreSlim GetLock(string nuzlockeId) =>
        _locks.GetOrAdd(nuzlockeId, _ => new SemaphoreSlim(1, 1));

    private string ResolvePath(string nuzlockeId, string fileName)
    {
        var basePath = GetNuzlockePath(nuzlockeId)
            ?? throw new KeyNotFoundException($"Nuzlocke not found: {nuzlockeId}. Use ListNuzlockesAsync or CreateNuzlockeAsync first.");
        return Path.Combine(basePath, fileName);
    }

    private static async Task WriteJsonAsync<T>(string path, T data)
    {
        var json = JsonSerializer.Serialize(data, _writeOptions);
        await File.WriteAllTextAsync(path, json);
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
