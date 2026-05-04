using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Pipeline;
using TransactionCompliance.Core.Rules;
using TransactionCompliance.Core.Stores;

namespace TransactionCompliance.Core.Tests.Pipeline;

public class ScreeningPipelineTests
{
    // ── Fake stores ──────────────────────────────────────────────────────────

    private sealed class FakeScreeningResultStore : IScreeningResultStore
    {
        public Task SaveAsync(ScreeningResponse response, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<ScreeningResponse?> GetAsync(string requestId, CancellationToken ct = default) =>
            Task.FromResult<ScreeningResponse?>(null);
    }

    private sealed class TrackingDailyAggregateStore : IDailyAggregateStore
    {
        public int AddCallCount { get; private set; }

        public Task<long> GetDailyTotalAsync(string accountId, DateOnly date, CancellationToken ct = default) =>
            Task.FromResult(0L);

        public Task AddAsync(string accountId, DateOnly date, long amount, CancellationToken ct = default)
        {
            AddCallCount++;
            return Task.CompletedTask;
        }
    }

    // ── Fake rules ───────────────────────────────────────────────────────────

    private sealed class AlwaysPassRule : IScreeningRule
    {
        public Task<RuleResult> EvaluateAsync(ScreeningRequest request, CancellationToken ct = default) =>
            Task.FromResult(new RuleResult("always_pass", RuleStatus.Passed, RuleSeverity.Low, "Pass"));
    }

    private sealed class FixedResultRule : IScreeningRule
    {
        private readonly RuleResult _result;

        public FixedResultRule(RuleResult result)
        {
            _result = result;
        }

        public Task<RuleResult> EvaluateAsync(ScreeningRequest request, CancellationToken ct = default) =>
            Task.FromResult(_result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ScreeningRequest MakeRequest(string receiverCountry = "NO", long amount = 1_000_000L) =>
        new("TXN-001",
            new PartyInfo("ACC-SENDER", "Sender", "NO"),
            new PartyInfo("ACC-001", "Receiver", receiverCountry),
            amount,
            "NOK");

    private static ScreeningPipeline BuildPipeline(
        IEnumerable<IScreeningRule> rules,
        IDailyAggregateStore? dailyStore = null,
        IScreeningResultStore? resultStore = null)
    {
        return new ScreeningPipeline(
            rules,
            dailyStore ?? new TrackingDailyAggregateStore(),
            resultStore ?? new FakeScreeningResultStore(),
            NullLogger<ScreeningPipeline>.Instance);
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task sanctioned_country_returns_rejected()
    {
        var rejectedResult = new RuleResult("sanctioned_country", RuleStatus.Triggered, RuleSeverity.High, "Receiver country is sanctioned");
        var pipeline = BuildPipeline([new FixedResultRule(rejectedResult)]);

        var response = await pipeline.ScreenAsync("REQ-001", MakeRequest("RU"));

        response.Status.Should().Be(ScreeningStatus.Rejected);
    }

    [Fact]
    public async Task multiple_rules_triggered_strictest_wins()
    {
        var rejectedResult = new RuleResult("sanctioned_country", RuleStatus.Triggered, RuleSeverity.High, "Receiver country is sanctioned");
        var pendingResult = new RuleResult("pep_check", RuleStatus.Triggered, RuleSeverity.Medium, "PEP");

        var pipeline = BuildPipeline([
            new FixedResultRule(rejectedResult),
            new FixedResultRule(pendingResult)
        ]);

        var response = await pipeline.ScreenAsync("REQ-002", MakeRequest());

        response.Status.Should().Be(ScreeningStatus.Rejected);
    }

    [Fact]
    public async Task non_triggered_transaction_returns_approved()
    {
        var pipeline = BuildPipeline([new AlwaysPassRule()]);

        var response = await pipeline.ScreenAsync("REQ-003", MakeRequest());

        response.Status.Should().Be(ScreeningStatus.Approved);
    }

    [Fact]
    public async Task rejected_transaction_does_not_call_add_async()
    {
        var dailyStore = new TrackingDailyAggregateStore();
        var rejectedResult = new RuleResult("sanctioned_country", RuleStatus.Triggered, RuleSeverity.High, "Receiver country is sanctioned");
        var pipeline = BuildPipeline([new FixedResultRule(rejectedResult)], dailyStore: dailyStore);

        await pipeline.ScreenAsync("REQ-004", MakeRequest("RU"));

        dailyStore.AddCallCount.Should().Be(0);
    }
}
