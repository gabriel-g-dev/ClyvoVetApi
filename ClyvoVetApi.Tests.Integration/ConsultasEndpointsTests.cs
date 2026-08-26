using System.Net;
using System.Net.Http.Json;
using ClyvoVetApi.DTOs.Response;
using Xunit;

namespace ClyvoVetApi.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class ConsultasEndpointsTests(IntegrationTestFixture fixture)
{
    private readonly IntegrationTestFixture _fixture = fixture;

    [Fact]
    public async Task PostConsulta_WhenPayloadIsValid_ReturnsCreated()
    {
        var seed = _fixture.ResetDatabase();
        var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/consultas", new
        {
            data = DateTime.Today.AddDays(7),
            tipo = "Retorno",
            descricao = "Avaliação pós-tratamento",
            valor = 120.00m,
            status = "A",
            petId = seed.PetId,
            funcionarioId = seed.FuncionarioId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ConsultaResponseDto>();
        Assert.NotNull(created);
        Assert.Equal("Retorno", created.Tipo);
        Assert.Equal("Rex", created.PetNome);
        Assert.Equal("Dra. Ana", created.FuncionarioNome);
    }

    [Fact]
    public async Task PostConsulta_WhenStatusIsInvalid_ReturnsBadRequest()
    {
        var seed = _fixture.ResetDatabase();
        var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/consultas", new
        {
            data = DateTime.Today.AddDays(7),
            tipo = "Retorno",
            descricao = "Avaliação",
            valor = 120.00m,
            status = "X",
            petId = seed.PetId,
            funcionarioId = seed.FuncionarioId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostConsulta_WhenPetDoesNotExist_ReturnsBusinessError()
    {
        var seed = _fixture.ResetDatabase();
        var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/consultas", new
        {
            data = DateTime.Today.AddDays(7),
            tipo = "Retorno",
            descricao = "Avaliação",
            valor = 120.00m,
            status = "A",
            petId = 9999,
            funcionarioId = seed.FuncionarioId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Violação de regra de negócio", problem.Title);
    }

    [Fact]
    public async Task PostConsulta_WhenTokenIsMissing_ReturnsUnauthorized()
    {
        var seed = _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/consultas", new
        {
            data = DateTime.Today.AddDays(7),
            tipo = "Retorno",
            descricao = "Avaliação",
            valor = 120.00m,
            status = "A",
            petId = seed.PetId,
            funcionarioId = seed.FuncionarioId
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConsultaByPeriodo_WhenStartIsAfterEnd_ReturnsBusinessError()
    {
        _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var inicio = DateTime.Today.AddDays(10).ToString("yyyy-MM-dd");
        var fim = DateTime.Today.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/consultas/periodo?inicio={inicio}&fim={fim}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Violação de regra de negócio", problem.Title);
    }

    [Fact]
    public async Task GetConsulta_WhenConsultaDoesNotExist_ReturnsNotFound()
    {
        _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/consultas/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteConsulta_WhenConsultaExists_ReturnsNoContent()
    {
        var seed = _fixture.ResetDatabase();
        var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync($"/api/consultas/{seed.ConsultaId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var follow = await client.GetAsync($"/api/consultas/{seed.ConsultaId}");
        Assert.Equal(HttpStatusCode.NotFound, follow.StatusCode);
    }
}
