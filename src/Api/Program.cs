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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Transaction Compliance API", Version = "v1" });
});

// JSON enum serialization — returns "Approved" not 0
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Infrastructure — TimeProvider and duplicate-transaction store
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITransactionSeenStore, InMemoryTransactionSeenStore>();

// Register compliance rules (multiple registration pattern — ALL rules)
// DuplicateTransactionRule MUST be first so duplicate detection short-circuits before other rules record state
builder.Services.AddSingleton<IScreeningRule, DuplicateTransactionRule>();
builder.Services.AddSingleton<IScreeningRule, AmountThresholdRule>();
builder.Services.AddSingleton<IScreeningRule>(_ =>
    new SanctionedCountryRule(CountryLists.SanctionedCountries));
builder.Services.AddSingleton<IScreeningRule, CumulativeDailyLimitRule>();
builder.Services.AddSingleton<IScreeningRule, PepCheckRule>();
builder.Services.AddSingleton<IScreeningRule>(_ =>
    new CurrencyRestrictionRule(new[] { "NOK", "EUR", "USD", "GBP" }));

// Infrastructure — remaining in-memory implementations
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

// Swagger — dev only, after exception handler
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transaction Compliance API v1"));
}

// X-Request-Id middleware — must be before endpoints
app.UseMiddleware<RequestIdMiddleware>();

app.MapScreeningEndpoints();
app.MapHealthEndpoints();

app.Run();

// Make Program visible to WebApplicationFactory
public partial class Program { }
