using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class NuzlockeFileManagerTests : IDisposable
{
    private readonly NuzlockeFileManager _manager;
    private readonly string _testBasePath;

    private static readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NuzlockeFileManagerTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"nuzlocke_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testBasePath);
        _manager = new NuzlockeFileManager(NullLogger<NuzlockeFileManager>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
            Directory.Delete(_testBasePath, recursive: true);
    }

    // --- CreateNuzlocke ---

    [Fact]
    public async Task CreateNuzlocke_CreatesCompleteStructure()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");

        var nuzlockePath = Path.Combine(_testBasePath, nuzlockeId);
        Assert.True(Directory.Exists(nuzlockePath));
        Assert.True(File.Exists(Path.Combine(nuzlockePath, ".nuzlocke")));
        Assert.True(File.Exists(Path.Combine(nuzlockePath, "game_state.json")));
        Assert.True(Directory.Exists(Path.Combine(nuzlockePath, "memory")));
        Assert.True(Directory.Exists(Path.Combine(nuzlockePath, "results")));
    }

    [Fact]
    public async Task CreateNuzlocke_MetadataContainsCorrectData()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "hardcore");

        var metadataPath = Path.Combine(_testBasePath, nuzlockeId, ".nuzlocke");
        var json = await File.ReadAllTextAsync(metadataPath);
        var metadata = JsonSerializer.Deserialize<NuzlockeMetadata>(json, _readOptions);

        Assert.NotNull(metadata);
        Assert.Equal(nuzlockeId, metadata!.NuzlockeId);
        Assert.Equal(1, metadata.Generation);
        Assert.Equal("hardcore", metadata.LockeType);
        Assert.Equal(_testBasePath, metadata.BasePath);
        Assert.True(metadata.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateNuzlocke_GameStateInitializedWithGenAndType()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "hardcore");

        var statePath = Path.Combine(_testBasePath, nuzlockeId, "game_state.json");
        var json = await File.ReadAllTextAsync(statePath);
        var state = JsonSerializer.Deserialize<NuzlockeState>(json, _readOptions);

        Assert.NotNull(state);
        Assert.Equal(1, state!.Generation);
        Assert.Equal("hardcore", state.LockeType);
        Assert.Empty(state.Team);
    }

    [Fact]
    public async Task CreateNuzlocke_NuzlockeIdHasGuidAndDate()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");

        // Format: {8-char-guid}_{yyyy-MM-dd}
        var parts = nuzlockeId.Split('_', 2);
        Assert.Equal(2, parts.Length);
        Assert.Equal(8, parts[0].Length);
        Assert.True(DateTime.TryParse(parts[1], out _));
    }

    // --- ListNuzlockes ---

    [Fact]
    public async Task ListNuzlockes_DiscoversExistingNuzlockes()
    {
        await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");
        await _manager.CreateNuzlockeAsync(_testBasePath, 1, "hardcore");

        // Create a fresh manager to ensure it discovers from disk
        var freshManager = new NuzlockeFileManager(NullLogger<NuzlockeFileManager>.Instance);
        var list = await freshManager.ListNuzlockesAsync(_testBasePath);

        Assert.Equal(2, list.Count);
        Assert.Contains(list, m => m.LockeType == "standard");
        Assert.Contains(list, m => m.LockeType == "hardcore");
    }

    [Fact]
    public async Task ListNuzlockes_EmptyBasePath_ReturnsEmptyList()
    {
        var emptyPath = Path.Combine(_testBasePath, "empty");
        Directory.CreateDirectory(emptyPath);

        var list = await _manager.ListNuzlockesAsync(emptyPath);
        Assert.Empty(list);
    }

    [Fact]
    public async Task ListNuzlockes_NonexistentPath_ReturnsEmptyList()
    {
        var list = await _manager.ListNuzlockesAsync(Path.Combine(_testBasePath, "nonexistent"));
        Assert.Empty(list);
    }

    // --- Load/Save GameState ---

    [Fact]
    public async Task LoadSaveGameState_RoundTrips()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");

        var state = await _manager.LoadGameStateAsync(nuzlockeId);
        state.Team.Add(new TeamMember { Nickname = "Sparky", Species = "pikachu", Level = 10 });
        await _manager.SaveGameStateAsync(nuzlockeId, state);

        var reloaded = await _manager.LoadGameStateAsync(nuzlockeId);
        Assert.Single(reloaded.Team);
        Assert.Equal("Sparky", reloaded.Team[0].Nickname);
    }

    // --- Load/Save BattleState ---

    [Fact]
    public async Task LoadBattleState_NoBattle_ReturnsNull()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");
        var battle = await _manager.LoadBattleStateAsync(nuzlockeId);
        Assert.Null(battle);
    }

    [Fact]
    public async Task SaveLoadBattleState_RoundTrips()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");

        var battle = new BattleContext
        {
            InBattle = true,
            OpponentName = "Brock",
            TurnCount = 3
        };
        await _manager.SaveBattleStateAsync(nuzlockeId, battle);

        var reloaded = await _manager.LoadBattleStateAsync(nuzlockeId);
        Assert.NotNull(reloaded);
        Assert.True(reloaded!.InBattle);
        Assert.Equal("Brock", reloaded.OpponentName);
        Assert.Equal(3, reloaded.TurnCount);
    }

    [Fact]
    public async Task DeleteBattleState_RemovesFile()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");

        await _manager.SaveBattleStateAsync(nuzlockeId, new BattleContext { InBattle = true, OpponentName = "Brock" });
        var battlePath = Path.Combine(_testBasePath, nuzlockeId, "battle_state.json");
        Assert.True(File.Exists(battlePath));

        await _manager.DeleteBattleStateAsync(nuzlockeId);
        Assert.False(File.Exists(battlePath));
    }

    // --- SaveBattleRecord ---

    [Fact]
    public async Task SaveBattleRecord_CreatesFileInResults()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");

        var record = new BattleRecord
        {
            OpponentName = "Brock",
            BattleType = "gym",
            Outcome = "won",
            TurnCount = 5
        };
        await _manager.SaveBattleRecordAsync(nuzlockeId, record);

        var resultsDir = Path.Combine(_testBasePath, nuzlockeId, "results");
        var files = Directory.GetFiles(resultsDir, "battle_*.json");
        Assert.Single(files);
        Assert.Contains("battle_001_vs_brock.json", files[0]);
    }

    [Fact]
    public async Task SaveBattleRecord_SequentialNumbering()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");

        await _manager.SaveBattleRecordAsync(nuzlockeId, new BattleRecord { OpponentName = "Brock", Outcome = "won" });
        await _manager.SaveBattleRecordAsync(nuzlockeId, new BattleRecord { OpponentName = "Misty", Outcome = "won" });

        var resultsDir = Path.Combine(_testBasePath, nuzlockeId, "results");
        var files = Directory.GetFiles(resultsDir, "battle_*.json").OrderBy(f => f).ToArray();
        Assert.Equal(2, files.Length);
        Assert.Contains("battle_001_vs_brock.json", files[0]);
        Assert.Contains("battle_002_vs_misty.json", files[1]);
    }

    // --- GetNuzlockeMetadata ---

    [Fact]
    public async Task GetNuzlockeMetadata_ReturnsMetadata()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");

        var metadata = await _manager.GetNuzlockeMetadataAsync(nuzlockeId);
        Assert.NotNull(metadata);
        Assert.Equal(nuzlockeId, metadata!.NuzlockeId);
        Assert.Equal(1, metadata.Generation);
    }

    [Fact]
    public async Task GetNuzlockeMetadata_UnknownId_ReturnsNull()
    {
        var metadata = await _manager.GetNuzlockeMetadataAsync("nonexistent_2026-01-01");
        Assert.Null(metadata);
    }
}
