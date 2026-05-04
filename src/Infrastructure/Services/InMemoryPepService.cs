using TransactionCompliance.Core.Services;

namespace TransactionCompliance.Infrastructure.Services;

public sealed class InMemoryPepService : IPepService
{
    private static readonly HashSet<string> PepAccounts = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACC-PEP-001"
    };

    public Task<bool> IsPepAsync(string accountId, CancellationToken ct = default) =>
        Task.FromResult(PepAccounts.Contains(accountId));
}
