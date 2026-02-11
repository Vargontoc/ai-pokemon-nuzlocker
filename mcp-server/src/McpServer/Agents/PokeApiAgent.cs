
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Plugins;
using Microsoft.SemanticKernel;
using es.vargontoc.nuzlocke.ai.Connectors;

namespace es.vargontoc.nuzlocke.ai.Agents
{
    public class PokeApiAgent
    {
        private static readonly string SYSTEM_PROMPT =
        """
        Eres un asistente experto en Pokemon. Ayudas a proporcionar la información más actualizada posible enfocado principalmente en el competitivo.
        La información que proporcionas va a ser consumida por otro agente. Por tanto dala de la forma mas esquematica posible.

        Tienes acceso a herramientas para consultar información detallada:
        - Pokemon Stats, tipos, habilidades, movimientos (get_pokemon)
        - Potencia de movimientos, precisión, tipo, efectos y metadatos (get_move)
        - Tabla de efectividades (get_type)
        - Descripcion de habilidades y sus efectos (get_ability)
        - Información de los distintos items y sus efectos (get_item)

        Usa estas herramientas para dar información precisa. Va ser consumida por otro agente
        """;

        private readonly Kernel _kernel;
        private readonly IPokeApiConnector _connector;
        private readonly ILogger<PokeApiAgent> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public PokeApiAgent(Kernel kernel, IPokeApiConnector connector, ILogger<PokeApiAgent> logger, ILoggerFactory loggerFactory)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _connector = connector ?? throw new ArgumentNullException(nameof(connector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            _logger.LogInformation("PokeApiAgent constructed");

            // Register PokeApi plugin for kernel (scoped registration is fine here)
            if (_kernel.Plugins.FirstOrDefault(x => x.Name == "PokeAPI") == null)
            {
                _logger.LogInformation("Registering PokeAPI plugin in kernel");
                var plugin = new PokeApiPlugin(_connector, _loggerFactory.CreateLogger<PokeApiPlugin>());
                _kernel.Plugins.AddFromObject(plugin, "PokeAPI");
            }
        }

        public async Task<string> GetResponse(string question, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                _logger.LogWarning("GetResponse called with empty question");
                return string.Empty;
            }

            _logger.LogInformation("GetResponse asked: {question}", question);

            var fullPrompt = $"{SYSTEM_PROMPT}\n\nUSER QUESTION: {question}";

            var executionSettings = new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            try
            {
                _logger.LogDebug("Invoking kernel with function-calling enabled (timeout 30s)");
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            

                var result = await _kernel.InvokePromptAsync(fullPrompt, new KernelArguments(executionSettings), cancellationToken: linkedCts.Token);
                var text = result?.ToString() ?? string.Empty;
                _logger.LogInformation("Kernel returned result length {len}", text.Length);
                _logger.LogDebug("Kernel result: {result}", text);
                return text;
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Kernel invocation canceled by caller");
                throw;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Kernel invocation timed out after 30s");
                return string.Empty; // fallback: return empty so calling agent can continue
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking kernel in GetResponse");
                throw;
            }
        }
    }
}