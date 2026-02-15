using es.vargontoc.nuzlocke.ai.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class RetryHelperTests
{
    private readonly Mock<ILogger> _mockLogger = new();

    [Fact]
    public async Task ExecuteWithRetriesAsync_SucceedsOnFirstAttempt()
    {
        var callCount = 0;
        var result = await RetryHelper.ExecuteWithRetriesAsync(
            () => { callCount++; return Task.FromResult(42); },
            maxRetries: 3,
            _mockLogger.Object);

        Assert.Equal(42, result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetriesAsync_RetriesOnTransientError()
    {
        var callCount = 0;
        var result = await RetryHelper.ExecuteWithRetriesAsync(
            () =>
            {
                callCount++;
                if (callCount == 1) throw new HttpRequestException("connection refused");
                return Task.FromResult("ok");
            },
            maxRetries: 3,
            _mockLogger.Object);

        Assert.Equal("ok", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetriesAsync_ThrowsAfterMaxRetries()
    {
        var callCount = 0;
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            RetryHelper.ExecuteWithRetriesAsync<string>(
                () =>
                {
                    callCount++;
                    throw new HttpRequestException("always fails");
                },
                maxRetries: 2,
                _mockLogger.Object));

        Assert.Equal("always fails", ex.Message);
        Assert.Equal(3, callCount); // 1 initial + 2 retries
    }

    [Fact]
    public async Task ExecuteWithRetriesAsync_DoesNotRetryNonTransient()
    {
        var callCount = 0;
        await Assert.ThrowsAsync<ArgumentException>(() =>
            RetryHelper.ExecuteWithRetriesAsync<string>(
                () =>
                {
                    callCount++;
                    throw new ArgumentException("bad arg");
                },
                maxRetries: 3,
                _mockLogger.Object));

        Assert.Equal(1, callCount); // No retries for non-transient
    }

    [Fact]
    public async Task ExecuteWithRetriesAsync_RespectsCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            RetryHelper.ExecuteWithRetriesAsync(
                async () =>
                {
                    await Task.Delay(1000, cts.Token);
                    return "never";
                },
                maxRetries: 3,
                _mockLogger.Object,
                cts.Token));
    }

    [Fact]
    public void IsTransient_HttpRequestException_ReturnsTrue()
    {
        Assert.True(RetryHelper.IsTransient(new HttpRequestException("timeout")));
    }

    [Fact]
    public void IsTransient_TimeoutTaskCanceled_ReturnsTrue()
    {
        var ex = new TaskCanceledException("timeout", new TimeoutException());
        Assert.True(RetryHelper.IsTransient(ex));
    }

    [Fact]
    public void IsTransient_ArgumentException_ReturnsFalse()
    {
        Assert.False(RetryHelper.IsTransient(new ArgumentException("bad")));
    }
}
