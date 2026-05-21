using TransactionCompliance.Core.Models;

namespace TransactionCompliance.Core.Rules;

public sealed class CurrencyRestrictionRule : IScreeningRule
{
    private readonly IReadOnlySet<string> _allowedCurrencies;

    public CurrencyRestrictionRule(IEnumerable<string> allowedCurrencies)
    {
        _allowedCurrencies = new HashSet<string>(
            allowedCurrencies,
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<RuleResult> EvaluateAsync(ScreeningRequest request, CancellationToken ct = default)
    {
        var result = _allowedCurrencies.Contains(request.Currency)
            ? new RuleResult("currency_restriction", RuleStatus.Passed, RuleSeverity.High, "Currency is permitted")
            : new RuleResult("currency_restriction", RuleStatus.Triggered, RuleSeverity.High, "Currency is not permitted");

        return Task.FromResult(result);
    }
}
