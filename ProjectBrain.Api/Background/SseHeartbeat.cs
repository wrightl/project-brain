namespace ProjectBrain.Api.Background;

/// <summary>Sends periodic SSE comment frames (<c>: \n\n</c>) to keep intermediaries from treating the connection as idle.</summary>
public sealed class SseHeartbeat : IAsyncDisposable
{
    private readonly HttpResponse _response;
    private readonly SemaphoreSlim _writeLock;
    private readonly TimeSpan _interval;
    private readonly CancellationTokenSource _cts;
    private Task? _loopTask;

    public SseHeartbeat(HttpResponse response, SemaphoreSlim writeLock, TimeSpan interval, CancellationToken requestAborted)
    {
        _response = response;
        _writeLock = writeLock;
        _interval = interval <= TimeSpan.Zero ? TimeSpan.FromSeconds(15) : interval;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
    }

    public void Start() => _loopTask = RunLoopAsync(_cts.Token);

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await _writeLock.WaitAsync(ct);
                try
                {
                    await _response.WriteAsync(": \n\n", ct);
                    await _response.Body.FlushAsync(ct);
                }
                finally
                {
                    _writeLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Client may have disconnected; stop heartbeats for this response.
                break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _cts.CancelAsync();
        }
        catch
        {
            // ignored
        }

        if (_loopTask is not null)
        {
            try
            {
                await _loopTask.ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
        }

        _cts.Dispose();
    }
}
