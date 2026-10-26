using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClyvoVetApi.Data;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ClyvoVetApi.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class RestContractTests(IntegrationTestFixture fixture)
{
    public static TheoryData<string> ClinicalGetRoutes => new()
    {
        "/api/tutores", "/api/tutores/1", "/api/tutores/email/tutor%40clyvovet.com", "/api/tutores/1/pets",
        "/api/pets", "/api/pets/1", "/api/pets/especie/Cachorro", "/api/pets/raca/Labrador",
        "/api/pets/1/vacinas", "/api/pets/1/consultas", "/api/pets/1/inteligencia-preventiva",
        "/api/consultas", "/api/consultas/1", "/api/consultas/funcionario/1",
        "/api/consultas/periodo?inicio=2026-01-01&fim=2026-12-31",
        "/api/vacinas", "/api/vacinas/1", "/api/vacinas/pendentes", "/api/vacinas/nome/Anti", "/api/vacinas/proximas",
        "/api/funcionarios", "/api/funcionarios/1", "/api/funcionarios/email/ana%40clyvovet.com",
        "/api/funcionarios/setor/Clinica", "/api/funcionarios/cargo/Veterinaria",
        "/api/medicamentos", "/api/medicamentos/1", "/api/medicamentos/nome/Amoxi",
        "/api/consultamedicamentos", "/api/consultamedicamentos/1", "/api/consultamedicamentos/consulta/1",
        "/api/pets/1/evolucoes", "/api/pets/1/evolucoes/00000000000000000000000000000001"
    };

    public static TheoryData<string> CollectionRoutes => new()
    {
        "/api/tutores", "/api/pets", "/api/consultas", "/api/vacinas",
        "/api/funcionarios", "/api/medicamentos", "/api/consultamedicamentos"
    };

    [Theory]
    [MemberData(nameof(ClinicalGetRoutes))]
    public async Task ClinicalGet_WhenAnonymous_ReturnsUnauthorizedProblem(string route)
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
    }

    [Theory]
    [MemberData(nameof(ClinicalGetRoutes))]
    public async Task ClinicalGet_WhenTokenLacksClinicalScope_ReturnsForbidden(string route)
    {
        var client = ClientWithoutClinicalScope();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal(403, problem.Status);
    }

    [Theory]
    [MemberData(nameof(CollectionRoutes))]
    public async Task ClinicalPost_WhenTokenLacksClinicalScope_ReturnsForbidden(string route)
    {
        var client = ClientWithoutClinicalScope();

        var response = await client.PostAsJsonAsync(route, new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(CollectionRoutes))]
    public async Task List_WhenSortIsUnknown_ReturnsBadRequestProblem(string route)
    {
        var client = await fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"{route}?sortBy=nome;DROP-TABLE");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Contains("permitidos", problem.Detail);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("page=1000001")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("descending=invalid")]
    public async Task List_WhenPaginationOrSortDirectionIsInvalid_ReturnsBadRequest(string query)
    {
        var client = await fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/pets?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(CollectionRoutes))]
    public async Task List_WhenSearchHasNoMatches_ReturnsEmptyFilteredPage(string route)
    {
        fixture.ResetDatabase();
        var client = await fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"{route}?search=registro-inexistente&sortBy=id");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponseDto<PetResponseDto>>();
        Assert.NotNull(page);
        Assert.Empty(page.Data);
        Assert.Equal(0, page.TotalItems);
        Assert.Equal(0, page.TotalPages);
        Assert.DoesNotContain(page.Links, l => l.Rel is "next" or "prev");
        Assert.Contains(page.Links, l => l.Rel == "first" && l.Href.Contains("page=1"));
    }

    [Fact]
    public async Task Pets_WhenFilteredAndSorted_PaginatesAndFollowsHateoasPreservingQuery()
    {
        var seed = fixture.ResetDatabase();
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Pets.AddRange(
                NewPet("Agata", "Gato", seed.DonoId),
                NewPet("Zeca", "Gato", seed.DonoId),
                NewPet("Bidu", "Cachorro", seed.DonoId));
            await db.SaveChangesAsync();
        }
        var client = await fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/pets?search=Gato&sortBy=nome&descending=true&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var first = await response.Content.ReadFromJsonAsync<PagedResponseDto<PetResponseDto>>();
        Assert.NotNull(first);
        Assert.Equal(2, first.TotalItems);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal("Zeca", Assert.Single(first.Data).Nome);
        var next = Assert.Single(first.Links, l => l.Rel == "next");
        Assert.Contains("search=Gato", next.Href);
        Assert.Contains("sortBy=nome", next.Href);
        Assert.Contains("descending=true", next.Href);
        var second = await client.GetFromJsonAsync<PagedResponseDto<PetResponseDto>>(next.Href);
        Assert.NotNull(second);
        Assert.Equal("Agata", Assert.Single(second.Data).Nome);
        Assert.DoesNotContain(second.Links, l => l.Rel == "next");
        Assert.Contains(second.Links, l => l.Rel == "prev");
        foreach (var link in first.Data.Single().Links)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(link.Href)).StatusCode);
    }

    [Fact]
    public async Task Pets_WhenSortKeysTie_UsesIdAsStableTieBreaker()
    {
        var seed = fixture.ResetDatabase();
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Pets.AddRange(NewPet("Mesmo nome", "Gato", seed.DonoId), NewPet("Mesmo nome", "Gato", seed.DonoId));
            await db.SaveChangesAsync();
        }
        var client = await fixture.CreateAuthenticatedClientAsync();

        var page = await client.GetFromJsonAsync<PagedResponseDto<PetResponseDto>>("/api/pets?search=Mesmo%20nome&sortBy=nome&descending=true");

        Assert.NotNull(page);
        Assert.Equal(2, page.TotalItems);
        Assert.Equal(page.Data.Select(p => p.Id).OrderBy(id => id), page.Data.Select(p => p.Id));
    }

    [Fact]
    public async Task PetDetailsAndFilteredArray_ExposeNavigableResourceLinksWithoutChangingShape()
    {
        var seed = fixture.ResetDatabase();
        var client = await fixture.CreateAuthenticatedClientAsync();

        var detail = await client.GetFromJsonAsync<PetDetailsResponseDto>($"/api/pets/{seed.PetId}");
        var response = await client.GetAsync("/api/pets/especie/Cachorro");

        Assert.NotNull(detail);
        Assert.Contains(detail.Links, l => l.Rel == "self" && l.Href == $"/api/pets/{seed.PetId}");
        Assert.NotEmpty(detail.Consultas.Single().Links);
        Assert.Equal("Dra. Ana", detail.Consultas.Single().FuncionarioNome);
        Assert.NotEmpty(detail.Vacinas.Single().Links);
        Assert.Contains("self", response.Headers.GetValues("Link").Single());
        var items = await response.Content.ReadFromJsonAsync<List<PetResponseDto>>();
        Assert.NotNull(items);
        var self = Assert.Single(items.Single().Links, l => l.Rel == "self");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(self.Href)).StatusCode);
    }

    [Theory]
    [InlineData("/api/consultas/periodo")]
    [InlineData("/api/consultas/periodo?inicio=2026-01-01")]
    [InlineData("/api/vacinas/proximas?dias=2147483647")]
    public async Task SpecializedQuery_WhenRequiredOrBoundedParameterIsInvalid_ReturnsBadRequest(string route)
    {
        var client = await fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPet_WhenDateIsMissingOrOwnerIdIsZero_ReturnsValidationProblem()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/pets", new { nome = "Bidu", especie = "Cachorro", raca = "SRD", peso = 5m, tutorId = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("errors", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OpenApi_WhenRequested_DescribesBearerQueriesAndHateoas()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var scheme = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
        var paths = root.GetProperty("paths").EnumerateObject().ToList();
        foreach (var route in CollectionRoutes)
        {
            var path = Assert.Single(paths, p => string.Equals(p.Name, route, StringComparison.OrdinalIgnoreCase));
            var get = path.Value.GetProperty("get");
            Assert.Contains(get.GetProperty("security").EnumerateArray(), s => s.TryGetProperty("Bearer", out _));
            Assert.True(get.GetProperty("responses").TryGetProperty("401", out _));
            Assert.True(get.GetProperty("responses").TryGetProperty("403", out _));
            var names = get.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToList();
            foreach (var parameter in new[] { "page", "pageSize", "search", "sortBy", "descending" })
                Assert.Contains(names, n => string.Equals(n, parameter, StringComparison.OrdinalIgnoreCase));
        }
        var login = Assert.Single(paths, p => p.Name == "/api/auth/login").Value.GetProperty("post");
        Assert.False(login.TryGetProperty("security", out var security) && security.GetArrayLength() > 0);
        Assert.Contains(root.GetProperty("components").GetProperty("schemas").EnumerateObject(),
            schema => schema.Name == "PetResponseDto" && schema.Value.GetProperty("properties").TryGetProperty("links", out _));
    }

    [Fact]
    public async Task DonoByEmail_WhenOwnerHasPets_ReturnsCompleteHistoryAndNavigableLinks()
    {
        fixture.ResetDatabase();
        var client = await fixture.CreateAuthenticatedClientAsync();

        var detail = await client.GetFromJsonAsync<DonoDetailsResponseDto>("/api/tutores/email/tutor%40clyvovet.com");

        Assert.NotNull(detail);
        Assert.Equal("Rex", Assert.Single(detail.Pets).Nome);
        foreach (var link in detail.Links.Concat(detail.Pets.Single().Links))
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(link.Href)).StatusCode);
    }

    [Theory]
    [MemberData(nameof(CollectionRoutes))]
    public async Task Collections_WhenRecordsExist_ExposeNavigableLinksForEveryResource(string route)
    {
        var seed = fixture.ResetDatabase();
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ConsultasMedicamentos.Add(new ConsultaMedicamento
            {
                ConsultaId = seed.ConsultaId, MedicamentoId = seed.MedicamentoId, Dosagem = "5 mg"
            });
            await db.SaveChangesAsync();
        }
        var client = await fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var record = Assert.Single(document.RootElement.GetProperty("data").EnumerateArray());
        var links = record.GetProperty("links").EnumerateArray().ToList();
        Assert.Contains(links, l => l.GetProperty("rel").GetString() == "self");
        foreach (var link in links)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(link.GetProperty("href").GetString())).StatusCode);
    }

    private HttpClient ClientWithoutClinicalScope()
    {
        var token = new JwtSecurityToken(
            issuer: ClyvoVetWebApplicationFactory.Issuer,
            audience: ClyvoVetWebApplicationFactory.Audience,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ClyvoVetWebApplicationFactory.SigningKey)),
                SecurityAlgorithms.HmacSha256));
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    private static Pet NewPet(string name, string species, int ownerId) => new()
    {
        Nome = name, Especie = species, Raca = "SRD", DataNascimento = new DateTime(2020, 1, 1), Peso = 4m, DonoId = ownerId
    };
}
