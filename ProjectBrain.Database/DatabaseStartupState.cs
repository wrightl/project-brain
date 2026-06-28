namespace ProjectBrain.Database;

public sealed class DatabaseStartupState : IDatabaseStartupState
{
    private readonly TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _migrationsAppliedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsWarmedUp => _readyTcs.Task.IsCompletedSuccessfully;

    public bool AreMigrationsApplied => _migrationsAppliedTcs.Task.IsCompletedSuccessfully;

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.CanBeCanceled)
        {
            return _readyTcs.Task.WaitAsync(cancellationToken);
        }

        return _readyTcs.Task;
    }

    public void MarkReady() => _readyTcs.TrySetResult();

    public void MarkMigrationsApplied() => _migrationsAppliedTcs.TrySetResult();
}
