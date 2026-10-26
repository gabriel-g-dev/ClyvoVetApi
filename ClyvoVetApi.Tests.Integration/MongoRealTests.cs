using System.Net;
using System.Net.Http.Json;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.HealthChecks;
using ClyvoVetApi.Models;
using ClyvoVetApi.Mongo;
using ClyvoVetApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ClyvoVetApi.Tests.Integration;

public sealed class MongoRealFactAttribute : FactAttribute
{
    public MongoRealFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLYVOVET_MONGO_TEST_CONNECTION")))
            Skip = "Defina CLYVOVET_MONGO_TEST_CONNECTION para executar contra MongoDB real descartável.";
    }
}

[Trait("Category", "MongoReal")]
public class MongoRealTests : IAsyncLifetime
{
    private readonly string _databaseName = $"clyvovet_tests_{Guid.NewGuid():N}";
    private readonly MongoOptions _options;
    private readonly MongoClinicalContext _context;
    private readonly MongoEvolucaoClinicaRepository _repository;

    public MongoRealTests()
    {
        _options = new MongoOptions
        {
            ConnectionString = Environment.GetEnvironmentVariable("CLYVOVET_MONGO_TEST_CONNECTION")
                ?? throw new InvalidOperationException("A conexão para MongoDB real não foi informada."),
            DatabaseName = _databaseName
        };
        _context = new MongoClinicalContext(_options);
        _repository = new MongoEvolucaoClinicaRepository(_context);
    }

    public async Task InitializeAsync() =>
        await _context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

    public async Task DisposeAsync()
    {
        if (!_databaseName.StartsWith("clyvovet_tests_", StringComparison.Ordinal) || _databaseName.Length != 47)
            throw new InvalidOperationException("Limpeza recusada para banco fora do prefixo descartável.");
        await _context.Database.Client.DropDatabaseAsync(_databaseName);
    }

    [MongoRealFact]
    public async Task Repository_WhenDocumentIsInserted_ReadsItFromRealMongoWithPetScope()
    {
        var document = Document(7, DateTime.UtcNow, "retorno");

        await _repository.InsertAsync(document);
        var read = await _repository.GetByIdAsync(7, document.Id);
        var otherPet = await _repository.GetByIdAsync(8, document.Id);

        Assert.NotNull(read);
        Assert.Equal(document.Texto, read.Texto);
        Assert.Equal(document.Autor, read.Autor);
        Assert.Equal(DateTimeKind.Utc, read.RegistradoEmUtc.Kind);
        Assert.Null(otherPet);
    }

    [MongoRealFact]
    public async Task Repository_WhenFiltered_ReturnsStableOrderedPagesAndCorrectTotal()
    {
        var inicio = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var documents = new[]
        {
            Document(7, inicio, "retorno"), Document(7, inicio.AddHours(1), "retorno"),
            Document(7, inicio.AddHours(2), "vacina"), Document(8, inicio, "retorno")
        };
        foreach (var document in documents)
            await _repository.InsertAsync(document);

        var firstPage = await _repository.GetByPetAsync(7, 1, 1, "retorno", inicio, inicio.AddDays(1), false);
        var secondPage = await _repository.GetByPetAsync(7, 2, 1, "retorno", inicio, inicio.AddDays(1), false);
        var descending = await _repository.GetByPetAsync(7, 1, 10, null, null, null, true);

        Assert.Equal(2, firstPage.Total);
        Assert.Equal(documents[0].Id, Assert.Single(firstPage.Items).Id);
        Assert.Equal(documents[1].Id, Assert.Single(secondPage.Items).Id);
        Assert.Equal(documents[2].Id, descending.Items[0].Id);
        Assert.Equal(3, descending.Total);
    }

    [MongoRealFact]
    public async Task HealthCheck_WhenMongoResponds_ReturnsHealthy()
    {
        var check = new MongoHealthCheck(_context, NullLogger<MongoHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [MongoRealFact]
    public async Task Repository_WhenTimestampsTie_UsesIdAsStableTieBreakerInBothDirections()
    {
        var timestamp = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var first = new EvolucaoClinica("00000000000000000000000000000001", 7, "Primeiro", "clinico", timestamp);
        var second = new EvolucaoClinica("00000000000000000000000000000002", 7, "Segundo", "clinico", timestamp);
        await _repository.InsertAsync(second);
        await _repository.InsertAsync(first);

        foreach (var descending in new[] { false, true })
        {
            var firstPage = await _repository.GetByPetAsync(7, 1, 1, null, null, null, descending);
            var secondPage = await _repository.GetByPetAsync(7, 2, 1, null, null, null, descending);

            Assert.Equal(2, firstPage.Total);
            Assert.Equal(2, secondPage.Total);
            Assert.Equal(first.Id, Assert.Single(firstPage.Items).Id);
            Assert.Equal(second.Id, Assert.Single(secondPage.Items).Id);
        }
    }

    [MongoRealFact]
    public async Task Repository_WhenUsed_CreatesTheDeclaredCompoundIndexInMongo()
    {
        await _repository.InsertAsync(Document(7, DateTime.UtcNow, "retorno"));
        var collection = _context.Database.GetCollection<BsonDocument>(_options.CollectionName);

        using var cursor = await collection.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();

        var index = Assert.Single(indexes, item => item["name"] == "pet_data_id");
        Assert.Equal(new BsonDocument { { "PetId", 1 }, { "RegistradoEmUtc", -1 }, { "_id", 1 } }, index["key"]);
        Assert.Contains(indexes, item => item["name"] == "_id_");
    }

    [MongoRealFact]
    public async Task Repository_WhenContextIsRecreated_ReadsTheCompletePersistedDocument()
    {
        var timestamp = new DateTime(2026, 10, 20, 12, 0, 0, 123, DateTimeKind.Utc);
        var document = new EvolucaoClinica(Guid.NewGuid().ToString("N"), 7, "Paciente ativo.",
            "clinico-persistencia", timestamp, ["retorno", "preventivo"]);
        await _repository.InsertAsync(document);
        var independentContext = new MongoClinicalContext(_options);
        var independentRepository = new MongoEvolucaoClinicaRepository(independentContext);

        var read = await independentRepository.GetByIdAsync(7, document.Id);

        Assert.NotNull(read);
        Assert.Equal(document.Id, read.Id);
        Assert.Equal(document.PetId, read.PetId);
        Assert.Equal(document.Texto, read.Texto);
        Assert.Equal(document.Autor, read.Autor);
        Assert.Equal(document.RegistradoEmUtc, read.RegistradoEmUtc);
        Assert.Equal(document.Tags, read.Tags);
    }

    [MongoRealFact]
    public async Task Http_WhenEvolutionIsCreated_PersistsAndQueriesRealMongo()
    {
        using var factory = new RealMongoWebFactory(_options);
        using var client = await EvolucoesEndpointsTests.CreateAuthenticatedClientAsync(factory);
        var petId = await EvolucoesEndpointsTests.CreatePetAsync(factory);

        var created = await client.PostAsJsonAsync($"/api/pets/{petId}/evolucoes", new { texto = "Observação em Mongo real", tags = new[] { "retorno" } });
        var read = await client.GetFromJsonAsync<EvolucaoClinicaResponseDto>(created.Headers.Location);
        var page = await client.GetFromJsonAsync<PagedResponseDto<EvolucaoClinicaResponseDto>>($"/api/pets/{petId}/evolucoes?tag=retorno");
        var persisted = await _repository.GetByIdAsync(petId, read!.Id);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(persisted);
        Assert.Equal("Observação em Mongo real", persisted.Texto);
        Assert.Equal(ClyvoVetWebApplicationFactory.Username, persisted.Autor);
        Assert.Equal(read.Id, Assert.Single(page!.Data).Id);
        Assert.NotEmpty(read.Links);
    }

    private static EvolucaoClinica Document(int petId, DateTime data, string tag) =>
        new(Guid.NewGuid().ToString("N"), petId, "Paciente ativo, sem intercorrências.", "clinico-teste", data, [tag]);

    private sealed class RealMongoWebFactory(MongoOptions options) : ClyvoVetWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<MongoClinicalContext>();
                services.RemoveAll<MongoOptions>();
                services.RemoveAll<IEvolucaoClinicaRepository>();
                services.AddSingleton(options);
                services.AddSingleton<MongoClinicalContext>();
                services.AddScoped<IEvolucaoClinicaRepository, MongoEvolucaoClinicaRepository>();
            });
        }
    }
}
