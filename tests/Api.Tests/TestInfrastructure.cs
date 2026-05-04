using System.IO.Pipelines;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TransactionCompliance.Api.Tests;

/// <summary>
/// Wraps a PipeWriter to implement UnflushedBytes and CanGetUnflushedBytes,
/// which are required by System.Text.Json on .NET 10 but absent from the test
/// host's ResponseBodyPipeWriter when running net8.0-targeted assemblies on a
/// .NET 10 runtime via rollForward:Major.
/// </summary>
internal sealed class UnflushedBytesAwarePipeWriter(PipeWriter inner) : PipeWriter
{
    public override bool CanGetUnflushedBytes => true;
    public override long UnflushedBytes => 0;
    public override void Advance(int bytes) => inner.Advance(bytes);
    public override void CancelPendingFlush() => inner.CancelPendingFlush();
    public override void Complete(Exception? exception = null) => inner.Complete(exception);
    public override ValueTask<FlushResult> FlushAsync(CancellationToken ct = default) =>
        inner.FlushAsync(ct);
    public override Memory<byte> GetMemory(int sizeHint = 0) => inner.GetMemory(sizeHint);
    public override Span<byte> GetSpan(int sizeHint = 0) => inner.GetSpan(sizeHint);
}

internal sealed class WrappedResponseBodyFeature(IHttpResponseBodyFeature inner)
    : IHttpResponseBodyFeature
{
    private readonly UnflushedBytesAwarePipeWriter _writer = new(inner.Writer);

    public Stream Stream => inner.Stream;
    public PipeWriter Writer => _writer;

    public Task CompleteAsync() => inner.CompleteAsync();
    public void DisableBuffering() => inner.DisableBuffering();
    public Task SendFileAsync(string path, long offset, long? count, CancellationToken ct) =>
        inner.SendFileAsync(path, offset, count, ct);
    public Task StartAsync(CancellationToken ct = default) => inner.StartAsync(ct);
}

internal sealed class ResponseBodyPipeWriterFixMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var feature = ctx.Features.Get<IHttpResponseBodyFeature>();
        if (feature is not null)
        {
            ctx.Features.Set<IHttpResponseBodyFeature>(new WrappedResponseBodyFeature(feature));
        }
        await next(ctx);
    }
}

internal sealed class ResponseBodyPipeWriterStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.UseMiddleware<ResponseBodyPipeWriterFixMiddleware>();
            next(app);
        };
}

/// <summary>
/// Shared <see cref="WebApplicationFactory{TEntryPoint}"/> that injects the
/// <see cref="ResponseBodyPipeWriterStartupFilter"/> to ensure compatibility
/// when running net8.0-targeted tests on the .NET 10 runtime.
/// </summary>
public class ComplianceApiFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(_ => new ResponseBodyPipeWriterStartupFilter());
        });
        return base.CreateHost(builder);
    }
}
