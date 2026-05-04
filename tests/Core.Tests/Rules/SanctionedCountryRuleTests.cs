using FluentAssertions;
using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Rules;

namespace TransactionCompliance.Core.Tests.Rules;

public class SanctionedCountryRuleTests
{
    private static readonly string[] Sanctioned = ["KP", "IR", "SY", "CU", "RU"];

    private static SanctionedCountryRule CreateRule() => new(Sanctioned);

    private static ScreeningRequest MakeRequest(string receiverCountry) =>
        new("TXN-001",
            new PartyInfo("ACC-001", "Sender", "NO"),
            new PartyInfo("ACC-002", "Receiver", receiverCountry),
            1_000_000L,
            "NOK");

    [Theory]
    [InlineData("KP")]
    [InlineData("IR")]
    [InlineData("SY")]
    [InlineData("CU")]
    [InlineData("RU")]
    public async Task sanctioned_country_triggers(string country)
    {
        var rule = CreateRule();
        var result = await rule.EvaluateAsync(MakeRequest(country));

        result.Status.Should().Be(RuleStatus.Triggered);
        result.Rule.Should().Be("sanctioned_country");
    }

    [Theory]
    [InlineData("NO")]
    [InlineData("SE")]
    public async Task non_sanctioned_country_passes(string country)
    {
        var rule = CreateRule();
        var result = await rule.EvaluateAsync(MakeRequest(country));

        result.Status.Should().Be(RuleStatus.Passed);
    }

    [Fact]
    public async Task lowercase_ru_triggers()
    {
        var rule = CreateRule();
        var result = await rule.EvaluateAsync(MakeRequest("ru"));

        result.Status.Should().Be(RuleStatus.Triggered);
    }
}
