using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class NuzlockeAgentTests
{
    private readonly Mock<IAiProvider> _mockAiProvider;
    private readonly Mock<IStateManager> _mockStateManager;
    private readonly NuzlockeAgent _agent;

    public NuzlockeAgentTests()
    {
        _mockAiProvider = new Mock<IAiProvider>();
        _mockStateManager = new Mock<IStateManager>();
        _agent = new NuzlockeAgent(
            _mockAiProvider.Object,
            _mockStateManager.Object,
            NullLogger<NuzlockeAgent>.Instance);
    }

    [Fact]
    public async Task GetAdviceAsync_WithEmptyTeam_IncludesEmptyTeamInContext()
    {
        // Arrange
        var emptyState = new NuzlockeState();
        _mockStateManager.Setup(m => m.GetStateAsync())
            .ReturnsAsync(emptyState);

        _mockAiProvider.Setup(p => p.GetCompletionAsync(
                It.IsAny<string>(),
                It.Is<string>(msg => msg.Contains("TEAM (0/6)") && msg.Contains("No Pokemon in team yet")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("You should catch a starter Pokemon first!");

        // Act
        var advice = await _agent.GetAdviceAsync("What should I do?");

        // Assert
        Assert.Contains("starter Pokemon", advice);
        _mockAiProvider.Verify(p => p.GetCompletionAsync(
            It.IsAny<string>(),
            It.Is<string>(msg => msg.Contains("No Pokemon in team yet")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAdviceAsync_WithActiveTeam_IncludesTeamMembersInContext()
    {
        // Arrange
        var state = new NuzlockeState
        {
            Team = new List<TeamMember>
            {
                new TeamMember
                {
                    Nickname = "Sparky",
                    Species = "pikachu",
                    Level = 15,
                    CurrentHP = 40,
                    MaxHP = 50,
                    Moves = new List<string> { "thundershock", "quick-attack" }
                },
                new TeamMember
                {
                    Nickname = "Bubbles",
                    Species = "squirtle",
                    Level = 12,
                    CurrentHP = 35,
                    MaxHP = 35,
                    Moves = new List<string> { "water-gun", "tackle" }
                }
            }
        };

        _mockStateManager.Setup(m => m.GetStateAsync())
            .ReturnsAsync(state);

        _mockAiProvider.Setup(p => p.GetCompletionAsync(
                It.IsAny<string>(),
                It.Is<string>(msg =>
                    msg.Contains("Sparky") &&
                    msg.Contains("pikachu") &&
                    msg.Contains("Bubbles") &&
                    msg.Contains("squirtle")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Your team has good type coverage with Electric and Water types!");

        // Act
        var advice = await _agent.GetAdviceAsync("How is my team composition?");

        // Assert
        Assert.Contains("type coverage", advice);
        _mockStateManager.Verify(m => m.GetStateAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAdviceAsync_WithDeadPokemon_IncludesGraveyardInContext()
    {
        // Arrange
        var state = new NuzlockeState
        {
            Team = new List<TeamMember>
            {
                new TeamMember { Nickname = "Survivor", Species = "pidgey", Level = 10 }
            },
            DeadPokemon = new List<DeadPokemon>
            {
                new DeadPokemon
                {
                    Nickname = "Fallen",
                    Species = "rattata",
                    Level = 5,
                    DeathLocation = "Route 1",
                    CauseOfDeath = "Critical hit from wild Pidgey"
                }
            }
        };

        _mockStateManager.Setup(m => m.GetStateAsync())
            .ReturnsAsync(state);

        _mockAiProvider.Setup(p => p.GetCompletionAsync(
                It.IsAny<string>(),
                It.Is<string>(msg =>
                    msg.Contains("DEATHS (1)") &&
                    msg.Contains("Fallen") &&
                    msg.Contains("Critical hit from wild Pidgey")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Be careful of critical hits - they're very dangerous in Nuzlocke runs!");

        // Act
        var advice = await _agent.GetAdviceAsync("What should I be careful about?");

        // Assert
        Assert.Contains("critical hits", advice);
        _mockAiProvider.Verify(p => p.GetCompletionAsync(
            It.IsAny<string>(),
            It.Is<string>(msg => msg.Contains("Fallen")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAdviceAsync_WithEncounters_IncludesEncounterHistoryInContext()
    {
        // Arrange
        var state = new NuzlockeState
        {
            Encounters = new Dictionary<string, EncounterRecord>
            {
                ["Route 1"] = new EncounterRecord
                {
                    Location = "Route 1",
                    CapturedSpecies = "pidgey",
                    CapturedNickname = "Birdy",
                    EncounterUsed = true
                },
                ["Route 2"] = new EncounterRecord
                {
                    Location = "Route 2",
                    EncounterUsed = false
                }
            }
        };

        _mockStateManager.Setup(m => m.GetStateAsync())
            .ReturnsAsync(state);

        _mockAiProvider.Setup(p => p.GetCompletionAsync(
                It.IsAny<string>(),
                It.Is<string>(msg =>
                    msg.Contains("Route 1: Caught: Birdy (pidgey)") &&
                    msg.Contains("Route 2: Failed/Skipped")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("You've already used your Route 1 encounter. Consider which Pokemon you want from Route 3.");

        // Act
        var advice = await _agent.GetAdviceAsync("Where should I catch my next Pokemon?");

        // Assert
        Assert.Contains("Route", advice);
    }

    [Fact]
    public async Task GetAdviceAsync_PassesSystemPromptWithNuzlockeRules()
    {
        // Arrange
        var state = new NuzlockeState();
        _mockStateManager.Setup(m => m.GetStateAsync())
            .ReturnsAsync(state);

        _mockAiProvider.Setup(p => p.GetCompletionAsync(
                It.Is<string>(prompt =>
                    prompt.Contains("Nuzlocke Challenge") &&
                    prompt.Contains("If a Pokemon faints") &&
                    prompt.Contains("first Pokemon encountered")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Advice");

        // Act
        await _agent.GetAdviceAsync("Test question");

        // Assert
        _mockAiProvider.Verify(p => p.GetCompletionAsync(
            It.Is<string>(prompt => prompt.Contains("Nuzlocke")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
