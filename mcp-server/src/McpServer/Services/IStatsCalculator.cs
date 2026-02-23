using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// Calculates Pokemon stats from base stats, DVs, StatExp and level.
/// Stat index convention: 0=HP, 1=Attack, 2=Defense, 3=Speed, 4=Special
/// </summary>
public interface IStatsCalculator
{
    /// <summary>
    /// Calculates all 5 stats for a Gen 1 Pokemon.
    /// </summary>
    /// <param name="baseStats">Base stats array [HP, Atk, Def, Spe, Sp]</param>
    /// <param name="dvs">Determinant Values 0-15 per stat [HP, Atk, Def, Spe, Sp]</param>
    /// <param name="statExp">Stat Experience 0-65535 per stat [HP, Atk, Def, Spe, Sp]</param>
    /// <param name="level">Pokemon level 1-100</param>
    /// <param name="nature">Nature multiplier (1.0f for Gen 1; applies only to non-HP stats)</param>
    PokemonStats Calculate(int[] baseStats, int[] dvs, int[] statExp, int level, float nature = 1.0f);
}
