using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Observability;
using ClyvoVetApi.Prevention;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services.Interfaces;

namespace ClyvoVetApi.Services;

public class IntelligenceService(IPetRepository petRepository, TimeProvider? timeProvider = null) : IIntelligenceService
{
    private readonly IPetRepository _petRepository = petRepository;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<HealthDashboardResponseDto> GetIntelligencePreventivaAsync(int petId)
    {
        using var activity = Telemetry.Source.StartActivity("IntelligenceService.GetIntelligencePreventiva");
        activity?.SetTag("pet.id", petId);

        var pet = await _petRepository.GetByIdAsync(petId);
        if (pet == null)
        {
            throw new NotFoundException($"Pet com ID {petId} não foi encontrado.");
        }

        var assessment = PreventiveHealthAssessment.Evaluate(pet, (_timeProvider.GetLocalNow()).Date);

        return new HealthDashboardResponseDto
        {
            Score = assessment.Score,
            Status = assessment.Status,
            Recomendacoes = assessment.Recomendacoes.ToList()
        };
    }
}
