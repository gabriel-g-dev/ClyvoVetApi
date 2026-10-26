using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Observability;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services.Interfaces;

namespace ClyvoVetApi.Services;

public class EvolucaoClinicaService(IEvolucaoClinicaRepository repository, IPetRepository petRepository)
    : IEvolucaoClinicaService
{
    public async Task<EvolucaoClinicaResponseDto> CreateAsync(int petId, EvolucaoClinicaRequestDto dto,
        string autor, CancellationToken cancellationToken = default)
    {
        using var activity = Telemetry.Source.StartActivity("EvolucaoClinicaService.Create");
        activity?.SetTag("pet.id", petId);
        var evolucao = new EvolucaoClinica(Guid.NewGuid().ToString("N"), petId, dto.Texto,
            autor, DateTime.UtcNow, dto.Tags);
        _ = await petRepository.GetByIdAsync(petId)
            ?? throw new NotFoundException($"Pet com ID {petId} não encontrado.");

        await repository.InsertAsync(evolucao, cancellationToken);
        return ToResponse(evolucao);
    }

    public async Task<EvolucaoClinicaResponseDto> GetByIdAsync(int petId, string id,
        CancellationToken cancellationToken = default)
    {
        if (petId <= 0 || !Guid.TryParseExact(id, "N", out _))
            throw new BusinessException("Informe um pet e um identificador de evolução válidos.");

        var evolucao = await repository.GetByIdAsync(petId, id, cancellationToken)
            ?? throw new NotFoundException("Evolução clínica não encontrada para o pet informado.");
        return ToResponse(evolucao);
    }

    public async Task<PagedResponseDto<EvolucaoClinicaResponseDto>> GetByPetAsync(int petId,
        EvolucaoClinicaQueryDto query, CancellationToken cancellationToken = default)
    {
        if (petId <= 0 || query.Page is < 1 or > 1000000 || query.PageSize is < 1 or > 100)
            throw new BusinessException("Informe um pet positivo, página de 1 a 1000000 e tamanho de 1 a 100.");
        if (query.Direction is not ("asc" or "desc"))
            throw new BusinessException("A direção deve ser asc ou desc.");
        if (query.Tag is not null && (string.IsNullOrWhiteSpace(query.Tag) || query.Tag.Length > 40))
            throw new BusinessException("A tag deve conter entre 1 e 40 caracteres.");
        if (query.Inicio > query.Fim)
            throw new BusinessException("A data inicial não pode ser posterior à data final.");

        var (items, total) = await repository.GetByPetAsync(petId, query.Page, query.PageSize,
            query.Tag?.Trim(), query.Inicio?.UtcDateTime, query.Fim?.UtcDateTime,
            query.Direction == "desc", cancellationToken);
        return new PagedResponseDto<EvolucaoClinicaResponseDto>
        {
            Data = items.Select(ToResponse).ToArray(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = total
        };
    }

    private static EvolucaoClinicaResponseDto ToResponse(EvolucaoClinica evolucao) => new()
    {
        Id = evolucao.Id,
        PetId = evolucao.PetId,
        Texto = evolucao.Texto,
        Autor = evolucao.Autor,
        RegistradoEmUtc = evolucao.RegistradoEmUtc,
        Tags = evolucao.Tags
    };
}
