using TransactionCompliance.Core.Models;

namespace TransactionCompliance.Core.Rules;

public sealed class AmountThresholdRule : IScreeningRule
{
    private const long ThresholdCents = 10_000_000L;

    public Task<RuleResult> EvaluateAsync(ScreeningRequest request, CancellationToken ct = default)
    {
        var result = request.Amount > ThresholdCents
            ? new RuleResult("amount_threshold", RuleStatus.Triggered, RuleSeverity.High, "Amount exceeds 100,000 NOK threshold")
            : new RuleResult("amount_threshold", RuleStatus.Passed, RuleSeverity.High, "Amount within threshold");

        return Task.FromResult(result);
    }
}
