using ClyvoVetApi.Middleware;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClyvoVetApi.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            timestamp = DateTimeOffset.UtcNow,
            correlationId = context.Items[CorrelationIdMiddleware.PropertyName] as string,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                description = entry.Value.Description,
                tags = entry.Value.Tags
            })
        };

        return context.Response.WriteAsJsonAsync(payload);
    }
}
