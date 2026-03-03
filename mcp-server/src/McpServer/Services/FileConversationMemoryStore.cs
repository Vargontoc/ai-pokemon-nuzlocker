using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// Stores conversation history in {nuzlockePath}/memory/conversation.json.
/// Uses INuzlockeRepository to resolve the nuzlocke path from a session ID.
/// </summary>
public class FileConversationMemoryStore : IConversationMemoryStore
{
    private readonly INuzlockeRepository _repository;
    private readonly ILogger<FileConversationMemoryStore> _logger;

    private const string ConversationFileName = "conversation.json";
    private const string MemoryDirName = "memory";

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FileConversationMemoryStore(INuzlockeRepository repository, ILogger<FileConversationMemoryStore> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<ConversationEntry>> LoadAsync(string sessionId)
    {
        var path = await ResolveConversationPathAsync(sessionId);
        if (path == null)
            return new List<ConversationEntry>();

        try
        {
            if (!File.Exists(path))
                return new List<ConversationEntry>();

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<ConversationEntry>>(json, _readOptions)
                   ?? new List<ConversationEntry>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load conversation history for session {SessionId}", sessionId);
            return new List<ConversationEntry>();
        }
    }

    public async Task AppendAsync(string sessionId, ConversationEntry entry)
    {
        var path = await ResolveConversationPathAsync(sessionId);
        if (path == null)
        {
            _logger.LogWarning("Cannot append conversation entry: nuzlocke path not found for session {SessionId}", sessionId);
            return;
        }

        try
        {
            // Ensure memory directory exists
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);

            // Load existing, append new entry, save back
            var entries = new List<ConversationEntry>();
            if (File.Exists(path))
            {
                var existing = await File.ReadAllTextAsync(path);
                entries = JsonSerializer.Deserialize<List<ConversationEntry>>(existing, _readOptions)
                          ?? new List<ConversationEntry>();
            }

            entries.Add(entry);

            var json = JsonSerializer.Serialize(entries, _writeOptions);
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to append conversation entry for session {SessionId}", sessionId);
        }
    }

    private async Task<string?> ResolveConversationPathAsync(string sessionId)
    {
        var nuzlockePath = await _repository.GetNuzlockePathAsync(sessionId);
        if (nuzlockePath == null)
            return null;

        return Path.Combine(nuzlockePath, MemoryDirName, ConversationFileName);
    }
}
