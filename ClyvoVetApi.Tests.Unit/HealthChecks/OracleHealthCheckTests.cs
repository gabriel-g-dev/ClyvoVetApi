using ClyvoVetApi.Data;
using ClyvoVetApi.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClyvoVetApi.Tests.Unit.HealthChecks;

public class OracleHealthCheckTests
{
    private const string ConexaoInvalida = "Data Source=;User Id=;Password=";

    private static AppDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseOracle(ConexaoInvalida).Options);

    private static OracleHealthCheck NovoCheck(AppDbContext contexto) =>
        new(contexto, NullLogger<OracleHealthCheck>.Instance);

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsUnreachable_ReturnsUnhealthy()
    {
        using var contexto = NovoContexto();

        var resultado = await NovoCheck(contexto).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
        Assert.Equal("Não foi possível estabelecer conexão com o banco Oracle.", resultado.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionFails_ThrowsNothingToTheCaller()
    {
        var contexto = NovoContexto();
        await contexto.DisposeAsync();

        var resultado = await NovoCheck(contexto).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
        Assert.Equal("Falha ao verificar a conectividade com o banco Oracle.", resultado.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsUnreachable_DoesNotExposeConnectionString()
    {
        using var contexto = NovoContexto();

        var resultado = await NovoCheck(contexto).CheckHealthAsync(new HealthCheckContext());

        Assert.DoesNotContain("Password", resultado.Description);
        Assert.DoesNotContain("User Id", resultado.Description);
        Assert.DoesNotContain("Data Source", resultado.Description);
        Assert.Null(resultado.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsUnreachable_RespondsWithinTheConfiguredTimeout()
    {
        using var contexto = NovoContexto();
        var inicio = DateTime.UtcNow;

        await NovoCheck(contexto).CheckHealthAsync(new HealthCheckContext());

        Assert.True(DateTime.UtcNow - inicio < TimeSpan.FromSeconds(10));
    }
}
