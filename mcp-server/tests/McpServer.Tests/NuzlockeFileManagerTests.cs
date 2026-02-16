using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Repositories;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class NuzlockeFileManagerTests : IDisposable
{
    private readonly NuzlockeFileManager _manager;
    private readonly PokeDbContext _dbContext;
    private readonly string _testBasePath;

    private static readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NuzlockeFileManagerTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"nuzlocke_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testBasePath);

        _dbContext = CreateDbContext();
        var registry = new NuzlockeRegistryRepository(_dbContext);
        _manager = new NuzlockeFileManager(NullLogger<NuzlockeFileManager>.Instance, registry);
    }

    private static PokeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PokeDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var ctx = new PokeDbContext(options);
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
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

        // Create a fresh manager with a fresh DB to ensure it discovers from disk
        using var freshDb = CreateDbContext();
        var freshRegistry = new NuzlockeRegistryRepository(freshDb);
        var freshManager = new NuzlockeFileManager(NullLogger<NuzlockeFileManager>.Instance, freshRegistry);
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

    // --- Registry persistence (server restart via SQLite) ---

    [Fact]
    public async Task Registry_NewManagerFindsNuzlockeAfterRestart()
    {
        // Create a nuzlocke — persisted to both disk and SQLite
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");
        Assert.NotNull(_manager.GetNuzlockePath(nuzlockeId));

        // Simulate server restart: new manager, SAME database (shared SQLite)
        var freshRegistry = new NuzlockeRegistryRepository(_dbContext);
        var freshManager = new NuzlockeFileManager(NullLogger<NuzlockeFileManager>.Instance, freshRegistry);
        await freshManager.InitializeAsync();

        // The fresh manager should find the nuzlocke via the SQLite registry
        Assert.NotNull(freshManager.GetNuzlockePath(nuzlockeId));
        var state = await freshManager.LoadGameStateAsync(nuzlockeId);
        Assert.Equal(1, state.Generation);
    }

    [Fact]
    public async Task Registry_NewManagerLoadsMetadataAfterRestart()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "hardcore");

        var freshRegistry = new NuzlockeRegistryRepository(_dbContext);
        var freshManager = new NuzlockeFileManager(NullLogger<NuzlockeFileManager>.Instance, freshRegistry);
        await freshManager.InitializeAsync();

        var metadata = await freshManager.GetNuzlockeMetadataAsync(nuzlockeId);
        Assert.NotNull(metadata);
        Assert.Equal(nuzlockeId, metadata!.NuzlockeId);
        Assert.Equal("hardcore", metadata.LockeType);
    }

    [Fact]
    public async Task Registry_DeletedDirectoryNotLoadedOnRestart()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");

        // Delete the nuzlocke directory to simulate manual removal
        var nuzlockePath = _manager.GetNuzlockePath(nuzlockeId)!;
        Directory.Delete(nuzlockePath, recursive: true);

        // Fresh manager should skip the entry since the directory no longer exists
        var freshRegistry = new NuzlockeRegistryRepository(_dbContext);
        var freshManager = new NuzlockeFileManager(NullLogger<NuzlockeFileManager>.Instance, freshRegistry);
        await freshManager.InitializeAsync();

        Assert.Null(freshManager.GetNuzlockePath(nuzlockeId));
    }

    [Fact]
    public async Task Registry_PersistsToSQLite()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");

        // Verify the registry entry exists in SQLite directly
        var dbEntry = await _dbContext.NuzlockeRegistries
            .FirstOrDefaultAsync(r => r.NuzlockeId == nuzlockeId);
        Assert.NotNull(dbEntry);
        Assert.Contains(nuzlockeId, dbEntry!.Path);
    }

    // --- Metadata validation on InitializeAsync ---

    [Fact]
    public async Task Registry_TamperedMetadataId_SkippedOnRestart()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");
        var nuzlockePath = _manager.GetNuzlockePath(nuzlockeId)!;

        // Tamper: overwrite .nuzlocke with a different NuzlockeId
        var tampered = new NuzlockeMetadata
        {
            NuzlockeId = "tampered_2026-01-01",
            Generation = 1,
            LockeType = "standard",
            CreatedAt = DateTime.UtcNow,
            BasePath = _testBasePath
        };
        await File.WriteAllTextAsync(
            Path.Combine(nuzlockePath, ".nuzlocke"),
            JsonSerializer.Serialize(tampered, _readOptions));

        var freshRegistry = new NuzlockeRegistryRepository(_dbContext);
        var freshManager = new NuzlockeFileManager(NullLogger<NuzlockeFileManager>.Instance, freshRegistry);
        await freshManager.InitializeAsync();

        Assert.Null(freshManager.GetNuzlockePath(nuzlockeId));
    }

    [Fact]
    public async Task Registry_TamperedMetadataPath_SkippedOnRestart()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");
        var nuzlockePath = _manager.GetNuzlockePath(nuzlockeId)!;

        // Tamper: overwrite .nuzlocke with a different BasePath
        var tampered = new NuzlockeMetadata
        {
            NuzlockeId = nuzlockeId,
            Generation = 1,
            LockeType = "standard",
            CreatedAt = DateTime.UtcNow,
            BasePath = "/some/other/path"
        };
        await File.WriteAllTextAsync(
            Path.Combine(nuzlockePath, ".nuzlocke"),
            JsonSerializer.Serialize(tampered, _readOptions));

        var freshRegistry = new NuzlockeRegistryRepository(_dbContext);
        var freshManager = new NuzlockeFileManager(NullLogger<NuzlockeFileManager>.Instance, freshRegistry);
        await freshManager.InitializeAsync();

        Assert.Null(freshManager.GetNuzlockePath(nuzlockeId));
    }

    [Fact]
    public async Task Registry_MissingMetadataFile_SkippedOnRestart()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "standard");
        var nuzlockePath = _manager.GetNuzlockePath(nuzlockeId)!;

        // Delete only the .nuzlocke metadata file (directory still exists)
        File.Delete(Path.Combine(nuzlockePath, ".nuzlocke"));

        var freshRegistry = new NuzlockeRegistryRepository(_dbContext);
        var freshManager = new NuzlockeFileManager(NullLogger<NuzlockeFileManager>.Instance, freshRegistry);
        await freshManager.InitializeAsync();

        Assert.Null(freshManager.GetNuzlockePath(nuzlockeId));
    }

    [Fact]
    public async Task Registry_ValidMetadata_LoadedOnRestart()
    {
        var nuzlockeId = await _manager.CreateNuzlockeAsync(_testBasePath, 1, "hardcore");

        // Fresh manager with same DB — metadata should pass validation
        var freshRegistry = new NuzlockeRegistryRepository(_dbContext);
        var freshManager = new NuzlockeFileManager(NullLogger<NuzlockeFileManager>.Instance, freshRegistry);
        await freshManager.InitializeAsync();

        Assert.NotNull(freshManager.GetNuzlockePath(nuzlockeId));

        var metadata = await freshManager.GetNuzlockeMetadataAsync(nuzlockeId);
        Assert.NotNull(metadata);
        Assert.Equal(nuzlockeId, metadata!.NuzlockeId);
        Assert.Equal("hardcore", metadata.LockeType);
    }
}
