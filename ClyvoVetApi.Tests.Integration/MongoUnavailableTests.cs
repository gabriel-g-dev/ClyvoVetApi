using ClyvoVetApi.Exceptions;
using ClyvoVetApi.HealthChecks;
using ClyvoVetApi.Mongo;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClyvoVetApi.Tests.Integration;

public class MongoUnavailableTests
{
    [Fact]
    public async Task HealthCheck_WhenMongoIsUnreachable_ReturnsUnhealthyWithoutExposingConnection()
    {
        var context = new MongoClinicalContext(new MongoOptions { ConnectionString = "mongodb://127.0.0.1:1" });
        var check = new MongoHealthCheck(context, NullLogger<MongoHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.DoesNotContain("127.0.0.1", result.Description);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task Repository_WhenMongoIsUnreachable_ReportsDependencyFailure()
    {
        var context = new MongoClinicalContext(new MongoOptions { ConnectionString = "mongodb://127.0.0.1:1" });
        var repository = new MongoEvolucaoClinicaRepository(context);

        var error = await Assert.ThrowsAsync<DependencyUnavailableException>(() =>
            repository.GetByIdAsync(1, Guid.NewGuid().ToString("N")));

        Assert.DoesNotContain("127.0.0.1", error.Message);
    }
}
