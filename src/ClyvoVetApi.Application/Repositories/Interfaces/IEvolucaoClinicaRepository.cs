using ClyvoVetApi.Models;

namespace ClyvoVetApi.Repositories.Interfaces;

public interface IEvolucaoClinicaRepository
{
    Task InsertAsync(EvolucaoClinica evolucao, CancellationToken cancellationToken = default);
    Task<EvolucaoClinica?> GetByIdAsync(int petId, string id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<EvolucaoClinica> Items, int Total)> GetByPetAsync(int petId,
        int page, int pageSize, string? tag, DateTime? inicioUtc, DateTime? fimUtc,
        bool descending, CancellationToken cancellationToken = default);
}
