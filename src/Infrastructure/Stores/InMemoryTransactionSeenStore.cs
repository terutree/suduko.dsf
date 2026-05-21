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
        var now = _timeProvider.GetUtcNow();

        var isDuplicate = false;

        _store.AddOrUpdate(
            transactionId,
            addValueFactory: _ => seenAt.ToUniversalTime(),
            updateValueFactory: (_, existing) =>
            {
                var age = now - existing;
                if (age < DuplicateWindow)
                {
                    isDuplicate = true;
                    return existing;
                }

                // Expired entry — replace with new timestamp
                return seenAt.ToUniversalTime();
            });

        return Task.FromResult(!isDuplicate);
    }
}
