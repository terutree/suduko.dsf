using System.Text.Json.Serialization;
using TransactionCompliance.Api.Endpoints;
using TransactionCompliance.Api.Middleware;
using TransactionCompliance.Core.Pipeline;
using TransactionCompliance.Core.Rules;
using TransactionCompliance.Core.Services;
using TransactionCompliance.Core.Stores;
using TransactionCompliance.Infrastructure;
using TransactionCompliance.Infrastructure.Services;
using TransactionCompliance.Infrastructure.Stores;

var builder = WebApplication.CreateBuilder(args);

// JSON enum serialization — returns "Approved" not 0
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Register compliance rules (multiple registration pattern — ALL 4 rules)
builder.Services.AddSingleton<IScreeningRule, AmountThresholdRule>();
builder.Services.AddSingleton<IScreeningRule>(_ =>
    new SanctionedCountryRule(CountryLists.SanctionedCountries));
builder.Services.AddSingleton<IScreeningRule, CumulativeDailyLimitRule>();
builder.Services.AddSingleton<IScreeningRule, PepCheckRule>();

// Infrastructure — in-memory implementations
builder.Services.AddSingleton<IScreeningResultStore, InMemoryScreeningResultStore>();
builder.Services.AddSingleton<IDailyAggregateStore, InMemoryDailyAggregateStore>();
builder.Services.AddSingleton<IPepService, InMemoryPepService>();

// Pipeline
builder.Services.AddSingleton<IScreeningPipeline, ScreeningPipeline>();

var app = builder.Build();

// Global exception handler — only in non-Development to expose real errors in tests
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
    {
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    }));
}

// X-Request-Id middleware — must be before endpoints
app.UseMiddleware<RequestIdMiddleware>();

app.MapScreeningEndpoints();
app.MapHealthEndpoints();

app.Run();

// Make Program visible to WebApplicationFactory
public partial class Program { }
