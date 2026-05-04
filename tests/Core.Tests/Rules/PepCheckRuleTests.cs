using FluentAssertions;
using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Rules;
using TransactionCompliance.Core.Services;

namespace TransactionCompliance.Core.Tests.Rules;

public class PepCheckRuleTests
{
    private sealed class FakePepService : IPepService
    {
        private readonly bool _isPep;

        public FakePepService(bool isPep)
        {
            _isPep = isPep;
        }

        public Task<bool> IsPepAsync(string accountId, CancellationToken ct = default) =>
            Task.FromResult(_isPep);
    }

    private static ScreeningRequest MakeRequest(long amount, string receiverAccountId = "ACC-001") =>
        new("TXN-001",
            new PartyInfo("ACC-SENDER", "Sender", "NO"),
            new PartyInfo(receiverAccountId, "Receiver", "NO"),
            amount,
            "NOK");

    [Fact]
    public async Task pep_receiver_with_amount_above_threshold_triggers()
    {
        var rule = new PepCheckRule(new FakePepService(isPep: true));

        var result = await rule.EvaluateAsync(MakeRequest(5_000_001L));

        result.Status.Should().Be(RuleStatus.Triggered);
        result.Rule.Should().Be("pep_check");
    }

    [Fact]
    public async Task pep_receiver_with_amount_exactly_at_threshold_passes()
    {
        // 5_000_000 is NOT > 5_000_000
        var rule = new PepCheckRule(new FakePepService(isPep: true));

        var result = await rule.EvaluateAsync(MakeRequest(5_000_000L));

        result.Status.Should().Be(RuleStatus.Passed);
    }

    [Fact]
    public async Task non_pep_receiver_with_large_amount_passes()
    {
        var rule = new PepCheckRule(new FakePepService(isPep: false));

        var result = await rule.EvaluateAsync(MakeRequest(50_000_000L));

        result.Status.Should().Be(RuleStatus.Passed);
    }

    [Fact]
    public async Task pep_receiver_with_small_amount_passes()
    {
        var rule = new PepCheckRule(new FakePepService(isPep: true));

        var result = await rule.EvaluateAsync(MakeRequest(1_000_000L));

        result.Status.Should().Be(RuleStatus.Passed);
    }
}
