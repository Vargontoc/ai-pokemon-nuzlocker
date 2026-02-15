using System.Text;
using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Models;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class TokenMetricsTests
{
    /// <summary>
    /// Creates a realistic PokemonData fixture similar to what PokeAPI returns for Pikachu.
    /// </summary>
    private static PokemonData CreatePikachuFixture()
    {
        var pokemon = new PokemonData
        {
            Id = 25,
            Name = "pikachu",
            Height = 4,
            Weight = 60,
            BaseExperience = 112,
            Types = new List<PokemonTypeSlot>
            {
                new() { Slot = 1, Type = new NamedApiResource { Name = "electric", Url = "https://pokeapi.co/api/v2/type/13/" } }
            },
            Abilities = new List<PokemonAbilitySlot>
            {
                new() { Slot = 1, IsHidden = false, Ability = new NamedApiResource { Name = "static", Url = "https://pokeapi.co/api/v2/ability/9/" } },
                new() { Slot = 3, IsHidden = true, Ability = new NamedApiResource { Name = "lightning-rod", Url = "https://pokeapi.co/api/v2/ability/31/" } }
            },
            Stats = new List<PokemonStatSlot>
            {
                new() { BaseStat = 35, Effort = 0, Stat = new NamedApiResource { Name = "hp", Url = "https://pokeapi.co/api/v2/stat/1/" } },
                new() { BaseStat = 55, Effort = 0, Stat = new NamedApiResource { Name = "attack", Url = "https://pokeapi.co/api/v2/stat/2/" } },
                new() { BaseStat = 40, Effort = 0, Stat = new NamedApiResource { Name = "defense", Url = "https://pokeapi.co/api/v2/stat/3/" } },
                new() { BaseStat = 50, Effort = 0, Stat = new NamedApiResource { Name = "special-attack", Url = "https://pokeapi.co/api/v2/stat/4/" } },
                new() { BaseStat = 50, Effort = 0, Stat = new NamedApiResource { Name = "special-defense", Url = "https://pokeapi.co/api/v2/stat/5/" } },
                new() { BaseStat = 90, Effort = 2, Stat = new NamedApiResource { Name = "speed", Url = "https://pokeapi.co/api/v2/stat/6/" } }
            }
        };

        // Add many moves (Pikachu learns ~80+ moves) to simulate real payload
        var moveNames = new[]
        {
            "mega-punch", "pay-day", "thunder-punch", "slam", "mega-kick",
            "headbutt", "body-slam", "take-down", "double-edge", "tail-whip",
            "growl", "surf", "thunderbolt", "thunder-wave", "thunder",
            "dig", "toxic", "agility", "quick-attack", "rage",
            "mimic", "double-team", "light-screen", "reflect", "bide",
            "swift", "skull-bash", "flash", "rest", "substitute",
            "thief", "snore", "curse", "protect", "sweet-kiss",
            "mud-slap", "detect", "endure", "charm", "rollout",
            "swagger", "spark", "attract", "sleep-talk", "return",
            "frustration", "dynamic-punch", "encore", "iron-tail", "hidden-power",
            "rain-dance", "sunny-day", "fake-out", "uproar", "knock-off",
            "wish", "helping-hand", "trick", "volt-tackle", "secret-power",
            "signal-beam", "feint", "discharge", "nasty-plot", "electro-ball",
            "round", "echoed-voice", "volt-switch", "wild-charge", "play-nice",
            "nuzzle", "draining-kiss", "grass-knot", "electroweb", "rising-voltage"
        };

        pokemon.Moves = moveNames.Select(name => new PokemonMoveSlot
        {
            Move = new NamedApiResource { Name = name, Url = $"https://pokeapi.co/api/v2/move/{name}/" }
        }).ToList();

        return pokemon;
    }

    private static PokemonSubset BuildSubset(PokemonData full, int movesLimit = 6)
    {
        var subset = new PokemonSubset
        {
            Id = full.Id,
            Name = full.Name,
            Types = full.Types.Select(t => t.Type.Name).ToList(),
            MovesBasicos = full.Moves.Take(movesLimit).Select(m => m.Move.Name).ToList()
        };
        foreach (var s in full.Stats)
        {
            subset.Stats[s.Stat.Name] = s.BaseStat;
        }
        subset.SpriteMin = null;
        return subset;
    }

    [Fact]
    public void PokemonSubset_IsAtLeast40PercentSmaller()
    {
        var full = CreatePikachuFixture();
        var subset = BuildSubset(full);

        var fullBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(full));
        var subsetBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(subset));
        var reductionPct = (1.0 - (double)subsetBytes / fullBytes) * 100;

        Assert.True(reductionPct >= 40.0,
            $"Expected >= 40% reduction but got {reductionPct:F1}% (full={fullBytes}B, subset={subsetBytes}B)");
    }

    [Fact]
    public void PokemonSubset_ContainsEssentialFields()
    {
        var full = CreatePikachuFixture();
        var subset = BuildSubset(full);

        Assert.Equal(25, subset.Id);
        Assert.Equal("pikachu", subset.Name);
        Assert.Contains("electric", subset.Types);
        Assert.True(subset.Stats.ContainsKey("hp"));
        Assert.True(subset.Stats.ContainsKey("attack"));
        Assert.True(subset.Stats.ContainsKey("speed"));
        Assert.Equal(35, subset.Stats["hp"]);
        Assert.Equal(90, subset.Stats["speed"]);
        Assert.NotEmpty(subset.MovesBasicos);
    }

    [Fact]
    public void PokemonSubset_LimitsMovesCorrectly()
    {
        var full = CreatePikachuFixture();

        var subset3 = BuildSubset(full, movesLimit: 3);
        Assert.Equal(3, subset3.MovesBasicos.Count);

        var subset6 = BuildSubset(full, movesLimit: 6);
        Assert.Equal(6, subset6.MovesBasicos.Count);

        var subset1 = BuildSubset(full, movesLimit: 1);
        Assert.Single(subset1.MovesBasicos);
        Assert.Equal("mega-punch", subset1.MovesBasicos[0]);
    }
}
