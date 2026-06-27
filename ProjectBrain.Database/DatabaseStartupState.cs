namespace ProjectBrain.Database;

public sealed class DatabaseStartupState : IDatabaseStartupState
{
    private readonly TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsWarmedUp => _readyTcs.Task.IsCompletedSuccessfully;

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.CanBeCanceled)
        {
            return _readyTcs.Task.WaitAsync(cancellationToken);
        }

        return _readyTcs.Task;
    }

    public void MarkReady() => _readyTcs.TrySetResult();
}
