using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class ToolExecutorTests
{
    private readonly Mock<IStateManager> _mockStateManager;
    private readonly Mock<IPokeApiConnector> _mockPokeApiConnector;
    private readonly Mock<ILogger<ToolExecutor>> _mockLogger;
    private readonly ToolExecutor _executor;

    public ToolExecutorTests()
    {
        _mockStateManager = new Mock<IStateManager>();
        _mockPokeApiConnector = new Mock<IPokeApiConnector>();
        _mockLogger = new Mock<ILogger<ToolExecutor>>();
        _executor = new ToolExecutor(_mockStateManager.Object, _mockPokeApiConnector.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_GetGameState_ReturnsCurrentState()
    {
        // Arrange
        var expectedState = new NuzlockeState
        {
            Team = new List<TeamMember>
            {
                new TeamMember { Nickname = "Sparky", Species = "pikachu", Level = 15 }
            }
        };

        _mockStateManager.Setup(m => m.GetStateAsync()).ReturnsAsync(expectedState);

        var toolCall = new ToolCall
        {
            Id = "call_123",
            Name = "get_game_state",
            ArgumentsJson = "{}"
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Equal("call_123", result.ToolCallId);
        Assert.Equal("get_game_state", result.ToolName);
        Assert.Contains("Sparky", result.Content);
        Assert.Contains("pikachu", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_AddToTeam_CallsStateManager()
    {
        // Arrange
        _mockStateManager.Setup(m => m.AddToTeamAsync(It.IsAny<TeamMember>()))
            .ReturnsAsync(true);

        var args = new
        {
            nickname = "Blaze",
            species = "charmander",
            level = 5,
            caughtAt = "Route 1",
            currentHP = 20,
            maxHP = 20,
            moves = "scratch,growl"
        };

        var toolCall = new ToolCall
        {
            Id = "call_456",
            Name = "add_to_team",
            ArgumentsJson = JsonSerializer.Serialize(args, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Equal("call_456", result.ToolCallId);
        Assert.Equal("add_to_team", result.ToolName);
        Assert.Contains("success", result.Content);
        Assert.Contains("Blaze", result.Content);

        _mockStateManager.Verify(m => m.AddToTeamAsync(It.Is<TeamMember>(tm =>
            tm.Nickname == "Blaze" &&
            tm.Species == "charmander" &&
            tm.Level == 5 &&
            tm.Moves.Count == 2 &&
            tm.Moves.Contains("scratch")
        )), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MarkAsDead_CallsStateManagerWithCorrectParams()
    {
        // Arrange
        _mockStateManager.Setup(m => m.MarkAsDeadAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(true);

        var args = new
        {
            nickname = "Sparky",
            deathLocation = "Cerulean Gym",
            causeOfDeath = "Defeated by Misty's Starmie"
        };

        var toolCall = new ToolCall
        {
            Id = "call_789",
            Name = "mark_as_dead",
            ArgumentsJson = JsonSerializer.Serialize(args, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Equal("call_789", result.ToolCallId);
        Assert.Contains("success", result.Content);

        _mockStateManager.Verify(m => m.MarkAsDeadAsync(
            "Sparky",
            "Cerulean Gym",
            "Defeated by Misty's Starmie"
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MoveToPC_CallsStateManager()
    {
        // Arrange
        _mockStateManager.Setup(m => m.MoveToPCAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var args = new { nickname = "Pidgey" };
        var toolCall = new ToolCall
        {
            Id = "call_101",
            Name = "move_to_pc",
            ArgumentsJson = JsonSerializer.Serialize(args, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Contains("success", result.Content);
        _mockStateManager.Verify(m => m.MoveToPCAsync("Pidgey"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RecordEncounter_CallsStateManager()
    {
        // Arrange
        _mockStateManager.Setup(m => m.RecordEncounterAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(true);

        var args = new
        {
            location = "Route 1",
            capturedSpecies = "rattata",
            capturedNickname = "Whiskers"
        };

        var toolCall = new ToolCall
        {
            Id = "call_202",
            Name = "record_encounter",
            ArgumentsJson = JsonSerializer.Serialize(args, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Contains("success", result.Content);
        Assert.Contains("Route 1", result.Content);

        _mockStateManager.Verify(m => m.RecordEncounterAsync(
            "Route 1",
            "rattata",
            "Whiskers"
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownTool_ReturnsError()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "call_999",
            Name = "unknown_tool",
            ArgumentsJson = "{}"
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Equal("call_999", result.ToolCallId);
        Assert.Contains("error", result.Content.ToLower());
        Assert.Contains("success", result.Content.ToLower());
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsError()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "call_error",
            Name = "add_to_team",
            ArgumentsJson = "invalid json"
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Contains("error", result.Content.ToLower());
    }

    [Fact]
    public async Task ExecuteAsync_GetPokemon_ReturnsPokeApiData()
    {
        // Arrange
        var mockPokemon = new PokemonData
        {
            Id = 25,
            Name = "pikachu",
            Height = 4,
            Weight = 60,
            BaseExperience = 112,
            Stats = new List<PokemonStatSlot>
            {
                new PokemonStatSlot { BaseStat = 35, Effort = 0, Stat = new NamedApiResource { Name = "hp" } }
            },
            Types = new List<PokemonTypeSlot>
            {
                new PokemonTypeSlot { Slot = 1, Type = new NamedApiResource { Name = "electric" } }
            }
        };

        _mockPokeApiConnector.Setup(m => m.GetPokemonAsync("pikachu"))
            .ReturnsAsync(mockPokemon);

        var args = new { nameOrId = "pikachu" };
        var toolCall = new ToolCall
        {
            Id = "call_poke_1",
            Name = "get_pokemon",
            ArgumentsJson = JsonSerializer.Serialize(args, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Equal("call_poke_1", result.ToolCallId);
        Assert.Equal("get_pokemon", result.ToolName);
        Assert.Contains("pikachu", result.Content);
        Assert.Contains("25", result.Content);

        _mockPokeApiConnector.Verify(m => m.GetPokemonAsync("pikachu"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_GetMove_ReturnsPokeApiData()
    {
        // Arrange
        var mockMove = new MoveData
        {
            Id = 85,
            Name = "thunderbolt",
            Accuracy = 100,
            Power = 90,
            Pp = 15,
            Type = new NamedApiResource { Name = "electric" },
            DamageClass = new NamedApiResource { Name = "special" }
        };

        _mockPokeApiConnector.Setup(m => m.GetMoveAsync("thunderbolt"))
            .ReturnsAsync(mockMove);

        var args = new { nameOrId = "thunderbolt" };
        var toolCall = new ToolCall
        {
            Id = "call_move_1",
            Name = "get_move",
            ArgumentsJson = JsonSerializer.Serialize(args, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Equal("call_move_1", result.ToolCallId);
        Assert.Equal("get_move", result.ToolName);
        Assert.Contains("thunderbolt", result.Content);
        Assert.Contains("90", result.Content);

        _mockPokeApiConnector.Verify(m => m.GetMoveAsync("thunderbolt"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_GetType_ReturnsPokeApiData()
    {
        // Arrange
        var mockType = new TypeData
        {
            Id = 10,
            Name = "fire",
            DamageRelations = new TypeDamageRelations
            {
                DoubleDamageFrom = new List<NamedApiResource>
                {
                    new NamedApiResource { Name = "water" }
                },
                DoubleDamageTo = new List<NamedApiResource>
                {
                    new NamedApiResource { Name = "grass" }
                }
            }
        };

        _mockPokeApiConnector.Setup(m => m.GetTypeAsync("fire"))
            .ReturnsAsync(mockType);

        var args = new { nameOrId = "fire" };
        var toolCall = new ToolCall
        {
            Id = "call_type_1",
            Name = "get_type",
            ArgumentsJson = JsonSerializer.Serialize(args, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Equal("call_type_1", result.ToolCallId);
        Assert.Equal("get_type", result.ToolName);
        Assert.Contains("fire", result.Content);
        Assert.Contains("water", result.Content);

        _mockPokeApiConnector.Verify(m => m.GetTypeAsync("fire"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_GetAbility_ReturnsPokeApiData()
    {
        // Arrange
        var mockAbility = new AbilityData
        {
            Id = 65,
            Name = "overgrow",
            EffectEntries = new List<AbilityEffectEntry>
            {
                new AbilityEffectEntry
                {
                    Effect = "Increases Grass-type move power by 50% when HP is below 1/3",
                    ShortEffect = "Boosts Grass moves in a pinch",
                    Language = new NamedApiResource { Name = "en" }
                }
            }
        };

        _mockPokeApiConnector.Setup(m => m.GetAbilityAsync("overgrow"))
            .ReturnsAsync(mockAbility);

        var args = new { nameOrId = "overgrow" };
        var toolCall = new ToolCall
        {
            Id = "call_ability_1",
            Name = "get_ability",
            ArgumentsJson = JsonSerializer.Serialize(args, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Equal("call_ability_1", result.ToolCallId);
        Assert.Equal("get_ability", result.ToolName);
        Assert.Contains("overgrow", result.Content);
        Assert.Contains("65", result.Content);

        _mockPokeApiConnector.Verify(m => m.GetAbilityAsync("overgrow"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_GetItem_ReturnsPokeApiData()
    {
        // Arrange
        var mockItem = new ItemData
        {
            Id = 1,
            Name = "master-ball",
            Cost = 0,
            Category = new ItemCategory { Name = "special-balls" },
            EffectEntries = new List<ItemEffectEntry>
            {
                new ItemEffectEntry
                {
                    Effect = "Catches any Pokemon without fail",
                    ShortEffect = "Never misses",
                    Language = new NamedApiResource { Name = "en" }
                }
            }
        };

        _mockPokeApiConnector.Setup(m => m.GetItemAsync("master-ball"))
            .ReturnsAsync(mockItem);

        var args = new { nameOrId = "master-ball" };
        var toolCall = new ToolCall
        {
            Id = "call_item_1",
            Name = "get_item",
            ArgumentsJson = JsonSerializer.Serialize(args, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Equal("call_item_1", result.ToolCallId);
        Assert.Equal("get_item", result.ToolName);
        Assert.Contains("master-ball", result.Content);
        Assert.Contains("1", result.Content);

        _mockPokeApiConnector.Verify(m => m.GetItemAsync("master-ball"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_GetPokemon_NotFound_ReturnsError()
    {
        // Arrange
        _mockPokeApiConnector.Setup(m => m.GetPokemonAsync("unknown"))
            .ReturnsAsync((PokemonData?)null);

        var args = new { nameOrId = "unknown" };
        var toolCall = new ToolCall
        {
            Id = "call_poke_err",
            Name = "get_pokemon",
            ArgumentsJson = JsonSerializer.Serialize(args, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Contains("error", result.Content);
        Assert.Contains("unknown", result.Content);
    }
}
