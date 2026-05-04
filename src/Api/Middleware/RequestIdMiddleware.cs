namespace TransactionCompliance.Api.Middleware;

public class RequestIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var requestId = ctx.Request.Headers["X-Request-Id"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();
        ctx.Items["RequestId"] = requestId;
        ctx.Response.Headers["X-Request-Id"] = requestId;
        await next(ctx);
    }
}
