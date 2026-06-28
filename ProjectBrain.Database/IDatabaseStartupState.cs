namespace ProjectBrain.Database;

public interface IDatabaseStartupState
{
    bool IsWarmedUp { get; }

    bool AreMigrationsApplied { get; }

    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);

    void MarkReady();

    void MarkMigrationsApplied();
}
