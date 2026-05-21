using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Services;

namespace TransactionCompliance.Api.Tests;

/// <summary>
/// Integration tests for the duplicate_transaction compliance rule.
/// Uses the real InMemoryTransactionSeenStore (registered as Singleton in Program.cs)
/// so that duplicate detection actually works end-to-end.
/// IPepService is replaced with a no-op fake to avoid interference from the PEP rule.
/// </summary>
public class ScreeningEndpointDuplicateTests : IClassFixture<ComplianceApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ScreeningEndpointDuplicateTests(ComplianceApiFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace PEP service so PEP rule never triggers and obscures results
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPepService));
                if (descriptor != null)
                    services.Remove(descriptor);
                services.AddSingleton<IPepService, NoPepService>();
            });
        });
    }

    // All tests share the SAME HttpClient so they hit the same Singleton store instance
    private HttpClient CreateClient() => _factory.CreateClient();

    private static ScreeningRequest BuildRequest(string transactionId) =>
        new(
            TransactionId: transactionId,
            Sender: new PartyInfo($"ACC-SENDER-DUP-{Guid.NewGuid()}", "Dup Test Sender", "NO"),
            Receiver: new PartyInfo("ACC-RECV-DUP-001", "Dup Test Receiver", "NO"),
            Amount: 100_000L,
            Currency: "NOK"
        );

    [Fact]
    public async Task first_post_returns_approved_and_duplicate_transaction_passed()
    {
        var client = CreateClient();
        var txId = $"TX-DUP-{Guid.NewGuid()}";

        var response = await client.PostAsJsonAsync("/api/v1/screen", BuildRequest(txId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Status.Should().Be(ScreeningStatus.Approved);
        var dupRule = body.Rules.Single(r => r.Rule == "duplicate_transaction");
        dupRule.Status.Should().Be(RuleStatus.Passed);
    }

    [Fact]
    public async Task second_post_same_transactionId_returns_rejected_and_duplicate_transaction_triggered()
    {
        var client = CreateClient();
        var txId = $"TX-DUP-{Guid.NewGuid()}";

        // First POST — records the transaction
        var first = await client.PostAsJsonAsync("/api/v1/screen", BuildRequest(txId));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);

        // Second POST — same transactionId must be detected as duplicate
        var second = await client.PostAsJsonAsync("/api/v1/screen", BuildRequest(txId));

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        secondBody.Should().NotBeNull();
        secondBody!.Status.Should().Be(ScreeningStatus.Rejected);

        var dupRule = secondBody.Rules.Single(r => r.Rule == "duplicate_transaction");
        dupRule.Status.Should().Be(RuleStatus.Triggered);
        dupRule.Severity.Should().Be(RuleSeverity.High);
        dupRule.Message.Should().Be("Transaction has already been screened within the past 24 hours");

        // requestId must differ between first and second call
        secondBody.RequestId.Should().NotBe(firstBody!.RequestId);
    }

    [Fact]
    public async Task third_post_different_transactionId_returns_approved()
    {
        var client = CreateClient();
        var txId1 = $"TX-DUP-{Guid.NewGuid()}";
        var txId2 = $"TX-DUP-{Guid.NewGuid()}";

        // Record txId1 first
        await client.PostAsJsonAsync("/api/v1/screen", BuildRequest(txId1));

        // Different txId — must not be blocked
        var response = await client.PostAsJsonAsync("/api/v1/screen", BuildRequest(txId2));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Status.Should().Be(ScreeningStatus.Approved);
        var dupRule = body.Rules.Single(r => r.Rule == "duplicate_transaction");
        dupRule.Status.Should().Be(RuleStatus.Passed);
    }

}

/// <summary>
/// Returns false for all accounts — PEP rule never triggers.
/// </summary>
internal sealed class NoPepService : IPepService
{
    public Task<bool> IsPepAsync(string accountId, CancellationToken ct = default)
        => Task.FromResult(false);
}
