using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ClyvoVetApi.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class HealthCheckTests(IntegrationTestFixture fixture)
{
    private readonly IntegrationTestFixture _fixture = fixture;

    [Fact]
    public async Task GetHealthLive_WhenCalled_ReturnsHealthy()
    {
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<HealthReportResponse>();
        Assert.NotNull(report);
        Assert.Equal("Healthy", report.Status);
        Assert.Equal("self", report.Checks.Single().Name);
    }

    [Fact]
    public async Task GetHealthLive_WhenCalled_DoesNotRequireAuthentication()
    {
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHealthLive_WhenCalled_ExcludesDatabaseCheck()
    {
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/health/live");

        var report = await response.Content.ReadFromJsonAsync<HealthReportResponse>();
        Assert.NotNull(report);
        Assert.DoesNotContain(report.Checks, check => check.Name == "oracle");
        Assert.All(report.Checks, check => Assert.Contains("live", check.Tags));
    }

    [Fact]
    public async Task GetHealthLive_WhenClientSendsCorrelationId_EchoesItBack()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "teste-integracao");

        var response = await client.GetAsync("/health/live");

        Assert.Equal("teste-integracao", response.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task GetHealthReady_WhenDatabaseIsReachable_ReturnsHealthy()
    {
        _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<HealthReportResponse>();
        Assert.NotNull(report);
        Assert.Equal("Healthy", report.Status);

        var databaseCheck = Assert.Single(report.Checks);
        Assert.Equal("oracle", databaseCheck.Name);
        Assert.Equal("Healthy", databaseCheck.Status);
        Assert.Contains("ready", databaseCheck.Tags);
    }

    [Fact]
    public async Task GetHealthReady_WhenCalled_DoesNotRequireAuthentication()
    {
        _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_WhenCalled_AggregatesLivenessAndReadinessChecks()
    {
        _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<HealthReportResponse>();
        Assert.NotNull(report);
        Assert.Equal("Healthy", report.Status);
        Assert.Equal(2, report.Checks.Count);
        Assert.Contains(report.Checks, check => check.Name == "self");
        Assert.Contains(report.Checks, check => check.Name == "oracle");
    }

    [Fact]
    public async Task GetHealth_WhenCalled_ReturnsDurationsAndCorrelationId()
    {
        _fixture.ResetDatabase();
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "health-payload");

        var response = await client.GetAsync("/health");

        var report = await response.Content.ReadFromJsonAsync<HealthReportResponse>();
        Assert.NotNull(report);
        Assert.Equal("health-payload", report.CorrelationId);
        Assert.True(report.TotalDurationMs >= 0);
        Assert.All(report.Checks, check => Assert.True(check.DurationMs >= 0));
    }
}
