namespace es.vargontoc.nuzlocke.ai.Models;

/// <summary>
/// Calculated stats for a Pokemon (HP, Attack, Defense, Speed, Special in Gen 1)
/// </summary>
public class PokemonStats
{
    public int HP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Special { get; set; }
}

/// <summary>
/// Representa el estado completo de una partida Nuzlocke
/// </summary>
public class NuzlockeState
{
    /// <summary>
    /// Equipo actual (máximo 6 Pokemon)
    /// </summary>
    public List<TeamMember> Team { get; set; } = new();

    /// <summary>
    /// Pokemon que han muerto durante la partida (cementerio Nuzlocke)
    /// </summary>
    public List<DeadPokemon> DeadPokemon { get; set; } = new();

    /// <summary>
    /// Pokemon almacenados en el PC
    /// </summary>
    public List<StoredPokemon> PCStorage { get; set; } = new();

    /// <summary>
    /// Registro de encuentros por ruta (para regla de 1 captura por ruta)
    /// </summary>
    public Dictionary<string, EncounterRecord> Encounters { get; set; } = new();

    /// <summary>
    /// Inventario del jugador
    /// </summary>
    public List<InventoryItem> Inventory { get; set; } = new();

    /// <summary>
    /// Historial de batallas completadas (para futuro ML)
    /// </summary>
    public List<BattleRecord> BattleHistory { get; set; } = new();

    /// <summary>
    /// Generación del juego (1 por defecto, extensible)
    /// </summary>
    public int Generation { get; set; } = 1;

    /// <summary>
    /// Variante de Nuzlocke (standard, hardcore, etc.)
    /// </summary>
    public string LockeType { get; set; } = "standard";

    /// <summary>
    /// Personalidad del agente para esta partida. Default: Technical.
    /// </summary>
    public AgentPersonality Personality { get; set; } = AgentPersonality.Technical;

    /// <summary>
    /// Fecha de inicio de la partida
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Última actualización del estado
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Pokemon en el equipo activo
/// </summary>
public class TeamMember
{
    /// <summary>
    /// Apodo del Pokemon (personalización Nuzlocke)
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la especie (ej: "pikachu")
    /// </summary>
    public string Species { get; set; } = string.Empty;

    /// <summary>
    /// Nivel actual
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// PS actuales
    /// </summary>
    public int CurrentHP { get; set; }

    /// <summary>
    /// PS máximos
    /// </summary>
    public int MaxHP { get; set; }

    /// <summary>
    /// Tipos del pokemon (ej: ["fire", "flying"])
    /// </summary>
    public List<string> Types { get; set; } = new();

    /// <summary>
    /// Movimientos actuales
    /// </summary>
    public List<string> Moves { get; set; } = new();

    /// <summary>
    /// Ubicación donde fue capturado
    /// </summary>
    public string CaughtAt { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de captura
    /// </summary>
    public DateTime CaughtDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Determinant Values (DVs/IVs) por stat: [HP, Atk, Def, Spe, Sp]. Rango 0-15. Default 8 (media).
    /// </summary>
    public int[] DVs { get; set; } = new[] { 8, 8, 8, 8, 8 };

    /// <summary>
    /// Stat Experience (EVs) por stat: [HP, Atk, Def, Spe, Sp]. Rango 0-65535. Default 0.
    /// </summary>
    public int[] StatExp { get; set; } = new[] { 0, 0, 0, 0, 0 };

    /// <summary>
    /// Stats calculados según fórmula Gen 1.
    /// </summary>
    public PokemonStats Stats { get; set; } = new();
}

/// <summary>
/// Pokemon que murió durante la partida
/// </summary>
public class DeadPokemon
{
    /// <summary>
    /// Apodo del Pokemon
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// Especie
    /// </summary>
    public string Species { get; set; } = string.Empty;

    /// <summary>
    /// Nivel al morir
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Ubicación donde murió
    /// </summary>
    public string DeathLocation { get; set; } = string.Empty;

    /// <summary>
    /// Causa de la muerte
    /// </summary>
    public string CauseOfDeath { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de muerte
    /// </summary>
    public DateTime DeathDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ubicación donde fue capturado originalmente
    /// </summary>
    public string CaughtAt { get; set; } = string.Empty;
}

/// <summary>
/// Pokemon almacenado en el PC
/// </summary>
public class StoredPokemon
{
    /// <summary>
    /// Apodo
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// Especie
    /// </summary>
    public string Species { get; set; } = string.Empty;

    /// <summary>
    /// Nivel
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Tipos del pokemon (ej: ["water"])
    /// </summary>
    public List<string> Types { get; set; } = new();

    /// <summary>
    /// Movimientos
    /// </summary>
    public List<string> Moves { get; set; } = new();

    /// <summary>
    /// Ubicación de captura
    /// </summary>
    public string CaughtAt { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de captura
    /// </summary>
    public DateTime CaughtDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Determinant Values (DVs/IVs) por stat: [HP, Atk, Def, Spe, Sp]. Rango 0-15. Default 8 (media).
    /// </summary>
    public int[] DVs { get; set; } = new[] { 8, 8, 8, 8, 8 };

    /// <summary>
    /// Stat Experience (EVs) por stat: [HP, Atk, Def, Spe, Sp]. Rango 0-65535. Default 0.
    /// </summary>
    public int[] StatExp { get; set; } = new[] { 0, 0, 0, 0, 0 };

    /// <summary>
    /// Stats calculados según fórmula Gen 1.
    /// </summary>
    public PokemonStats Stats { get; set; } = new();
}

/// <summary>
/// Registro de encuentro en una ruta
/// </summary>
public class EncounterRecord
{
    /// <summary>
    /// Nombre de la ruta/ubicación
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Pokemon capturado en esta ubicación (null si no se capturó ninguno)
    /// </summary>
    public string? CapturedSpecies { get; set; }

    /// <summary>
    /// Apodo del Pokemon capturado
    /// </summary>
    public string? CapturedNickname { get; set; }

    /// <summary>
    /// Indica si ya se usó el encuentro de esta ruta
    /// </summary>
    public bool EncounterUsed { get; set; }

    /// <summary>
    /// Fecha del encuentro
    /// </summary>
    public DateTime EncounterDate { get; set; } = DateTime.UtcNow;
}
