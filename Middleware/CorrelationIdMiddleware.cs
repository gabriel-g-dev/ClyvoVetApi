using System.Diagnostics;
using Serilog.Context;

namespace ClyvoVetApi.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string PropertyName = "CorrelationId";

    private const int MaxLength = 64;

    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[PropertyName] = correlationId;
        Activity.Current?.SetTag("clyvovet.correlation_id", correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(PropertyName, correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var header = context.Request.Headers[HeaderName].ToString();

        if (IsSafe(header))
            return header;

        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }

    private static bool IsSafe(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaxLength
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
