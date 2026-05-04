using TransactionCompliance.Core.Models;

namespace TransactionCompliance.Core.Rules;

public interface IScreeningRule
{
    Task<RuleResult> EvaluateAsync(ScreeningRequest request, CancellationToken ct = default);
}
