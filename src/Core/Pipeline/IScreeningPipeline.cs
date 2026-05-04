using TransactionCompliance.Core.Models;

namespace TransactionCompliance.Core.Pipeline;

public interface IScreeningPipeline
{
    Task<ScreeningResponse> ScreenAsync(string requestId, ScreeningRequest request, CancellationToken ct = default);
}
