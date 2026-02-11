using System.Text.Json;
using System.Collections.Concurrent;
using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

public class NuzlockeSessionManager : INuzlockeSessionManager
{
    private readonly string _registryPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly object _registryLock = new();

    public NuzlockeSessionManager(IConfiguration configuration)
    {
        _registryPath = configuration["NuzlockeRegistryPath"] ?? Path.Combine(AppContext.BaseDirectory, "nuzlocke_registry.json");
        EnsureRegistryExists();
    }

    private void EnsureRegistryExists()
    {
        var dir = Path.GetDirectoryName(_registryPath) ?? AppContext.BaseDirectory;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        if (!File.Exists(_registryPath)) File.WriteAllText(_registryPath, JsonSerializer.Serialize(new NuzlockeRegistry()));
    }

    private NuzlockeRegistry ReadRegistry()
    {
        lock (_registryLock)
        {
            var txt = File.ReadAllText(_registryPath);
            return JsonSerializer.Deserialize<NuzlockeRegistry>(txt, _jsonOptions) ?? new NuzlockeRegistry();
        }
    }

    private void WriteRegistry(NuzlockeRegistry reg)
    {
        lock (_registryLock)
        {
            File.WriteAllText(_registryPath, JsonSerializer.Serialize(reg, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public async Task<NuzlockeSessionInfo> CreateSessionAsync(string name, string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) throw new DirectoryNotFoundException(directoryPath);

        var info = new NuzlockeSessionInfo { Name = name, Path = directoryPath };
        var reg = ReadRegistry();
        reg.Sessions.Add(info);
        WriteRegistry(reg);

        var filePath = Path.Combine(directoryPath, ".nuzlocke");
        var data = new NuzlockeFileData { SessionId = info.Id };
        var txt = JsonSerializer.Serialize(data, _jsonOptions);
        File.WriteAllText(filePath, txt);

        return info;
    }

    public Task<NuzlockeSessionInfo?> GetSessionAsync(string sessionId)
    {
        var reg = ReadRegistry();
        var s = reg.Sessions.FirstOrDefault(x => x.Id == sessionId);
        return Task.FromResult(s);
    }

    public Task<List<NuzlockeSessionInfo>> ListSessionsAsync()
    {
        var reg = ReadRegistry();
        return Task.FromResult(reg.Sessions);
    }

    public Task<bool> DeleteSessionAsync(string sessionId)
    {
        var reg = ReadRegistry();
        var s = reg.Sessions.FirstOrDefault(x => x.Id == sessionId);
        if (s == null) return Task.FromResult(false);
        var filePath = Path.Combine(s.Path, ".nuzlocke");
        if (File.Exists(filePath)) File.Delete(filePath);
        reg.Sessions.Remove(s);
        WriteRegistry(reg);
        return Task.FromResult(true);
    }

    private SemaphoreSlim GetLock(string sessionId) => _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1,1));

    public async Task<NuzlockeFileData> LoadSessionDataAsync(string sessionId)
    {
        var s = (await GetSessionAsync(sessionId));
        if (s == null) throw new KeyNotFoundException(sessionId);
        var filePath = Path.Combine(s.Path, ".nuzlocke");
        var sem = GetLock(sessionId);
        await sem.WaitAsync();
        try
        {
            var txt = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<NuzlockeFileData>(txt, _jsonOptions) ?? new NuzlockeFileData { SessionId = sessionId };
        }
        finally { sem.Release(); }
    }

    public async Task SaveSessionDataAsync(string sessionId, NuzlockeFileData data)
    {
        var s = (await GetSessionAsync(sessionId));
        if (s == null) throw new KeyNotFoundException(sessionId);
        var filePath = Path.Combine(s.Path, ".nuzlocke");
        var sem = GetLock(sessionId);
        await sem.WaitAsync();
        try
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { sem.Release(); }
    }
}
