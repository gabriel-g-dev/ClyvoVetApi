using System.Diagnostics;
using ClyvoVetApi.Logging;
using OpenTelemetry;

namespace ClyvoVetApi.Observability;

/// <summary>Runs before exporters, so raw route values never leave in server span attributes.</summary>
public sealed class SafeTelemetryProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Server)
        {
            var route = activity.GetTagItem("http.route") as string;
            activity.SetTag("url.path", route is null ? "/*" : $"/{route.TrimStart('/')}");
            activity.SetTag("url.query", null);
            activity.SetTag("url.full", null);
            activity.SetTag("http.url", null);
        }

        foreach (var tag in activity.TagObjects.ToArray())
        {
            if (tag.Key is "exception.message" or "exception.stacktrace")
                activity.SetTag(tag.Key, null);
            else if (tag.Value is string text)
                activity.SetTag(tag.Key, SensitiveDataMasker.MaskSensitiveText(text));
        }
        // Descriptions supplied by dependency exceptions can contain arbitrary secrets.
        if (activity.Status == ActivityStatusCode.Error)
            activity.SetStatus(ActivityStatusCode.Error);
    }
}
