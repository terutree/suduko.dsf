using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TransactionCompliance.Api.Tests;

public class HealthEndpointTests : IClassFixture<ComplianceApiFactory>
{
    private readonly ComplianceApiFactory _factory;

    public HealthEndpointTests(ComplianceApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Health_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Health_ResponseContainsStatusOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("status").GetString().Should().Be("Healthy");
    }
}
