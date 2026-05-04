using System.Collections.Concurrent;
using TransactionCompliance.Core.Stores;

namespace TransactionCompliance.Infrastructure.Stores;

public sealed class InMemoryDailyAggregateStore : IDailyAggregateStore
{
    private readonly ConcurrentDictionary<string, long> _totals = new();

    private static string Key(string accountId, DateOnly date) =>
        $"{accountId}:{date:yyyy-MM-dd}";

    public Task<long> GetDailyTotalAsync(string accountId, DateOnly date, CancellationToken ct = default)
    {
        var total = _totals.GetValueOrDefault(Key(accountId, date), 0L);
        return Task.FromResult(total);
    }

    public Task AddAsync(string accountId, DateOnly date, long amount, CancellationToken ct = default)
    {
        _totals.AddOrUpdate(
            Key(accountId, date),
            amount,
            (_, existing) => existing + amount);
        return Task.CompletedTask;
    }
}
