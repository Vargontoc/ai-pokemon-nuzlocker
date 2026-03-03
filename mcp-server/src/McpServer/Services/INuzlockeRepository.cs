using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// Unified repository for all Nuzlocke persistent operations.
///
/// Storage layout:
///   {NuzlockeBasePath}/{id}/
///     .nuzlocke          — NuzlockeMetadata (copia local)
///     game_state.json    — NuzlockeState
///     battle_state.json  — BattleContext (only during an active battle)
///     memory/            — agent.json, conversation.json
///     results/           — individual BattleRecords
///
/// Metadata authoritative store: SQLite (NuzlockeMetadatas table).
/// Path is deterministic: {NuzlockeBasePath}/{id}/ — no cache needed.
/// </summary>
public interface INuzlockeRepository
{
    // ── Lifecycle ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new nuzlocke: generates UUID, creates directory structure,
    /// writes .nuzlocke metadata (IsInitialized = false, Status = Building),
    /// inserts in SQLite.
    /// </summary>
    Task<NuzlockeMetadata> CreateAsync(string name, LockeType lockeType = LockeType.Standard, int generation = 1, string? descripcion = null);

    /// <summary>Reads metadata from SQLite. Returns null if not found.</summary>
    Task<NuzlockeMetadata?> GetMetadataAsync(string nuzlockeId);

    /// <summary>Updates metadata in SQLite and overwrites .nuzlocke file.</summary>
    Task SaveMetadataAsync(NuzlockeMetadata metadata);

    /// <summary>Lists all entries from SQLite (lightweight, no game state).</summary>
    Task<List<NuzlockeMetadata>> ListAsync();

    /// <summary>
    /// Removes the nuzlocke from SQLite and deletes its directory.
    /// Returns false if not found.
    /// </summary>
    Task<bool> DeleteAsync(string nuzlockeId);

    /// <summary>Updates only Status + LastUpdated in SQLite and .nuzlocke file.</summary>
    Task<bool> UpdateStatusAsync(string nuzlockeId, NuzlockeStatus status);

    // ── Path resolution ────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the full directory path for a nuzlockeId.
    /// Returns null if not found in SQLite.
    /// </summary>
    Task<string?> GetNuzlockePathAsync(string nuzlockeId);

    // ── Game state ─────────────────────────────────────────────────────────

    Task<NuzlockeState> GetGameStateAsync(string nuzlockeId);
    Task SaveGameStateAsync(string nuzlockeId, NuzlockeState state);

    // ── Battle state ───────────────────────────────────────────────────────

    /// <summary>Returns null if no active battle.</summary>
    Task<BattleContext?> GetBattleStateAsync(string nuzlockeId);
    Task SaveBattleStateAsync(string nuzlockeId, BattleContext context);
    Task DeleteBattleStateAsync(string nuzlockeId);

    // ── Agent memory ───────────────────────────────────────────────────────

    Task<AgentMemory> GetAgentMemoryAsync(string nuzlockeId);
    Task SaveAgentMemoryAsync(string nuzlockeId, AgentMemory memory);

    // ── Battle records ─────────────────────────────────────────────────────

    Task SaveBattleRecordAsync(string nuzlockeId, BattleRecord record);
}
