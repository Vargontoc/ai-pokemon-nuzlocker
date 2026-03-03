using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Workflows;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.Workflows;

public class WorkflowParametersTests
{
    private static WorkflowParameters FromJson(string json)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        return new WorkflowParameters(dict);
    }

    [Fact]
    public void GetString_ReturnsValue_WhenKeyExists()
    {
        var p = FromJson("""{"name": "pikachu"}""");
        Assert.Equal("pikachu", p.GetString("name"));
    }

    [Fact]
    public void GetString_ReturnsNull_WhenKeyMissing()
    {
        var p = FromJson("""{"name": "pikachu"}""");
        Assert.Null(p.GetString("missing"));
    }

    [Fact]
    public void GetString_ReturnsNull_WhenValueIsNotString()
    {
        var p = FromJson("""{"level": 25}""");
        Assert.Null(p.GetString("level"));
    }

    [Fact]
    public void GetInt_ReturnsValue_WhenKeyExists()
    {
        var p = FromJson("""{"level": 25}""");
        Assert.Equal(25, p.GetInt("level"));
    }

    [Fact]
    public void GetInt_ReturnsNull_WhenKeyMissing()
    {
        var p = FromJson("""{"level": 25}""");
        Assert.Null(p.GetInt("missing"));
    }

    [Fact]
    public void GetInt_ReturnsNull_WhenValueIsString()
    {
        var p = FromJson("""{"level": "high"}""");
        Assert.Null(p.GetInt("level"));
    }

    [Fact]
    public void GetStringArray_ReturnsValues()
    {
        var p = FromJson("""{"species": ["pidgey", "rattata", "pikachu"]}""");
        var result = p.GetStringArray("species");
        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
        Assert.Equal("pidgey", result[0]);
        Assert.Equal("pikachu", result[2]);
    }

    [Fact]
    public void GetStringArray_ReturnsNull_WhenNotArray()
    {
        var p = FromJson("""{"species": "pikachu"}""");
        Assert.Null(p.GetStringArray("species"));
    }

    [Fact]
    public void GetStringArray_ReturnsNull_WhenKeyMissing()
    {
        var p = FromJson("""{}""");
        Assert.Null(p.GetStringArray("species"));
    }

    [Fact]
    public void HasKey_ReturnsTrue_WhenPresent()
    {
        var p = FromJson("""{"name": "pikachu"}""");
        Assert.True(p.HasKey("name"));
    }

    [Fact]
    public void HasKey_ReturnsFalse_WhenMissing()
    {
        var p = FromJson("""{"name": "pikachu"}""");
        Assert.False(p.HasKey("missing"));
    }

    [Fact]
    public void HasKey_IsCaseInsensitive()
    {
        var p = FromJson("""{"Name": "pikachu"}""");
        Assert.True(p.HasKey("name"));
        Assert.True(p.HasKey("NAME"));
    }

    [Fact]
    public void GetObject_DeserializesNestedObject()
    {
        var p = FromJson("""{"pokemon": {"name": "pikachu", "level": 25}}""");
        var obj = p.GetObject<TestPokemon>("pokemon");
        Assert.NotNull(obj);
        Assert.Equal("pikachu", obj!.Name);
        Assert.Equal(25, obj.Level);
    }

    [Fact]
    public void GetObject_ReturnsNull_WhenKeyMissing()
    {
        var p = FromJson("""{}""");
        Assert.Null(p.GetObject<TestPokemon>("pokemon"));
    }

    [Fact]
    public void GetObjectArray_DeserializesList()
    {
        var p = FromJson("""{"team": [{"name": "pikachu", "level": 25}, {"name": "bulbasaur", "level": 15}]}""");
        var list = p.GetObjectArray<TestPokemon>("team");
        Assert.NotNull(list);
        Assert.Equal(2, list!.Count);
        Assert.Equal("bulbasaur", list[1].Name);
    }

    [Fact]
    public void EmptyParameters_AllAccessorsReturnNull()
    {
        var p = new WorkflowParameters();
        Assert.Null(p.GetString("any"));
        Assert.Null(p.GetInt("any"));
        Assert.Null(p.GetStringArray("any"));
        Assert.False(p.HasKey("any"));
    }

    [Fact]
    public void Converter_DeserializesFromJson()
    {
        var json = """{"workflowId": "test", "nuzlockeId": "s1", "parameters": {"species": "pikachu", "level": 5}}""";
        var request = JsonSerializer.Deserialize<WorkflowRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.NotNull(request);
        Assert.Equal("pikachu", request!.Parameters.GetString("species"));
        Assert.Equal(5, request.Parameters.GetInt("level"));
    }

    [Fact]
    public void Converter_HandlesNullParameters()
    {
        var json = """{"workflowId": "test", "nuzlockeId": "s1"}""";
        var request = JsonSerializer.Deserialize<WorkflowRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.NotNull(request);
        Assert.False(request!.Parameters.HasKey("anything"));
    }

    private class TestPokemon
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
    }
}
