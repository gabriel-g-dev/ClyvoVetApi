namespace ClyvoVetApi.Tests.Integration;

public class HealthReportResponse
{
    public string Status { get; set; } = string.Empty;
    public double TotalDurationMs { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public List<HealthCheckEntry> Checks { get; set; } = [];
}

public class HealthCheckEntry
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public List<string> Tags { get; set; } = [];
}
