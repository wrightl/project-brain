namespace ProjectBrain.Database;

public interface IDatabaseStartupState
{
    bool IsWarmedUp { get; }

    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);

    void MarkReady();
}
