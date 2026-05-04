using FluentAssertions;
using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Rules;

namespace TransactionCompliance.Core.Tests.Rules;

public class AmountThresholdRuleTests
{
    private static readonly AmountThresholdRule Rule = new();

    private static ScreeningRequest MakeRequest(long amount) =>
        new("TXN-001",
            new PartyInfo("ACC-001", "Sender", "NO"),
            new PartyInfo("ACC-002", "Receiver", "NO"),
            amount,
            "NOK");

    [Fact]
    public async Task amount_10000001_triggers()
    {
        var result = await Rule.EvaluateAsync(MakeRequest(10_000_001L));

        result.Status.Should().Be(RuleStatus.Triggered);
        result.Rule.Should().Be("amount_threshold");
    }

    [Fact]
    public async Task amount_10000000_passes()
    {
        var result = await Rule.EvaluateAsync(MakeRequest(10_000_000L));

        result.Status.Should().Be(RuleStatus.Passed);
    }

    [Fact]
    public async Task amount_below_threshold_passes()
    {
        var result = await Rule.EvaluateAsync(MakeRequest(5_000_000L));

        result.Status.Should().Be(RuleStatus.Passed);
    }
}
