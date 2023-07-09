using System.Collections.Concurrent;

namespace BuildingBlocks.EventStoreDB.Subscriptions;

public class InMemorySubscriptionCheckpointRepository : ISubscriptionCheckpointRepository
{
    private readonly ConcurrentDictionary<string, ulong> _checkpoints = new();

    public ValueTask<ulong?> Load(string subscriptionId, CancellationToken ct)
    {
        return new ValueTask<ulong?>(_checkpoints.TryGetValue(subscriptionId, out ulong checkpoint) ? checkpoint : null);
    }

    public ValueTask Store(string subscriptionId, ulong position, CancellationToken ct)
    {
        _checkpoints.AddOrUpdate(subscriptionId, position, (_, _) => position);

        return ValueTask.CompletedTask;
    }
}
