namespace TransactionCompliance.Core.Stores;

public interface ITransactionSeenStore
{
    /// <summary>
    /// Records a transaction as seen. Returns <c>true</c> if the transaction is new (or expired),
    /// <c>false</c> if it was already seen within the past 24 hours (duplicate).
    /// </summary>
    Task<bool> TryRecordAsync(string transactionId, DateTimeOffset seenAt, CancellationToken ct = default);
}
