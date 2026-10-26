using ClyvoVetApi.Mongo;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ClyvoVetApi.HealthChecks;

public class MongoHealthCheck(MongoClinicalContext context, ILogger<MongoHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthCheckContext,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1),
                cancellationToken: timeout.Token);
            return HealthCheckResult.Healthy("Conexão com o MongoDB estabelecida.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            logger.LogWarning("Health check do MongoDB não conseguiu confirmar a conectividade.");
            return HealthCheckResult.Unhealthy("Não foi possível estabelecer conexão com o MongoDB.");
        }
    }
}
