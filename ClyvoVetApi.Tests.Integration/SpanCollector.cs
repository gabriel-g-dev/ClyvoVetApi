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

    public void Dispose()
    {
        _listener.Dispose();
        GC.SuppressFinalize(this);
    }
}
