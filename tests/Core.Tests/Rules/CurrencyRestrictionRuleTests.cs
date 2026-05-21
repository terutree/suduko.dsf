using FluentAssertions;
using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Rules;

namespace TransactionCompliance.Core.Tests.Rules;

public class CurrencyRestrictionRuleTests
{
    private static readonly string[] AllowedCurrencies = ["NOK", "EUR", "USD", "GBP"];

    private static CurrencyRestrictionRule CreateRule() => new(AllowedCurrencies);

    private static ScreeningRequest MakeRequest(string currency) =>
        new("TXN-CURR-001",
            new PartyInfo("ACC-001", "Sender", "NO"),
            new PartyInfo("ACC-002", "Receiver", "NO"),
            1_000_000L,
            currency);

    [Theory]
    [InlineData("NOK")]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public async Task permitted_currency_passes(string currency)
    {
        var rule = CreateRule();
        var result = await rule.EvaluateAsync(MakeRequest(currency));

        result.Status.Should().Be(RuleStatus.Passed);
        result.Rule.Should().Be("currency_restriction");
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("CHF")]
    public async Task non_permitted_currency_triggers(string currency)
    {
        var rule = CreateRule();
        var result = await rule.EvaluateAsync(MakeRequest(currency));

        result.Status.Should().Be(RuleStatus.Triggered);
        result.Severity.Should().Be(RuleSeverity.High);
        result.Message.Should().Be("Currency is not permitted");
        result.Rule.Should().Be("currency_restriction");
    }

    [Theory]
    [InlineData("nok")]
    [InlineData("Eur")]
    public async Task permitted_currency_case_insensitive_passes(string currency)
    {
        var rule = CreateRule();
        var result = await rule.EvaluateAsync(MakeRequest(currency));

        result.Status.Should().Be(RuleStatus.Passed);
        result.Rule.Should().Be("currency_restriction");
    }


}
