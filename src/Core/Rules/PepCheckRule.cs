using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Services;

namespace TransactionCompliance.Core.Rules;

public sealed class PepCheckRule : IScreeningRule
{
    private const long PepAmountThresholdCents = 5_000_000L;

    private readonly IPepService _pepService;

    public PepCheckRule(IPepService pepService)
    {
        _pepService = pepService;
    }

    public async Task<RuleResult> EvaluateAsync(ScreeningRequest request, CancellationToken ct = default)
    {
        var isPep = await _pepService.IsPepAsync(request.Receiver.AccountId, ct);

        var result = isPep && request.Amount > PepAmountThresholdCents
            ? new RuleResult("pep_check", RuleStatus.Triggered, RuleSeverity.Medium, "Receiver is a PEP with high-value transaction")
            : new RuleResult("pep_check", RuleStatus.Passed, RuleSeverity.Medium, "PEP check passed");

        return result;
    }
}
