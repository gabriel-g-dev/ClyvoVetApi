using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClyvoVetApi.Data;
using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClyvoVetApi.Tests.Integration;

public class EvolucoesEndpointsTests
{
    [Theory]
    [InlineData("/api/pets/1/evolucoes")]
    [InlineData("/api/pets/1/evolucoes/00000000000000000000000000000001")]
    public async Task Get_WhenAnonymous_Returns401(string path)
    {
        using var factory = new ClyvoVetWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WhenPetExists_ReturnsCreatedAndNavigableHistoryWithJwtAuthor()
    {
        using var factory = new ClyvoVetWebApplicationFactory();
        using var client = await CreateAuthenticatedClientAsync(factory);
        var petId = await CreatePetAsync(factory);

        var created = await client.PostAsJsonAsync($"/api/pets/{petId}/evolucoes", new EvolucaoClinicaRequestDto
        {
            Texto = "Ativo e sem intercorrências.", Tags = ["retorno"]
        });
        var body = await created.Content.ReadFromJsonAsync<EvolucaoClinicaResponseDto>();
        var read = await client.GetAsync(created.Headers.Location);
        var readBody = await read.Content.ReadFromJsonAsync<EvolucaoClinicaResponseDto>();
        var list = await client.GetFromJsonAsync<PagedResponseDto<EvolucaoClinicaResponseDto>>(
            $"/api/pets/{petId}/evolucoes?tag=retorno&pageSize=1&direction=asc");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(ClyvoVetWebApplicationFactory.Username, body.Autor);
        Assert.Equal(petId, body.PetId);
        Assert.NotNull(readBody);
        Assert.Contains(readBody.Links, link => link.Rel == "self");
        Assert.NotNull(list);
        Assert.Equal(body.Id, Assert.Single(list.Data).Id);
        Assert.Contains(list.Links, link => link.Rel == "self");
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("direction=drop")]
    [InlineData("inicio=2026-10-25T00:00:00Z&fim=2026-10-01T00:00:00Z")]
    public async Task Get_WhenQueryInvalid_Returns400(string query)
    {
        using var factory = new ClyvoVetWebApplicationFactory();
        using var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync($"/api/pets/1/evolucoes?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_WhenPetDoesNotExist_Returns404()
    {
        using var factory = new ClyvoVetWebApplicationFactory();
        using var client = await CreateAuthenticatedClientAsync(factory);
        await CreatePetAsync(factory);

        var response = await client.PostAsJsonAsync("/api/pets/999999/evolucoes", new { texto = "Retorno" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenDocumentBelongsToAnotherPet_Returns404()
    {
        using var factory = new ClyvoVetWebApplicationFactory();
        using var client = await CreateAuthenticatedClientAsync(factory);
        var petId = await CreatePetAsync(factory);
        var created = await client.PostAsJsonAsync($"/api/pets/{petId}/evolucoes", new { texto = "Retorno" });
        var document = await created.Content.ReadFromJsonAsync<EvolucaoClinicaResponseDto>();

        var response = await client.GetAsync($"/api/pets/{petId + 1}/evolucoes/{document!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_WhenMongoFails_Returns503ProblemDetails()
    {
        using var factory = new UnavailableDocumentFactory();
        using var client = await CreateAuthenticatedClientAsync(factory);
        var petId = await CreatePetAsync(factory);

        var response = await client.PostAsJsonAsync($"/api/pets/{petId}/evolucoes", new { texto = "Retorno" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(body);
        Assert.Equal(503, body.Status);
    }

    internal static async Task<HttpClient> CreateAuthenticatedClientAsync(ClyvoVetWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var token = await IntegrationTestFixture.GetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    internal static async Task<int> CreatePetAsync(ClyvoVetWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
        await context.Database.EnsureCreatedAsync();
        var dono = new Dono { Nome = "Tutor Teste", Email = $"{Guid.NewGuid():N}@example.test", Telefone = "11999999999" };
        var pet = new Pet
        {
            Nome = "Rex", Especie = "Cachorro", Raca = "SRD", DataNascimento = new DateTime(2020, 1, 1),
            Peso = 12, Dono = dono
        };
        context.Pets.Add(pet);
        await context.SaveChangesAsync();
        return pet.Id;
    }

    private sealed class UnavailableDocumentFactory : ClyvoVetWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEvolucaoClinicaRepository>();
                services.AddSingleton<IEvolucaoClinicaRepository, UnavailableDocumentRepository>();
            });
        }
    }

    private sealed class UnavailableDocumentRepository : IEvolucaoClinicaRepository
    {
        public Task InsertAsync(EvolucaoClinica evolucao, CancellationToken cancellationToken = default) =>
            throw new DependencyUnavailableException("Armazenamento documental indisponível.");
        public Task<EvolucaoClinica?> GetByIdAsync(int petId, string id, CancellationToken cancellationToken = default) =>
            throw new DependencyUnavailableException("Armazenamento documental indisponível.");
        public Task<(IReadOnlyList<EvolucaoClinica> Items, int Total)> GetByPetAsync(int petId, int page,
            int pageSize, string? tag, DateTime? inicioUtc, DateTime? fimUtc, bool descending,
            CancellationToken cancellationToken = default) =>
            throw new DependencyUnavailableException("Armazenamento documental indisponível.");
    }
}
