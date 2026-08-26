using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ClyvoVetApi.Observability;

public static class Telemetry
{
    public const string ServiceName = "ClyvoVetApi";

    public static readonly ActivitySource Source = new(ServiceName);

    private static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> HttpErrors = Meter.CreateCounter<long>(
        "clyvovet.http.errors",
        "{request}",
        "Requisições HTTP concluídas com status de erro");
}
