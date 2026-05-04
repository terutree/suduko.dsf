namespace TransactionCompliance.Core.Services;

public interface IPepService
{
    Task<bool> IsPepAsync(string accountId, CancellationToken ct = default);
}
