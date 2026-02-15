using Microsoft.Extensions.Logging;

namespace es.vargontoc.nuzlocke.ai.Providers;

/// <summary>
/// Shared retry helper with exponential backoff + jitter for transient errors.
/// </summary>
public static class RetryHelper
{
    public static async Task<T> ExecuteWithRetriesAsync<T>(
        Func<Task<T>> action,
        int maxRetries,
        ILogger logger,
        CancellationToken ct = default)
    {
        var rnd = new Random();
        var attempt = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxRetries)
            {
                attempt++;
                var delayMs = (int)(Math.Pow(2, attempt) * 100) + rnd.Next(0, 100);
                logger.LogWarning(ex, "Transient error; retry {Attempt}/{Max} after {Delay}ms",
                    attempt, maxRetries, delayMs);
                await Task.Delay(delayMs, ct);
            }
        }
    }

    public static bool IsTransient(Exception ex)
        => ex is HttpRequestException
        || ex is TaskCanceledException { InnerException: TimeoutException };
}
