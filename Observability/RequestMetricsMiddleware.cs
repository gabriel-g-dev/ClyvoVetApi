namespace ClyvoVetApi.Observability;

public class RequestMetricsMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var statusCode = context.Response.StatusCode;

        if (statusCode < StatusCodes.Status400BadRequest)
            return;

        Telemetry.HttpErrors.Add(
            1,
            new KeyValuePair<string, object?>("http.request.method", context.Request.Method),
            new KeyValuePair<string, object?>("http.response.status_code", statusCode),
            new KeyValuePair<string, object?>("error.kind", statusCode < 500 ? "client" : "server"));
    }
}
