using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class StreamingTests
{
    [Fact]
    public async Task StreamAdvice_YieldsChunks()
    {
        var fakeProvider = new FakeStreamingProvider(new[] { "hello", " ", "world" });
        var stateManager = new InMemoryStateManager();
        var pokeConnector = new FakePokeApiConnector();
        var toolExecutor = new es.vargontoc.nuzlocke.ai.Services.ToolExecutor(stateManager, pokeConnector, NullLogger<es.vargontoc.nuzlocke.ai.Services.ToolExecutor>.Instance);
        var agent = new NuzlockeAgent(fakeProvider, stateManager, toolExecutor, NullLogger<NuzlockeAgent>.Instance);

        var results = new List<string>();
        await foreach (var chunk in agent.StreamAdviceAsync("What should I do?"))
        {
            results.Add(chunk);
        }

        Assert.Equal(new[] { "hello", " ", "world" }, results);
    }

    [Fact]
    public async Task StreamAdvice_CancellationStops()
    {
        var cts = new CancellationTokenSource();
        var fakeProvider = new FakeStreamingProvider(ProduceInfiniteAsync(cts.Token));
        var stateManager = new InMemoryStateManager();
        var pokeConnector = new FakePokeApiConnector();
        var toolExecutor = new es.vargontoc.nuzlocke.ai.Services.ToolExecutor(stateManager, pokeConnector, NullLogger<es.vargontoc.nuzlocke.ai.Services.ToolExecutor>.Instance);
        var agent = new NuzlockeAgent(fakeProvider, stateManager, toolExecutor, NullLogger<NuzlockeAgent>.Instance);

        var enumerator = agent.StreamAdviceAsync("stream me").GetAsyncEnumerator(cts.Token);

        // read one element then cancel
        Assert.True(await enumerator.MoveNextAsync());
        cts.Cancel();

        // subsequent MoveNext should complete (either false or throw OperationCanceled)
        try
        {
            var hasMore = await enumerator.MoveNextAsync();
            // either stopped or not; ensure we don't hang
            Assert.False(hasMore);
        }
        catch (OperationCanceledException)
        {
            // accepted outcome
        }
    }

    // Helper: produce infinite stream of chunks (delayed)
    private static async IAsyncEnumerable<string> ProduceInfiniteAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var i = 0;
        while (!ct.IsCancellationRequested && i < 1000)
        {
            await Task.Delay(10, ct);
            yield return $"part-{i++}";
        }
    }

    // Fake IAiProvider that yields provided chunks
    private class FakeStreamingProvider : IAiProvider
    {
        private readonly IEnumerable<string>? _chunks;
        private readonly IAsyncEnumerable<string>? _asyncChunks;

        public FakeStreamingProvider(IEnumerable<string> chunks) { _chunks = chunks; }
        public FakeStreamingProvider(IAsyncEnumerable<string> asyncChunks) { _asyncChunks = asyncChunks; }

        public Task<AiResponse> GetCompletionWithToolsAsync(string systemPrompt, string userMessage, IEnumerable<ToolDefinition> tools, List<ToolCallResult>? toolResults = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiResponse { TextResponse = _chunks != null ? string.Join("", _chunks) : "" });
        }

        public Task<string> GetCompletionAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_chunks != null ? string.Join("", _chunks) : "");
        }

        public async IAsyncEnumerable<string> StreamCompletionAsync(string systemPrompt, string userMessage, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_asyncChunks != null)
            {
                await foreach (var c in _asyncChunks.WithCancellation(cancellationToken))
                {
                    yield return c;
                }
            }
            else if (_chunks != null)
            {
                foreach (var c in _chunks)
                {
                    if (cancellationToken.IsCancellationRequested) yield break;
                    await Task.Yield();
                    yield return c;
                }
            }
        }
    }

    // Simple in-memory IStateManager implementation for tests
    private class InMemoryStateManager : IStateManager
    {
        private NuzlockeState _state = new NuzlockeState();

        public Task<bool> AddToTeamAsync(TeamMember pokemon) => Task.FromResult(true);
        public Task<bool> AddToTeamAsync(string sessionId, TeamMember pokemon) => Task.FromResult(true);
        public Task<bool> MarkAsDeadAsync(string nickname, string deathLocation, string causeOfDeath) => Task.FromResult(true);
        public Task<bool> MarkAsDeadAsync(string sessionId, string nickname, string deathLocation, string causeOfDeath) => Task.FromResult(true);
        public Task<bool> MoveToPCAsync(string nickname) => Task.FromResult(true);
        public Task<bool> MoveToPCAsync(string sessionId, string nickname) => Task.FromResult(true);
        public Task<NuzlockeState> GetStateAsync() => Task.FromResult(_state);
        public Task<NuzlockeState> GetStateAsync(string sessionId) => Task.FromResult(_state);
        public Task SaveStateAsync(NuzlockeState state) { _state = state; return Task.CompletedTask; }
        public Task SaveStateAsync(string sessionId, NuzlockeState state) { _state = state; return Task.CompletedTask; }
        public Task<bool> RecordEncounterAsync(string location, string? capturedSpecies = null, string? capturedNickname = null) => Task.FromResult(true);
        public Task<bool> RecordEncounterAsync(string sessionId, string location, string? capturedSpecies = null, string? capturedNickname = null) => Task.FromResult(true);
    }

    private class FakePokeApiConnector : IPokeApiConnector
    {
        public Task<AbilityData?> GetAbilityAsync(string nameOrId) => Task.FromResult<AbilityData?>(null);
        public Task<ItemData?> GetItemAsync(string nameOrId) => Task.FromResult<ItemData?>(null);
        public Task<MoveData?> GetMoveAsync(string nameOrId) => Task.FromResult<MoveData?>(null);
        public Task<PokemonData?> GetPokemonAsync(string nameOrId) => Task.FromResult<PokemonData?>(null);
        public Task<PokemonSubset?> GetPokemonSubsetAsync(string nameOrId, int movesLimit = 6) => Task.FromResult<PokemonSubset?>(null);
        public Task<TypeData?> GetTypeAsync(string nameOrId) => Task.FromResult<TypeData?>(null);
    }
}
