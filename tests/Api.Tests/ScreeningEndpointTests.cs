using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Services;

namespace TransactionCompliance.Api.Tests;

public class FakePepService : IPepService
{
    public Task<bool> IsPepAsync(string accountId, CancellationToken ct = default) =>
        Task.FromResult(accountId.Equals("ACC-PEP-001", StringComparison.OrdinalIgnoreCase));
}

public class ScreeningEndpointTests : IClassFixture<ComplianceApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ScreeningEndpointTests(ComplianceApiFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace PEP service with fake that returns true for ACC-PEP-001
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPepService));
                if (descriptor != null)
                    services.Remove(descriptor);
                services.AddSingleton<IPepService, FakePepService>();
            });
        });
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private static ScreeningRequest BuildRequest(
        string? transactionId = "TXN-001",
        string senderAccountId = "ACC-SENDER-001",
        string receiverAccountId = "ACC-RECV-001",
        string receiverCountry = "NO",
        long amount = 100_000L,
        string currency = "NOK") =>
        new(
            TransactionId: transactionId!,
            Sender: new PartyInfo(senderAccountId, "Test Sender", "NO"),
            Receiver: new PartyInfo(receiverAccountId, "Test Receiver", receiverCountry),
            Amount: amount,
            Currency: currency
        );

    [Fact]
    public async Task Post_ValidRequest_Returns200WithScreeningResponse()
    {
        var client = CreateClient();
        var request = BuildRequest(senderAccountId: $"ACC-SENDER-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/api/v1/screen", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.TransactionId.Should().Be("TXN-001");
        body.RequestId.Should().NotBeNullOrEmpty();
        body.Rules.Should().NotBeEmpty();
        body.Timestamp.Should().NotBe(default);
    }

    [Fact]
    public async Task Post_SanctionedCountry_ReturnsRejected()
    {
        var client = CreateClient();
        var request = BuildRequest(
            senderAccountId: $"ACC-SENDER-{Guid.NewGuid()}",
            receiverCountry: "RU",
            amount: 100_000L);

        var response = await client.PostAsJsonAsync("/api/v1/screen", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        body!.Status.Should().Be(ScreeningStatus.Rejected);
        body.Rules.Should().Contain(r => r.Rule == "sanctioned_country" && r.Status == RuleStatus.Triggered);
    }

    [Fact]
    public async Task Post_AmountOverThreshold_ReturnsFlagged()
    {
        var client = CreateClient();
        var request = BuildRequest(
            senderAccountId: $"ACC-SENDER-{Guid.NewGuid()}",
            amount: 10_000_001L);

        var response = await client.PostAsJsonAsync("/api/v1/screen", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        body!.Status.Should().Be(ScreeningStatus.Flagged);
        body.Rules.Should().Contain(r => r.Rule == "amount_threshold" && r.Status == RuleStatus.Triggered);
    }

    [Fact]
    public async Task Post_PepReceiverHighAmount_ReturnsPendingReview()
    {
        var client = CreateClient();
        var request = BuildRequest(
            senderAccountId: $"ACC-SENDER-{Guid.NewGuid()}",
            receiverAccountId: "ACC-PEP-001",
            amount: 5_000_001L);

        var response = await client.PostAsJsonAsync("/api/v1/screen", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        body!.Status.Should().Be(ScreeningStatus.PendingReview);
        body.Rules.Should().Contain(r => r.Rule == "pep_check" && r.Status == RuleStatus.Triggered);
    }

    [Fact]
    public async Task Post_MissingTransactionId_Returns400()
    {
        var client = CreateClient();
        // Serialize with null TransactionId — model binding should reject
        var payload = new
        {
            TransactionId = (string?)null,
            Sender = new { AccountId = $"ACC-SENDER-{Guid.NewGuid()}", Name = "Test", Country = "NO" },
            Receiver = new { AccountId = "ACC-RECV-001", Name = "Test", Country = "NO" },
            Amount = 100_000L,
            Currency = "NOK"
        };

        var response = await client.PostAsJsonAsync("/api/v1/screen", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ExistingRequestId_Returns200()
    {
        var client = CreateClient();
        var request = BuildRequest(senderAccountId: $"ACC-SENDER-{Guid.NewGuid()}");

        // First POST to create a result
        var postResponse = await client.PostAsJsonAsync("/api/v1/screen", request);
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var screeningResponse = await postResponse.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        var requestId = screeningResponse!.RequestId;

        // Then GET by requestId
        var getResponse = await client.GetAsync($"/api/v1/screen/{requestId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        body!.RequestId.Should().Be(requestId);
        body.TransactionId.Should().Be("TXN-001");
    }

    [Fact]
    public async Task Get_UnknownRequestId_Returns404()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/screen/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ResponseContainsXRequestIdHeader()
    {
        var client = CreateClient();
        var request = BuildRequest(senderAccountId: $"ACC-SENDER-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/api/v1/screen", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        var headerRequestId = response.Headers.GetValues("X-Request-Id").FirstOrDefault();
        headerRequestId.Should().NotBeNullOrEmpty();
        headerRequestId.Should().Be(body!.RequestId);
    }

    [Fact]
    public async Task Post_NullSender_Returns400()
    {
        var client = CreateClient();
        var payload = new
        {
            TransactionId = "TXN-001",
            Sender = (object?)null,
            Receiver = new { AccountId = "ACC-RECV-001", Name = "Test Receiver", Country = "NO" },
            Amount = 100_000L,
            Currency = "NOK"
        };

        var response = await client.PostAsJsonAsync("/api/v1/screen", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_NullReceiver_Returns400()
    {
        var client = CreateClient();
        var payload = new
        {
            TransactionId = "TXN-001",
            Sender = new { AccountId = "ACC-SENDER-001", Name = "Test Sender", Country = "NO" },
            Receiver = (object?)null,
            Amount = 100_000L,
            Currency = "NOK"
        };

        var response = await client.PostAsJsonAsync("/api/v1/screen", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_MissingCurrency_Returns400()
    {
        var client = CreateClient();
        var payload = new
        {
            TransactionId = "TXN-001",
            Sender = new { AccountId = "ACC-SENDER-001", Name = "Test Sender", Country = "NO" },
            Receiver = new { AccountId = "ACC-RECV-001", Name = "Test Receiver", Country = "NO" },
            Amount = 100_000L,
            Currency = (string?)null
        };

        var response = await client.PostAsJsonAsync("/api/v1/screen", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
