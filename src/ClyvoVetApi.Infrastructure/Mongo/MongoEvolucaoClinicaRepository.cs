using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Observability;
using ClyvoVetApi.Repositories.Interfaces;
using MongoDB.Driver;

namespace ClyvoVetApi.Mongo;

public class MongoEvolucaoClinicaRepository(MongoClinicalContext context) : IEvolucaoClinicaRepository
{
    public async Task InsertAsync(EvolucaoClinica evolucao, CancellationToken cancellationToken = default)
    {
        using var activity = Telemetry.Source.StartActivity("MongoEvolucaoClinicaRepository.Insert");
        try
        {
            await context.EnsureIndexesAsync(cancellationToken);
            await context.Evolucoes.InsertOneAsync(EvolucaoClinicaDocument.FromDomain(evolucao),
                cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            throw Unavailable();
        }
    }

    public async Task<EvolucaoClinica?> GetByIdAsync(int petId, string id,
        CancellationToken cancellationToken = default)
    {
        using var activity = Telemetry.Source.StartActivity("MongoEvolucaoClinicaRepository.GetById");
        try
        {
            var document = await context.Evolucoes.Find(x => x.PetId == petId && x.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
            return document?.ToDomain();
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            throw Unavailable();
        }
    }

    public async Task<(IReadOnlyList<EvolucaoClinica> Items, int Total)> GetByPetAsync(int petId,
        int page, int pageSize, string? tag, DateTime? inicioUtc, DateTime? fimUtc,
        bool descending, CancellationToken cancellationToken = default)
    {
        using var activity = Telemetry.Source.StartActivity("MongoEvolucaoClinicaRepository.GetByPet");
        try
        {
            await context.EnsureIndexesAsync(cancellationToken);
            var filters = Builders<EvolucaoClinicaDocument>.Filter;
            var filter = filters.Eq(x => x.PetId, petId);
            if (tag is not null)
                filter &= filters.AnyEq(x => x.Tags, tag);
            if (inicioUtc.HasValue)
                filter &= filters.Gte(x => x.RegistradoEmUtc, inicioUtc.Value);
            if (fimUtc.HasValue)
                filter &= filters.Lte(x => x.RegistradoEmUtc, fimUtc.Value);

            var sort = descending
                ? Builders<EvolucaoClinicaDocument>.Sort.Descending(x => x.RegistradoEmUtc).Ascending(x => x.Id)
                : Builders<EvolucaoClinicaDocument>.Sort.Ascending(x => x.RegistradoEmUtc).Ascending(x => x.Id);
            var total = await context.Evolucoes.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            var documents = await context.Evolucoes.Find(filter).Sort(sort)
                .Skip(checked((page - 1) * pageSize)).Limit(pageSize).ToListAsync(cancellationToken);
            return (documents.Select(document => document.ToDomain()).ToArray(), checked((int)total));
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            throw Unavailable();
        }
    }

    private static DependencyUnavailableException Unavailable() =>
        new("O armazenamento de evoluções clínicas está indisponível. Tente novamente mais tarde.");
}
