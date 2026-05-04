using TransactionCompliance.Core.Models;

namespace TransactionCompliance.Core.Stores;

public interface IScreeningResultStore
{
    Task SaveAsync(ScreeningResponse response, CancellationToken ct = default);
    Task<ScreeningResponse?> GetAsync(string requestId, CancellationToken ct = default);
}
