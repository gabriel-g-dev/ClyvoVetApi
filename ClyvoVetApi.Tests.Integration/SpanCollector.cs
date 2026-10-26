using System.Collections.Concurrent;
using System.Diagnostics;

namespace ClyvoVetApi.Tests.Integration;

public class SpanCollector : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly ConcurrentBag<Activity> _spans = [];

    public SpanCollector()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "ClyvoVetApi" or "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _spans.Add
        };

        ActivitySource.AddActivityListener(_listener);
    }

    public IReadOnlyCollection<Activity> Spans => _spans;

    public Activity Single(string displayName) =>
        Assert.Single(Spans, span => span.DisplayName == displayName);

    public Activity Single(string displayName, ActivityTraceId traceId) =>
        Assert.Single(Spans, span => span.DisplayName == displayName && span.TraceId == traceId);

    public async Task<Activity> WaitForAsync(string displayName)
    {
        // TestServer pode entregar a resposta antes do callback ActivityStopped da requisição.
        var timeout = Stopwatch.StartNew();
        while (!Spans.Any(span => span.DisplayName == displayName) && timeout.Elapsed < TimeSpan.FromSeconds(3))
            await Task.Delay(10);

        return Single(displayName);
    }

    public void Dispose()
    {
        _listener.Dispose();
        GC.SuppressFinalize(this);
    }
}
