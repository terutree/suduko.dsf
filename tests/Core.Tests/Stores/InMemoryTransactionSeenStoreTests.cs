using System.Collections.Concurrent;
using TransactionCompliance.Core.Stores;

namespace TransactionCompliance.Core.Tests.Stores;

/// <summary>
/// Controllable TimeProvider for store tests — no external package required.
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
}

/// <summary>
/// Inline copy of InMemoryTransactionSeenStore to keep Core.Tests free of
/// any Infrastructure project reference.  The logic is byte-for-byte identical
/// to the real implementation in src/Infrastructure/Stores/.
/// </summary>
internal sealed class TestableTransactionSeenStore : ITransactionSeenStore
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromHours(24);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _store = new();
    private readonly TimeProvider _timeProvider;

    public TestableTransactionSeenStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<bool> TryRecordAsync(string transactionId, DateTimeOffset seenAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var now = _timeProvider.GetUtcNow();
        var seenAtUtc = seenAt.ToUniversalTime();

        if (_store.TryAdd(transactionId, seenAtUtc))
            return Task.FromResult(true);

        var existing = _store[transactionId];
        var age = now - existing;
        if (age < DuplicateWindow)
            return Task.FromResult(false);

        if (_store.TryRemove(new KeyValuePair<string, DateTimeOffset>(transactionId, existing)))
        {
            _store.TryAdd(transactionId, seenAtUtc);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}

public class InMemoryTransactionSeenStoreTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task first_call_returns_true_new_entry()
    {
        var timeProvider = new FakeTimeProvider(BaseTime);
        var store = new TestableTransactionSeenStore(timeProvider);
        var txId = $"TX-DUP-{Guid.NewGuid()}";

        var result = await store.TryRecordAsync(txId, timeProvider.GetUtcNow());

        result.Should().BeTrue("first call should be treated as a new unique transaction");
    }

    [Fact]
    public async Task second_call_within_24h_returns_false_duplicate()
    {
        var timeProvider = new FakeTimeProvider(BaseTime);
        var store = new TestableTransactionSeenStore(timeProvider);
        var txId = $"TX-DUP-{Guid.NewGuid()}";

        await store.TryRecordAsync(txId, timeProvider.GetUtcNow());

        // Advance to just before the window expires (23h 59m 59s)
        timeProvider.Advance(TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59)).Add(TimeSpan.FromSeconds(59)));

        var result = await store.TryRecordAsync(txId, timeProvider.GetUtcNow());

        result.Should().BeFalse("second call within 24 h should be identified as a duplicate");
    }

    [Fact]
    public async Task call_after_24h_plus_1s_returns_true_expired()
    {
        var timeProvider = new FakeTimeProvider(BaseTime);
        var store = new TestableTransactionSeenStore(timeProvider);
        var txId = $"TX-DUP-{Guid.NewGuid()}";

        await store.TryRecordAsync(txId, timeProvider.GetUtcNow());

        // Advance to exactly 24 h + 1 s — entry has expired
        timeProvider.Advance(TimeSpan.FromHours(24).Add(TimeSpan.FromSeconds(1)));

        var result = await store.TryRecordAsync(txId, timeProvider.GetUtcNow());

        result.Should().BeTrue("call after the 24-hour window should be treated as a new transaction");
    }
}
