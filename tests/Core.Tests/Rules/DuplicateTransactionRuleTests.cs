using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Rules;
using TransactionCompliance.Core.Stores;

namespace TransactionCompliance.Core.Tests.Rules;

/// <summary>
/// Stub that always returns the configured value — no state, no real store.
/// </summary>
internal sealed class StubTransactionSeenStore : ITransactionSeenStore
{
    private readonly bool _isNew;

    public StubTransactionSeenStore(bool isNew)
    {
        _isNew = isNew;
    }

    public Task<bool> TryRecordAsync(string transactionId, DateTimeOffset seenAt, CancellationToken ct = default)
        => Task.FromResult(_isNew);
}

public class DuplicateTransactionRuleTests
{
    private static ScreeningRequest MakeRequest(string? txId = null) =>
        new(
            TransactionId: txId ?? $"TX-DUP-{Guid.NewGuid()}",
            Sender: new PartyInfo("ACC-SENDER", "Sender", "NO"),
            Receiver: new PartyInfo("ACC-RECV", "Receiver", "NO"),
            Amount: 1_000_000L,
            Currency: "NOK"
        );

    [Fact]
    public async Task rule_returns_triggered_high_when_store_returns_false_duplicate()
    {
        var rule = new DuplicateTransactionRule(
            seenStore: new StubTransactionSeenStore(isNew: false),
            timeProvider: TimeProvider.System);

        var result = await rule.EvaluateAsync(MakeRequest());

        result.Status.Should().Be(RuleStatus.Triggered);
        result.Severity.Should().Be(RuleSeverity.High);
        result.Message.Should().Be("Transaction has already been screened within the past 24 hours");
        result.Rule.Should().Be("duplicate_transaction");
    }

    [Fact]
    public async Task rule_returns_passed_high_when_store_returns_true_new()
    {
        var rule = new DuplicateTransactionRule(
            seenStore: new StubTransactionSeenStore(isNew: true),
            timeProvider: TimeProvider.System);

        var result = await rule.EvaluateAsync(MakeRequest());

        result.Status.Should().Be(RuleStatus.Passed);
        result.Severity.Should().Be(RuleSeverity.High);
        result.Rule.Should().Be("duplicate_transaction");
    }
}
