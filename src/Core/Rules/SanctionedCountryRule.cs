using TransactionCompliance.Core.Models;

namespace TransactionCompliance.Core.Rules;

public sealed class SanctionedCountryRule : IScreeningRule
{
    private readonly IReadOnlySet<string> _sanctionedCountries;

    public SanctionedCountryRule(IEnumerable<string> sanctionedCountries)
    {
        _sanctionedCountries = new HashSet<string>(
            sanctionedCountries,
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<RuleResult> EvaluateAsync(ScreeningRequest request, CancellationToken ct = default)
    {
        var result = _sanctionedCountries.Contains(request.Receiver.Country)
            ? new RuleResult("sanctioned_country", RuleStatus.Triggered, RuleSeverity.High, "Receiver country is sanctioned")
            : new RuleResult("sanctioned_country", RuleStatus.Passed, RuleSeverity.High, "Receiver country is not sanctioned");

        return Task.FromResult(result);
    }
}
