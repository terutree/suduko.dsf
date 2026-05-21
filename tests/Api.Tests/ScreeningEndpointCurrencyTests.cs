using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Services;

namespace TransactionCompliance.Api.Tests;

public class ScreeningEndpointCurrencyTests : IClassFixture<ComplianceApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ScreeningEndpointCurrencyTests(ComplianceApiFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPepService));
                if (descriptor != null)
                    services.Remove(descriptor);
                services.AddSingleton<IPepService, FakePepService>();
            });
        });
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private static ScreeningRequest BuildRequest(string currency, string senderAccountId) =>
        new(
            TransactionId: "TXN-CURR-001",
            Sender: new PartyInfo(senderAccountId, "Test Sender", "NO"),
            Receiver: new PartyInfo($"ACC-RECV-CURR-{Guid.NewGuid()}", "Test Receiver", "NO"),
            Amount: 1_000_000L,
            Currency: currency
        );

    [Fact]
    public async Task Post_JPY_Returns200WithRejectedStatus_AndCurrencyRestrictionTriggered()
    {
        var client = CreateClient();
        var request = BuildRequest("JPY", $"CURR-TEST-JPY-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/api/v1/screen", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Status.Should().Be(ScreeningStatus.Rejected);
        body.Rules.Should().Contain(r => r.Rule == "currency_restriction" && r.Status == RuleStatus.Triggered);
        var currencyRule = body.Rules.Single(r => r.Rule == "currency_restriction");
        currencyRule.Status.Should().Be(RuleStatus.Triggered);
        currencyRule.Severity.Should().Be(RuleSeverity.High);
        currencyRule.Message.Should().Be("Currency is not permitted");
    }

    [Fact]
    public async Task Post_CHF_Returns200WithRejectedStatus_AndCurrencyRestrictionTriggered()
    {
        var client = CreateClient();
        var request = BuildRequest("CHF", $"CURR-TEST-CHF-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/api/v1/screen", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Status.Should().Be(ScreeningStatus.Rejected);
        body.Rules.Should().Contain(r => r.Rule == "currency_restriction" && r.Status == RuleStatus.Triggered);
        var currencyRule = body.Rules.Single(r => r.Rule == "currency_restriction");
        currencyRule.Status.Should().Be(RuleStatus.Triggered);
        currencyRule.Severity.Should().Be(RuleSeverity.High);
        currencyRule.Message.Should().Be("Currency is not permitted");
    }

    [Theory]
    [InlineData("NOK")]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public async Task Post_PermittedCurrency_ReturnsApproved_AndCurrencyRestrictionPassed(string currency)
    {
        var client = CreateClient();
        var request = BuildRequest(currency, $"CURR-TEST-PERMITTED-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/api/v1/screen", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ScreeningResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Status.Should().Be(ScreeningStatus.Approved);
        body.Rules.Should().Contain(r => r.Rule == "currency_restriction" && r.Status == RuleStatus.Passed);
    }
}
