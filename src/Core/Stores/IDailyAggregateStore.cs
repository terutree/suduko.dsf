namespace TransactionCompliance.Core.Stores;

public interface IDailyAggregateStore
{
    Task<long> GetDailyTotalAsync(string accountId, DateOnly date, CancellationToken ct = default);
    Task AddAsync(string accountId, DateOnly date, long amount, CancellationToken ct = default);
}
