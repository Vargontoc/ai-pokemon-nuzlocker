using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// Gen 1 stat calculator using the classic Red/Blue/Yellow formula.
///
/// HP:    floor(((Base + DV) × 2 + floor(ceil(sqrt(StatExp)) / 4)) × Level / 100) + Level + 10
/// Other: floor((floor(((Base + DV) × 2 + floor(ceil(sqrt(StatExp)) / 4)) × Level / 100) + 5) × nature)
///
/// Stat index: 0=HP, 1=Attack, 2=Defense, 3=Speed, 4=Special
/// </summary>
public class Gen1StatsCalculator : IStatsCalculator
{
    public PokemonStats Calculate(int[] baseStats, int[] dvs, int[] statExp, int level, float nature = 1.0f)
    {
        return new PokemonStats
        {
            HP      = CalcHP(baseStats[0], dvs[0], statExp[0], level),
            Attack  = CalcStat(baseStats[1], dvs[1], statExp[1], level, nature),
            Defense = CalcStat(baseStats[2], dvs[2], statExp[2], level, nature),
            Speed   = CalcStat(baseStats[3], dvs[3], statExp[3], level, nature),
            Special = CalcStat(baseStats[4], dvs[4], statExp[4], level, nature)
        };
    }

    private static int CalcHP(int baseStat, int dv, int ev, int level)
    {
        var evFactor = EvFactor(ev);
        return (int)Math.Floor(((baseStat + dv) * 2.0 + evFactor) * level / 100.0) + level + 10;
    }

    private static int CalcStat(int baseStat, int dv, int ev, int level, float nature)
    {
        var evFactor = EvFactor(ev);
        var raw = (int)Math.Floor(((baseStat + dv) * 2.0 + evFactor) * level / 100.0) + 5;
        return (int)Math.Floor(raw * nature);
    }

    /// <summary>
    /// EV contribution: floor(ceil(sqrt(StatExp)) / 4)
    /// </summary>
    private static double EvFactor(int statExp)
    {
        var sq = Math.Ceiling(Math.Sqrt(statExp));
        return Math.Floor(sq / 4.0);
    }
}
