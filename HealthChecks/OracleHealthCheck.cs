using ClyvoVetApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClyvoVetApi.HealthChecks;

public class OracleHealthCheck(AppDbContext context, ILogger<OracleHealthCheck> logger) : IHealthCheck
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(5);

    private readonly AppDbContext _context = context;
    private readonly ILogger<OracleHealthCheck> _logger = logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthCheckContext,
        CancellationToken cancellationToken = default)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(ConnectionTimeout);

        try
        {
            if (await _context.Database.CanConnectAsync(timeoutSource.Token))
                return HealthCheckResult.Healthy("Conexão com o banco Oracle estabelecida.");

            _logger.LogWarning("Health check do Oracle não conseguiu conectar ao banco.");
            return HealthCheckResult.Unhealthy("Não foi possível estabelecer conexão com o banco Oracle.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Health check do Oracle excedeu o limite de {TimeoutSeconds}s.",
                ConnectionTimeout.TotalSeconds);

            return HealthCheckResult.Unhealthy("Tempo limite excedido ao verificar o banco Oracle.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Health check do Oracle falhou.");
            return HealthCheckResult.Unhealthy("Falha ao verificar a conectividade com o banco Oracle.");
        }
    }
}
