using ClyvoVetApi.Data;
using ClyvoVetApi.Logging;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVetApi.Services;

public class AlertScheduler(
    IServiceScopeFactory scopeFactory,
    ILogger<AlertScheduler> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<AlertScheduler> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("⏰ ClyvoVet AlertScheduler Background Service inicializado e rodando.");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("⏰ ClyvoVet AlertScheduler: Iniciando varredura preventiva de vacinas...");

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var limiteData = DateTime.Today.AddDays(3);

                var vacinasPendentes = await context.Vacinas
                    .Include(v => v.Pet)
                        .ThenInclude(p => p!.Dono)
                    .Where(v => v.Status == "P" && v.Data <= limiteData)
                    .ToListAsync(stoppingToken);

                if (vacinasPendentes.Count == 0)
                {
                    _logger.LogInformation("⏰ ClyvoVet AlertScheduler: Nenhuma vacina pendente ou em atraso identificada para envio de alertas.");
                    continue;
                }

                _logger.LogInformation("⏰ ClyvoVet AlertScheduler: Identificadas {Count} vacinas críticas. Iniciando simulação de notificações...", vacinasPendentes.Count);

                foreach (var vacina in vacinasPendentes)
                {
                    var pet = vacina.Pet;
                    var dono = pet?.Dono;

                    if (dono != null && pet != null)
                    {
                        var statusVacina = vacina.Data < DateTime.Today ? "VENCIDA ❌" : "PRÓXIMA 📅";

                        _logger.LogWarning(
                            "🔔 [NOTIFICAÇÃO SIMULADA] DonoId: {DonoId} <{DonoEmail}> | PetId: {PetId} | VacinaId: {VacinaId} | Vacina: {VacinaNome} | Status: {Status} | Data Limite: {Data:dd/MM/yyyy}",
                            dono.Id,
                            SensitiveDataMasker.MaskEmails(dono.Email),
                            pet.Id,
                            vacina.Id,
                            vacina.Nome,
                            statusVacina,
                            vacina.Data
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⏰ ClyvoVet AlertScheduler: Ocorreu um erro ao processar o agendamento de alertas.");
            }
        }
    }
}
