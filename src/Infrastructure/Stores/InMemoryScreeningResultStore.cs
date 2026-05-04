using System.Collections.Concurrent;
using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Stores;

namespace TransactionCompliance.Infrastructure.Stores;

public sealed class InMemoryScreeningResultStore : IScreeningResultStore
{
    private readonly ConcurrentDictionary<string, ScreeningResponse> _store = new();

    public Task SaveAsync(ScreeningResponse response, CancellationToken ct = default)
    {
        _store[response.RequestId] = response;
        return Task.CompletedTask;
    }

    public Task<ScreeningResponse?> GetAsync(string requestId, CancellationToken ct = default)
    {
        _store.TryGetValue(requestId, out var response);
        return Task.FromResult(response);
    }
}
