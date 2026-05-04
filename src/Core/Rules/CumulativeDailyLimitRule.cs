using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Stores;

namespace TransactionCompliance.Core.Rules;

public sealed class CumulativeDailyLimitRule : IScreeningRule
{
    private const long DailyLimitCents = 50_000_000L;

    private readonly IDailyAggregateStore _dailyStore;

    public CumulativeDailyLimitRule(IDailyAggregateStore dailyStore)
    {
        _dailyStore = dailyStore;
    }

    public async Task<RuleResult> EvaluateAsync(ScreeningRequest request, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentTotal = await _dailyStore.GetDailyTotalAsync(request.Sender.AccountId, today, ct);

        var result = currentTotal + request.Amount > DailyLimitCents
            ? new RuleResult("cumulative_daily_limit", RuleStatus.Triggered, RuleSeverity.High, "Cumulative daily limit exceeded")
            : new RuleResult("cumulative_daily_limit", RuleStatus.Passed, RuleSeverity.High, "Within daily limit");

        return result;
    }
}
