using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Models;
using Microsoft.Extensions.Options;

namespace es.vargontoc.nuzlocke.ai.Services;

public class PokeApiConnector : IPokeApiConnector
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PokeApiConnector> _logger;
    private readonly string _baseUrl;

    public PokeApiConnector(
        HttpClient httpClient,
        ILogger<PokeApiConnector> logger,
        IOptions<PokeApiOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = options.Value.BaseUrl;

        // Configure timeout from options
        _httpClient.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
    }

    public async Task<PokemonData?> GetPokemonAsync(string nameOrId)
    {
        return await GetResourceAsync<PokemonData>($"{_baseUrl}/pokemon/{nameOrId.ToLower()}");
    }

    public async Task<MoveData?> GetMoveAsync(string nameOrId)
    {
        return await GetResourceAsync<MoveData>($"{_baseUrl}/move/{nameOrId.ToLower()}");
    }

    public async Task<TypeData?> GetTypeAsync(string nameOrId)
    {
        return await GetResourceAsync<TypeData>($"{_baseUrl}/type/{nameOrId.ToLower()}");
    }

    public async Task<AbilityData?> GetAbilityAsync(string nameOrId)
    {
        return await GetResourceAsync<AbilityData>($"{_baseUrl}/ability/{nameOrId.ToLower()}");
    }

    private async Task<T?> GetResourceAsync<T>(string endpoint) where T : class
    {
        try
        {
            _logger.LogInformation("Fetching resource from PokeApi: {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PokeApi returned status code {StatusCode} for {Endpoint}",
                    response.StatusCode, endpoint);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<T>(content, options);
            _logger.LogInformation("Successfully fetched and deserialized {Type} from {Endpoint}",
                typeof(T).Name, endpoint);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching {Endpoint}", endpoint);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for {Endpoint}", endpoint);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching {Endpoint}", endpoint);
            return null;
        }
    }
}
