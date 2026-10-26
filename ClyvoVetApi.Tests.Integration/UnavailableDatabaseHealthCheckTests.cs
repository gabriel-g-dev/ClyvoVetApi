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
    public async Task GetPets_WhenDatabaseThrowsUnexpectedException_ReturnsSanitizedCorrelatedProblem()
    {
        var client = _factory.CreateClient();
        var token = await IntegrationTestFixture.GetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "falha-persistencia-sprint4");

        var response = await client.GetAsync("/api/pets");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.Equal("Erro interno do servidor", problem!.Title);
        Assert.Equal("falha-persistencia-sprint4", problem.TraceId);
        Assert.Equal(problem.TraceId, response.Headers.GetValues("X-Correlation-Id").Single());
        Assert.DoesNotContain("pasta-que-nao-existe", body);
        Assert.DoesNotContain("SqliteException", body);
        Assert.DoesNotContain("DataSource", body);
    }

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
