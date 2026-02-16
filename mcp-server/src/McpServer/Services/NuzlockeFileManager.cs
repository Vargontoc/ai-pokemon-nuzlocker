using System.Collections.Concurrent;
using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Repositories;

namespace es.vargontoc.nuzlocke.ai.Services;

public class NuzlockeFileManager : INuzlockeFileManager
{
    private readonly ILogger<NuzlockeFileManager> _logger;
    private readonly INuzlockeRegistryRepository _registry;
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

    public NuzlockeFileManager(ILogger<NuzlockeFileManager> logger, INuzlockeRegistryRepository registry)
    {
        _logger = logger;
        _registry = registry;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var all = await _registry.GetAllAsync();
            var loaded = 0;
            var skipped = 0;

            foreach (var (id, path) in all)
            {
                if (!Directory.Exists(path))
                {
                    _logger.LogWarning("Registry entry {NuzlockeId} skipped: directory not found at {Path}", id, path);
                    skipped++;
                    continue;
                }

                // Validate .nuzlocke metadata matches SQLite registry
                var metadataPath = Path.Combine(path, MetadataFileName);
                if (!File.Exists(metadataPath))
                {
                    _logger.LogWarning("Registry entry {NuzlockeId} skipped: .nuzlocke metadata file not found at {Path}", id, path);
                    skipped++;
                    continue;
                }

                var metadata = await ReadJsonAsync<NuzlockeMetadata>(metadataPath);
                if (metadata == null)
                {
                    _logger.LogWarning("Registry entry {NuzlockeId} skipped: failed to deserialize .nuzlocke metadata", id);
                    skipped++;
                    continue;
                }

                // Validate NuzlockeId matches
                if (metadata.NuzlockeId != id)
                {
                    _logger.LogWarning("Registry entry {NuzlockeId} skipped: metadata NuzlockeId mismatch (expected {Expected}, got {Actual})",
                        id, id, metadata.NuzlockeId);
                    skipped++;
                    continue;
                }

                // Validate Path consistency (metadata.BasePath + nuzlockeId should resolve to registry path)
                var expectedPath = Path.Combine(metadata.BasePath, metadata.NuzlockeId);
                if (!PathsAreEquivalent(expectedPath, path))
                {
                    _logger.LogWarning("Registry entry {NuzlockeId} skipped: path mismatch (registry: {RegistryPath}, metadata resolves to: {MetadataPath})",
                        id, path, expectedPath);
                    skipped++;
                    continue;
                }

                _pathCache.TryAdd(id, path);
                loaded++;
            }

            _logger.LogInformation("Registry initialized: {Loaded} loaded, {Skipped} skipped", loaded, skipped);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load nuzlocke registry from database");
        }
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

        // Cache the path and persist to SQLite
        _pathCache[nuzlockeId] = nuzlockePath;
        await _registry.RegisterAsync(nuzlockeId, nuzlockePath);

        _logger.LogInformation("Created nuzlocke {NuzlockeId} at {Path}", nuzlockeId, nuzlockePath);
        return nuzlockeId;
    }

    public async Task<NuzlockeState> LoadGameStateAsync(string nuzlockeId)
    {
        var path = await ResolvePathAsync(nuzlockeId, GameStateFileName);
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
        var path = await ResolvePathAsync(nuzlockeId, GameStateFileName);
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
        var path = await ResolvePathAsync(nuzlockeId, BattleStateFileName);
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
        var path = await ResolvePathAsync(nuzlockeId, BattleStateFileName);
        var sem = GetLock(nuzlockeId);
        await sem.WaitAsync();
        try
        {
            await WriteJsonAsync(path, battle);
        }
        finally { sem.Release(); }
    }

    public async Task DeleteBattleStateAsync(string nuzlockeId)
    {
        var path = await ResolvePathAsync(nuzlockeId, BattleStateFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            _logger.LogInformation("Deleted battle state for {NuzlockeId}", nuzlockeId);
        }
    }

    public async Task SaveBattleRecordAsync(string nuzlockeId, BattleRecord record)
    {
        var resultsDir = await ResolvePathAsync(nuzlockeId, ResultsDirName);
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
                    // Cache the path and persist to SQLite
                    _pathCache[metadata.NuzlockeId] = dir;
                    await _registry.RegisterAsync(metadata.NuzlockeId, dir);
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
        var nuzlockePath = await GetNuzlockePathAsync(nuzlockeId);
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

    public async Task<string?> GetNuzlockePathAsync(string nuzlockeId)
    {
        // L1: in-memory cache
        if (_pathCache.TryGetValue(nuzlockeId, out var cached))
            return cached;

        // L2: SQLite registry
        var dbPath = await _registry.GetPathAsync(nuzlockeId);
        if (dbPath != null && Directory.Exists(dbPath))
        {
            _pathCache[nuzlockeId] = dbPath;
            return dbPath;
        }

        return null;
    }

    // --- Private helpers ---

    private SemaphoreSlim GetLock(string nuzlockeId) =>
        _locks.GetOrAdd(nuzlockeId, _ => new SemaphoreSlim(1, 1));

    private async Task<string> ResolvePathAsync(string nuzlockeId, string fileName)
    {
        var basePath = await GetNuzlockePathAsync(nuzlockeId)
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

    private static bool PathsAreEquivalent(string path1, string path2)
    {
        var normalized1 = Path.GetFullPath(path1).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalized2 = Path.GetFullPath(path2).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(normalized1, normalized2, StringComparison.OrdinalIgnoreCase);
    }
}
