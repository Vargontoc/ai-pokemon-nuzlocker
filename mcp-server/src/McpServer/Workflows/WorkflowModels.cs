using System.Text.Json;
using System.Text.Json.Serialization;

namespace es.vargontoc.nuzlocke.ai.Workflows;

/// <summary>
/// Incoming request from the web app
/// </summary>
public class WorkflowRequest
{
    public required string WorkflowId { get; set; }
    public required string NuzlockeId { get; set; }

    [JsonConverter(typeof(WorkflowParametersConverter))]
    public WorkflowParameters Parameters { get; set; } = new();

    public string Language { get; set; } = "en-US";
}

/// <summary>
/// Generic parameter bag backed by Dictionary&lt;string, JsonElement&gt;.
/// Each workflow reads what it needs via typed accessors.
/// </summary>
[JsonConverter(typeof(WorkflowParametersConverter))]
public class WorkflowParameters
{
    private readonly Dictionary<string, JsonElement> _data = new(StringComparer.OrdinalIgnoreCase);

    public WorkflowParameters() { }

    public WorkflowParameters(Dictionary<string, JsonElement> data)
    {
        foreach (var kv in data)
            _data[kv.Key] = kv.Value;
    }

    public string? GetString(string key) =>
        _data.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() : null;

    public int? GetInt(string key) =>
        _data.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var val)
            ? val : null;

    public List<string>? GetStringArray(string key) =>
        _data.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Array
            ? el.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .ToList()
            : null;

    public T? GetObject<T>(string key) where T : class =>
        _data.TryGetValue(key, out var el)
            ? JsonSerializer.Deserialize<T>(el.GetRawText(), WorkflowJsonOptions.Default)
            : null;

    public List<T>? GetObjectArray<T>(string key) where T : class =>
        _data.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<List<T>>(el.GetRawText(), WorkflowJsonOptions.Default)
            : null;

    public bool HasKey(string key) => _data.ContainsKey(key);

    internal IReadOnlyDictionary<string, JsonElement> RawData => _data;
}

/// <summary>
/// Structured result from a workflow execution
/// </summary>
public class WorkflowResult
{
    public required string WorkflowId { get; set; }
    public bool Success { get; set; }

    /// <summary>
    /// Deterministic state changes that were applied
    /// </summary>
    public List<StateMutation> Mutations { get; set; } = new();

    /// <summary>
    /// Correlation ID linking this HTTP response to the async WebSocket advice stream.
    /// Null when advice was generated synchronously or skipped.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// LLM-generated strategic advice
    /// </summary>
    public string? Advice { get; set; }

    /// <summary>
    /// Fetched data for the frontend (e.g., pokemon info, item info)
    /// </summary>
    public Dictionary<string, object?> Data { get; set; } = new();

    /// <summary>
    /// Validation or execution errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    public static WorkflowResult Failure(string workflowId, params string[] errors) => new()
    {
        WorkflowId = workflowId,
        Success = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// Describes a state change that was applied
/// </summary>
public class StateMutation
{
    public required string Type { get; set; }
    public required string Description { get; set; }
}

/// <summary>
/// Result of executing only the deterministic part of a workflow (no LLM call).
/// Contains the workflow result plus the prompts needed for async advice generation.
/// </summary>
public class DeterministicResult
{
    public required WorkflowResult Result { get; set; }
    public string? SystemPrompt { get; set; }
    public string? UserMessage { get; set; }
}

internal static class WorkflowJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
