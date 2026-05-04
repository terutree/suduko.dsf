using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Pipeline;
using TransactionCompliance.Core.Stores;

namespace TransactionCompliance.Api.Endpoints;

public static class ScreeningEndpoints
{
    public static IEndpointRouteBuilder MapScreeningEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/screen", async (
            ScreeningRequest request,
            IScreeningPipeline pipeline,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.TransactionId))
                return Results.BadRequest(new { error = "TransactionId is required." });

            if ((object?)request.Sender is null)
                return Results.BadRequest(new { error = "Sender is required." });

            if ((object?)request.Receiver is null)
                return Results.BadRequest(new { error = "Receiver is required." });

            if (string.IsNullOrWhiteSpace(request.Currency))
                return Results.BadRequest(new { error = "Currency is required." });

            var requestId = ctx.Items["RequestId"]?.ToString()
                ?? throw new InvalidOperationException("RequestId middleware not registered");
            var response = await pipeline.ScreenAsync(requestId, request, ct);
            return Results.Ok(response);
        });

        app.MapGet("/api/v1/screen/{requestId}", async (
            string requestId,
            IScreeningResultStore store,
            CancellationToken ct) =>
        {
            var result = await store.GetAsync(requestId, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return app;
    }
}
