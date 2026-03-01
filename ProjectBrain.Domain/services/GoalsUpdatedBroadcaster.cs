using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ProjectBrain.Domain;

/// <summary>
/// In-memory per-user broadcaster for goals-updated events (e.g. SSE). Single-instance only;
/// for multi-instance scale-out, a shared pub/sub (e.g. Redis) can be used instead.
/// </summary>
public sealed class GoalsUpdatedBroadcaster : IGoalsUpdatedBroadcaster
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, ChannelWriter<GoalsUpdatedEvent>>> _subscribers = new();

    public async Task SubscribeAsync(
        string userId,
        Func<GoalsUpdatedEvent, CancellationToken, Task> writeEvent,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<GoalsUpdatedEvent>(new UnboundedChannelOptions { SingleReader = true });
        var id = Guid.NewGuid();
        var userSubs = _subscribers.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, ChannelWriter<GoalsUpdatedEvent>>());
        userSubs[id] = channel.Writer;

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await writeEvent(evt, cancellationToken);
            }
        }
        finally
        {
            userSubs.TryRemove(id, out _);
        }
    }

    public void NotifyGoalsUpdated(string userId, GoalsUpdatedEvent evt)
    {
        if (!_subscribers.TryGetValue(userId, out var userSubs))
            return;

        foreach (var writer in userSubs.Values)
        {
            writer.TryWrite(evt);
        }
    }
}
