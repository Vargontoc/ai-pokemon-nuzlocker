using System.Text.Json;
using System.Collections.Concurrent;
using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

public class NuzlockeSessionManager : INuzlockeSessionManager
{
    private readonly string _registryPath;
    private readonly ILogger<NuzlockeSessionManager> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly SemaphoreSlim _registryLock = new(1, 1);

    private static readonly JsonSerializerOptions _readOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public NuzlockeSessionManager(IConfiguration configuration, ILogger<NuzlockeSessionManager> logger)
    {
        _logger = logger;
        _registryPath = configuration["NuzlockeRegistryPath"] ?? Path.Combine(AppContext.BaseDirectory, "nuzlocke_registry.json");
        EnsureRegistryExists();
    }

    private void EnsureRegistryExists()
    {
        var dir = Path.GetDirectoryName(_registryPath) ?? AppContext.BaseDirectory;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        if (!File.Exists(_registryPath)) File.WriteAllText(_registryPath, JsonSerializer.Serialize(new NuzlockeRegistry()));
    }

    private async Task<NuzlockeRegistry> ReadRegistryAsync()
    {
        var txt = await File.ReadAllTextAsync(_registryPath);
        return JsonSerializer.Deserialize<NuzlockeRegistry>(txt, _readOptions) ?? new NuzlockeRegistry();
    }

    private async Task WriteRegistryAsync(NuzlockeRegistry reg)
    {
        await File.WriteAllTextAsync(_registryPath, JsonSerializer.Serialize(reg, _writeOptions));
    }

    public async Task<NuzlockeSessionInfo> CreateSessionAsync(string name, string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        await _registryLock.WaitAsync();
        try
        {
            var info = new NuzlockeSessionInfo { Name = name, Path = directoryPath };
            var reg = await ReadRegistryAsync();
            reg.Sessions.Add(info);
            await WriteRegistryAsync(reg);

            var filePath = Path.Combine(directoryPath, ".nuzlocke");
            var data = new NuzlockeFileData { SessionId = info.Id };
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(data, _writeOptions));

            _logger.LogInformation("Created session {SessionId} at {Path}", info.Id, directoryPath);
            return info;
        }
        finally
        {
            _registryLock.Release();
        }
    }

    public async Task<NuzlockeSessionInfo?> GetSessionAsync(string sessionId)
    {
        await _registryLock.WaitAsync();
        try
        {
            var reg = await ReadRegistryAsync();
            return reg.Sessions.FirstOrDefault(x => x.Id == sessionId);
        }
        finally
        {
            _registryLock.Release();
        }
    }

    public async Task<List<NuzlockeSessionInfo>> ListSessionsAsync()
    {
        await _registryLock.WaitAsync();
        try
        {
            var reg = await ReadRegistryAsync();
            return reg.Sessions;
        }
        finally
        {
            _registryLock.Release();
        }
    }

    public async Task<bool> DeleteSessionAsync(string sessionId)
    {
        await _registryLock.WaitAsync();
        try
        {
            var reg = await ReadRegistryAsync();
            var s = reg.Sessions.FirstOrDefault(x => x.Id == sessionId);
            if (s == null) return false;

            var filePath = Path.Combine(s.Path, ".nuzlocke");
            if (File.Exists(filePath)) File.Delete(filePath);

            reg.Sessions.Remove(s);
            await WriteRegistryAsync(reg);

            _logger.LogInformation("Deleted session {SessionId}", sessionId);
            return true;
        }
        finally
        {
            _registryLock.Release();
        }
    }

    private SemaphoreSlim GetLock(string sessionId) => _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1,1));

    public async Task<NuzlockeFileData> LoadSessionDataAsync(string sessionId)
    {
        var s = await GetSessionAsync(sessionId);
        if (s == null) throw new KeyNotFoundException($"Session not found: {sessionId}");

        var filePath = Path.Combine(s.Path, ".nuzlocke");
        var sem = GetLock(sessionId);
        await sem.WaitAsync();
        try
        {
            var txt = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<NuzlockeFileData>(txt, _readOptions)
                ?? new NuzlockeFileData { SessionId = sessionId };
        }
        finally { sem.Release(); }
    }

    public async Task SaveSessionDataAsync(string sessionId, NuzlockeFileData data)
    {
        var s = await GetSessionAsync(sessionId);
        if (s == null) throw new KeyNotFoundException($"Session not found: {sessionId}");

        var filePath = Path.Combine(s.Path, ".nuzlocke");
        var sem = GetLock(sessionId);
        await sem.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(data, _writeOptions));
        }
        finally { sem.Release(); }

        // Sync lightweight metadata back to the registry (no extra file reads on list)
        await _registryLock.WaitAsync();
        try
        {
            var reg = await ReadRegistryAsync();
            var entry = reg.Sessions.FirstOrDefault(x => x.Id == sessionId);
            if (entry != null)
            {
                entry.LastUpdated = DateTime.UtcNow;
                entry.Generation = data.GameState.Generation;
                entry.LockeType = data.GameState.LockeType;
                await WriteRegistryAsync(reg);
            }
        }
        finally { _registryLock.Release(); }
    }

    public async Task<bool> UpdateStatusAsync(string sessionId, NuzlockeStatus status)
    {
        await _registryLock.WaitAsync();
        try
        {
            var reg = await ReadRegistryAsync();
            var entry = reg.Sessions.FirstOrDefault(x => x.Id == sessionId);
            if (entry == null) return false;

            entry.Status = status;
            entry.LastUpdated = DateTime.UtcNow;
            await WriteRegistryAsync(reg);
            _logger.LogInformation("Session {SessionId} status updated to {Status}", sessionId, status);
            return true;
        }
        finally { _registryLock.Release(); }
    }
}
