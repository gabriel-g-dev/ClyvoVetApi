using System.Net;
using System.Net.Http.Json;
using ClyvoVetApi.DTOs.Response;
using Xunit;

namespace ClyvoVetApi.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class PetsEndpointsTests(IntegrationTestFixture fixture)
{
    private readonly IntegrationTestFixture _fixture = fixture;

    [Fact]
    public async Task GetPets_WhenCalled_ReturnsPagedList()
    {
        _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/pets?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<PagedResponseDto<PetResponseDto>>();
        Assert.NotNull(content);
        Assert.Equal(1, content.TotalItems);
        Assert.Equal("Rex", content.Data.Single().Nome);
    }

    [Fact]
    public async Task GetPet_WhenPetExists_ReturnsPetWithHistory()
    {
        var seed = _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var response = await client.GetAsync($"/api/pets/{seed.PetId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<PetDetailsResponseDto>();
        Assert.NotNull(content);
        Assert.Equal("Rex", content.Nome);
        Assert.Equal("Tutor Teste", content.DonoNome);
        Assert.Single(content.Consultas);
        Assert.Single(content.Vacinas);
    }

    [Fact]
    public async Task GetPet_WhenPetDoesNotExist_ReturnsNotFoundProblemDetails()
    {
        _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/pets/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Recurso não encontrado", problem.Title);
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
    }

    [Fact]
    public async Task PostPet_WhenPayloadIsValid_ReturnsCreatedWithLocation()
    {
        var seed = _fixture.ResetDatabase();
        var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/pets", new
        {
            nome = "Mingau",
            especie = "Gato",
            raca = "Persa",
            dataNascimento = "2022-03-01",
            peso = 4.2m,
            tutorId = seed.DonoId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<PetDetailsResponseDto>();
        Assert.NotNull(created);
        Assert.Equal("Mingau", created.Nome);

        var follow = await client.GetAsync($"/api/pets/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, follow.StatusCode);
    }

    [Fact]
    public async Task PostPet_WhenPayloadIsInvalid_ReturnsBadRequest()
    {
        var seed = _fixture.ResetDatabase();
        var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/pets", new
        {
            nome = "",
            especie = "Gato",
            raca = "Persa",
            dataNascimento = "2022-03-01",
            peso = 4.2m,
            tutorId = seed.DonoId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPet_WhenDonoDoesNotExist_ReturnsBusinessError()
    {
        _fixture.ResetDatabase();
        var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/pets", new
        {
            nome = "Fantasma",
            especie = "Gato",
            raca = "Persa",
            dataNascimento = "2022-03-01",
            peso = 4.2m,
            tutorId = 9999
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Violação de regra de negócio", problem.Title);
        Assert.Contains("9999", problem.Detail);
    }

    [Fact]
    public async Task PutPet_WhenPayloadIsValid_ReturnsUpdatedPet()
    {
        var seed = _fixture.ResetDatabase();
        var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync($"/api/pets/{seed.PetId}", new
        {
            nome = "Rex Atualizado",
            especie = "Cachorro",
            raca = "Golden",
            dataNascimento = "2020-01-15",
            peso = 27.30m,
            tutorId = seed.DonoId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<PetDetailsResponseDto>();
        Assert.NotNull(updated);
        Assert.Equal("Rex Atualizado", updated.Nome);
        Assert.Equal("Golden", updated.Raca);
    }

    [Fact]
    public async Task DeletePet_WhenPetExists_ReturnsNoContentAndRemovesPet()
    {
        var seed = _fixture.ResetDatabase();
        var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync($"/api/pets/{seed.PetId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var follow = await client.GetAsync($"/api/pets/{seed.PetId}");
        Assert.Equal(HttpStatusCode.NotFound, follow.StatusCode);
    }

    [Fact]
    public async Task DeletePet_WhenPetDoesNotExist_ReturnsNotFound()
    {
        _fixture.ResetDatabase();
        var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync("/api/pets/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetIntelligencePreventiva_WhenPetExists_ReturnsScore()
    {
        var seed = _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var response = await client.GetAsync($"/api/pets/{seed.PetId}/inteligencia-preventiva");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<HealthDashboardResponseDto>();
        Assert.NotNull(content);
        Assert.InRange(content.Score, 0, 100);
        Assert.NotEmpty(content.Recomendacoes);
    }
}
