using FluentAssertions;
using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Rules;
using TransactionCompliance.Core.Stores;

namespace TransactionCompliance.Core.Tests.Rules;

public class CumulativeDailyLimitRuleTests
{
    private sealed class FakeDailyAggregateStore : IDailyAggregateStore
    {
        private readonly long _existingTotal;

        public FakeDailyAggregateStore(long existingTotal = 0L)
        {
            _existingTotal = existingTotal;
        }

        public Task<long> GetDailyTotalAsync(string accountId, DateOnly date, CancellationToken ct = default) =>
            Task.FromResult(_existingTotal);

        public Task AddAsync(string accountId, DateOnly date, long amount, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private static ScreeningRequest MakeRequest(long amount) =>
        new("TXN-001",
            new PartyInfo("ACC-001", "Sender", "NO"),
            new PartyInfo("ACC-002", "Receiver", "NO"),
            amount,
            "NOK");

    [Fact]
    public async Task running_total_below_limit_passes()
    {
        var store = new FakeDailyAggregateStore(existingTotal: 10_000_000L);
        var rule = new CumulativeDailyLimitRule(store);

        var result = await rule.EvaluateAsync(MakeRequest(5_000_000L));

        result.Status.Should().Be(RuleStatus.Passed);
    }

    [Fact]
    public async Task running_total_exactly_at_limit_passes()
    {
        // existing 40M + new 10M = exactly 50M, which is NOT > 50M
        var store = new FakeDailyAggregateStore(existingTotal: 40_000_000L);
        var rule = new CumulativeDailyLimitRule(store);

        var result = await rule.EvaluateAsync(MakeRequest(10_000_000L));

        result.Status.Should().Be(RuleStatus.Passed);
    }

    [Fact]
    public async Task running_total_above_limit_triggers()
    {
        // existing 40M + new 10_000_001 = 50_000_001 > 50M
        var store = new FakeDailyAggregateStore(existingTotal: 40_000_000L);
        var rule = new CumulativeDailyLimitRule(store);

        var result = await rule.EvaluateAsync(MakeRequest(10_000_001L));

        result.Status.Should().Be(RuleStatus.Triggered);
        result.Rule.Should().Be("cumulative_daily_limit");
    }
}
