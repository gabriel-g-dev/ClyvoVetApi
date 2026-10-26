using System.Collections.Concurrent;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;

namespace ClyvoVetApi.Tests.Integration;

// Substituto usado exclusivamente nos testes de contrato HTTP, sem comprovar persistência MongoDB.
internal class InMemoryEvolucaoClinicaRepository : IEvolucaoClinicaRepository
{
    private readonly ConcurrentDictionary<string, EvolucaoClinica> _items = new();

    public Task InsertAsync(EvolucaoClinica evolucao, CancellationToken cancellationToken = default)
    {
        if (!_items.TryAdd(evolucao.Id, evolucao))
            throw new InvalidOperationException("Identificador duplicado no substituto de teste.");
        return Task.CompletedTask;
    }

    public Task<EvolucaoClinica?> GetByIdAsync(int petId, string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.TryGetValue(id, out var item) && item.PetId == petId ? item : null);

    public Task<(IReadOnlyList<EvolucaoClinica> Items, int Total)> GetByPetAsync(int petId,
        int page, int pageSize, string? tag, DateTime? inicioUtc, DateTime? fimUtc,
        bool descending, CancellationToken cancellationToken = default)
    {
        var filtered = _items.Values.Where(x => x.PetId == petId &&
            (tag is null || x.Tags.Contains(tag)) &&
            (!inicioUtc.HasValue || x.RegistradoEmUtc >= inicioUtc.Value) &&
            (!fimUtc.HasValue || x.RegistradoEmUtc <= fimUtc.Value));
        var ordered = descending ? filtered.OrderByDescending(x => x.RegistradoEmUtc).ThenBy(x => x.Id)
            : filtered.OrderBy(x => x.RegistradoEmUtc).ThenBy(x => x.Id);
        IReadOnlyList<EvolucaoClinica> items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        return Task.FromResult((items, filtered.Count()));
    }
}
