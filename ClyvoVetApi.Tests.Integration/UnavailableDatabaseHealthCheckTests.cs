using System.Net;
using System.Net.Http.Json;
using ClyvoVetApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClyvoVetApi.Tests.Integration;

public class UnavailableDatabaseFactory : ClyvoVetWebApplicationFactory
{
    private const string CaminhoInacessivel = "DataSource=/pasta-que-nao-existe/clyvovet.db";

    protected override void ConfigureDatabase(IServiceCollection services) =>
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(CaminhoInacessivel));
}

public class UnavailableDatabaseHealthCheckTests : IDisposable
{
    private readonly UnavailableDatabaseFactory _factory = new();

    [Fact]
    public async Task GetHealthReady_WhenDatabaseIsUnreachable_ReturnsServiceUnavailable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<HealthReportResponse>();
        Assert.NotNull(report);
        Assert.Equal("Unhealthy", report.Status);

        var databaseCheck = Assert.Single(report.Checks);
        Assert.Equal("oracle", databaseCheck.Name);
        Assert.Equal("Unhealthy", databaseCheck.Status);
    }

    [Fact]
    public async Task GetHealthLive_WhenDatabaseIsUnreachable_RemainsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<HealthReportResponse>();
        Assert.NotNull(report);
        Assert.Equal("Healthy", report.Status);
    }

    [Fact]
    public async Task GetHealth_WhenDatabaseIsUnreachable_ReportsSelfHealthyAndDatabaseUnhealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<HealthReportResponse>();
        Assert.NotNull(report);
        Assert.Equal("Unhealthy", report.Status);
        Assert.Equal("Healthy", report.Checks.Single(check => check.Name == "self").Status);
        Assert.Equal("Unhealthy", report.Checks.Single(check => check.Name == "oracle").Status);
    }

    [Fact]
    public async Task GetHealthReady_WhenDatabaseIsUnreachable_DoesNotExposeConnectionDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("DataSource", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pasta-que-nao-existe", body);
        Assert.DoesNotContain("SqliteException", body);
        Assert.DoesNotContain("at ", body);
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
