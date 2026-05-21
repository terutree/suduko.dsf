using System.Collections.Concurrent;
using TransactionCompliance.Core.Stores;

namespace TransactionCompliance.Infrastructure.Stores;

public sealed class InMemoryTransactionSeenStore : ITransactionSeenStore
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, DateTimeOffset> _store = new();
    private readonly TimeProvider _timeProvider;

    public InMemoryTransactionSeenStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<bool> TryRecordAsync(string transactionId, DateTimeOffset seenAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var now = _timeProvider.GetUtcNow();
        var seenAtUtc = seenAt.ToUniversalTime();

        if (_store.TryAdd(transactionId, seenAtUtc))
            return Task.FromResult(true); // new entry

        // Key exists — check if the existing entry is within the duplicate window
        var existing = _store[transactionId];
        var age = now - existing;
        if (age < DuplicateWindow)
            return Task.FromResult(false); // duplicate

        // Expired — atomically remove the specific expired entry, then add the fresh one.
        // TryRemove(KeyValuePair) only removes if both key AND value match, preventing
        // a race where two threads both see the expired entry and both return true.
        if (_store.TryRemove(new KeyValuePair<string, DateTimeOffset>(transactionId, existing)))
        {
            _store.TryAdd(transactionId, seenAtUtc);
            return Task.FromResult(true);
        }
        // Lost the race — another thread already refreshed the entry; treat as duplicate (conservative, safe).
        return Task.FromResult(false);
    }
}
