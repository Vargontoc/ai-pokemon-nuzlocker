using System.IO;
using System.Net.Sockets;
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
            catch (Exception ex) when (!ct.IsCancellationRequested && IsTransient(ex) && attempt < maxRetries)
            {
                attempt++;
                var delayMs = (int)(Math.Pow(2, attempt) * 100) + rnd.Next(0, 100);
                logger.LogWarning(ex, "Transient error; retry {Attempt}/{Max} after {Delay}ms",
                    attempt, maxRetries, delayMs);
                try
                {
                    await Task.Delay(delayMs, ct);
                }
                catch (OperationCanceledException)
                {
                    // Request was canceled during retry delay — propagate the original error
                    logger.LogWarning("Retry canceled during delay — aborting retries");
                    throw ex;
                }
            }
        }
    }

    public static bool IsTransient(Exception ex)
    {
        if (ex is HttpRequestException) return true;
        if (ex is IOException) return true;
        if (ex is SocketException) return true;
        if (ex is TimeoutException) return true;
        // TaskCanceledException wrapping IO/timeout errors (not user cancellation)
        if (ex is TaskCanceledException tce && tce.InnerException != null)
            return IsTransient(tce.InnerException);
        return false;
    }
}
