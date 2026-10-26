using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;

namespace ClyvoVetApi.Services.Interfaces;

public interface IEvolucaoClinicaService
{
    Task<EvolucaoClinicaResponseDto> CreateAsync(int petId, EvolucaoClinicaRequestDto dto,
        string autor, CancellationToken cancellationToken = default);
    Task<EvolucaoClinicaResponseDto> GetByIdAsync(int petId, string id,
        CancellationToken cancellationToken = default);
    Task<PagedResponseDto<EvolucaoClinicaResponseDto>> GetByPetAsync(int petId,
        EvolucaoClinicaQueryDto query, CancellationToken cancellationToken = default);
}
