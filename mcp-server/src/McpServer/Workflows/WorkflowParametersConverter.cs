using System.Text.Json;
using System.Text.Json.Serialization;

namespace es.vargontoc.nuzlocke.ai.Workflows;

/// <summary>
/// Converts a flat JSON object to/from WorkflowParameters (Dictionary&lt;string, JsonElement&gt; backing store)
/// </summary>
public class WorkflowParametersConverter : JsonConverter<WorkflowParameters>
{
    public override WorkflowParameters Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new WorkflowParameters();

        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options)
            ?? new Dictionary<string, JsonElement>();
        return new WorkflowParameters(dict);
    }

    public override void Write(Utf8JsonWriter writer, WorkflowParameters value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.RawData, options);
    }
}
