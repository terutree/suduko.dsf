using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Stores;

namespace TransactionCompliance.Core.Rules;

public sealed class DuplicateTransactionRule : IScreeningRule
{
    private readonly ITransactionSeenStore _seenStore;
    private readonly TimeProvider _timeProvider;

    public DuplicateTransactionRule(ITransactionSeenStore seenStore, TimeProvider timeProvider)
    {
        _seenStore = seenStore;
        _timeProvider = timeProvider;
    }

    public async Task<RuleResult> EvaluateAsync(ScreeningRequest request, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        var isNew = await _seenStore.TryRecordAsync(request.TransactionId, now, ct);

        return isNew
            ? new RuleResult("duplicate_transaction", RuleStatus.Passed, RuleSeverity.High, "Transaction ID is unique")
            : new RuleResult("duplicate_transaction", RuleStatus.Triggered, RuleSeverity.High, "Transaction has already been screened within the past 24 hours");
    }
}
