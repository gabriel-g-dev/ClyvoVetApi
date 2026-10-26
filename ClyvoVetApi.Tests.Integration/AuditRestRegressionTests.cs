using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClyvoVetApi.DTOs.Response;

namespace ClyvoVetApi.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class AuditRestRegressionTests(IntegrationTestFixture fixture)
{
    public static TheoryData<string, string> SortFields => new()
    {
        { "tutores", "id" }, { "tutores", "nome" }, { "tutores", "email" },
        { "funcionarios", "id" }, { "funcionarios", "nome" }, { "funcionarios", "email" },
        { "funcionarios", "setor" }, { "funcionarios", "cargo" },
        { "pets", "id" }, { "pets", "nome" }, { "pets", "especie" },
        { "pets", "raca" }, { "pets", "datanascimento" },
        { "consultas", "id" }, { "consultas", "data" }, { "consultas", "tipo" }, { "consultas", "status" },
        { "vacinas", "id" }, { "vacinas", "nome" }, { "vacinas", "data" }, { "vacinas", "status" },
        { "medicamentos", "id" }, { "medicamentos", "nome" },
        { "consultamedicamentos", "id" }, { "consultamedicamentos", "consultaid" },
        { "consultamedicamentos", "medicamentoid" }, { "consultamedicamentos", "dosagem" }
    };

    public static TheoryData<string, string> SearchFields => new()
    {
        { "tutores", "nome" }, { "tutores", "email" },
        { "funcionarios", "nome" }, { "funcionarios", "email" },
        { "funcionarios", "setor" }, { "funcionarios", "cargo" },
        { "pets", "nome" }, { "pets", "especie" }, { "pets", "raca" },
        { "consultas", "tipo" }, { "consultas", "descricao" },
        { "vacinas", "nome" }, { "medicamentos", "nome" },
        { "consultamedicamentos", "dosagem" }, { "consultamedicamentos", "medicamentoNome" }
    };

    [Theory]
    [InlineData("tutores", "nome")]
    [InlineData("funcionarios", "nome")]
    [InlineData("medicamentos", "nome")]
    [InlineData("vacinas", "nome")]
    [InlineData("consultas", "tipo")]
    [InlineData("consultamedicamentos", "dosagem")]
    public async Task Crud_WhenPayloadsAreValid_PersistsUpdatesAndDeletesThroughLocation(
        string resource, string changedField)
    {
        var seed = fixture.ResetDatabase();
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var payload = Payload(resource, seed, 1, "Audit A");

        using var created = await client.PostAsJsonAsync($"/api/{resource}", payload);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var createdBody = await ObjectAsync(created);
        var id = Field(createdBody, "id").GetInt32();
        var location = created.Headers.Location;
        using var read = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var readBody = await ObjectAsync(read);
        Assert.Equal(id, Field(readBody, "id").GetInt32());
        Assert.Equal("Audit A", Field(readBody, changedField).GetString());

        payload[changedField] = "Audit updated";
        using var updated = await client.PutAsJsonAsync(location, payload);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Audit updated", Field(await ObjectAsync(updated), changedField).GetString());
        using var persisted = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, persisted.StatusCode);
        var persistedBody = await ObjectAsync(persisted);
        Assert.Equal("Audit updated", Field(persistedBody, changedField).GetString());
        var self = Field(persistedBody, "links").EnumerateArray()
            .Single(link => Field(link, "rel").GetString() == "self");
        using var linkedRead = await client.GetAsync(Field(self, "href").GetString());
        Assert.Equal(HttpStatusCode.OK, linkedRead.StatusCode);
        Assert.Equal(id, Field(await ObjectAsync(linkedRead), "id").GetInt32());

        using var deleted = await client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var missing = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [MemberData(nameof(SortFields))]
    public async Task Collections_WhenFilteredAndSorted_UseRequestedFieldAndNavigateBothDirections(
        string resource, string sortBy)
    {
        var seed = fixture.ResetDatabase();
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var firstPayload = Payload(resource, seed, 2, "Audit B");
        var secondPayload = Payload(resource, seed, 1, "Audit A");
        if (resource == "consultamedicamentos")
        {
            var secondConsulta = await CreateAsync(client, "consultas", Payload("consultas", seed, 2, "Support"));
            var secondMedicamento = await CreateAsync(client, "medicamentos", Payload("medicamentos", seed, 2, "Support"));
            firstPayload["consultaId"] = Field(secondConsulta, "id").GetInt32();
            firstPayload["medicamentoId"] = Field(secondMedicamento, "id").GetInt32();
        }
        var records = new[]
        {
            await CreateAsync(client, resource, firstPayload),
            await CreateAsync(client, resource, secondPayload)
        };
        await CreateAsync(client, resource, Payload(resource, seed, 3, "Other"));
        var ascending = records.OrderBy(row => row, Comparer<JsonElement>.Create((left, right) =>
        {
            var comparison = Compare(Field(left, sortBy), Field(right, sortBy));
            return comparison != 0 ? comparison : Field(left, "id").GetInt32().CompareTo(Field(right, "id").GetInt32());
        })).ToArray();

        foreach (var descending in new[] { false, true })
        {
            var route = $"/api/{resource}?search=aUdIt&sortBy={sortBy}&descending={descending.ToString().ToLowerInvariant()}&pageSize=1";
            var expected = descending ? ascending.Reverse().ToArray() : ascending;
            using var firstResponse = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            var first = await ObjectAsync(firstResponse);
            Assert.Equal(2, Field(first, "totalItems").GetInt32());
            Assert.Equal(2, Field(first, "totalPages").GetInt32());
            var firstRecord = Assert.Single(Field(first, "data").EnumerateArray());
            Assert.Equal(Field(expected[0], "id").GetInt32(), Field(firstRecord, "id").GetInt32());
            var next = Field(first, "links").EnumerateArray().Single(link => Field(link, "rel").GetString() == "next");
            var nextUrl = Field(next, "href").GetString();
            Assert.Contains("search=aUdIt", nextUrl);
            Assert.Contains($"sortBy={sortBy}", nextUrl);
            using var nextResponse = await client.GetAsync(nextUrl);
            Assert.Equal(HttpStatusCode.OK, nextResponse.StatusCode);
            var second = await ObjectAsync(nextResponse);
            Assert.Equal(2, Field(second, "page").GetInt32());
            var secondRecord = Assert.Single(Field(second, "data").EnumerateArray());
            Assert.Equal(Field(expected[1], "id").GetInt32(), Field(secondRecord, "id").GetInt32());
            Assert.DoesNotContain(Field(second, "links").EnumerateArray(), link => Field(link, "rel").GetString() == "next");
        }
    }

    [Theory]
    [MemberData(nameof(SearchFields))]
    public async Task Search_WhenOnlyOneSupportedFieldMatches_FiltersBeforeCounting(string resource, string field)
    {
        var seed = fixture.ResetDatabase();
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var payload = Payload(resource, seed, 1, "Other");
        if (field == "medicamentoNome")
        {
            var medication = await CreateAsync(client, "medicamentos", Payload("medicamentos", seed, 1, "Auditneedle"));
            payload["medicamentoId"] = Field(medication, "id").GetInt32();
        }
        else
        {
            payload[field] = field == "email" ? "Auditneedle@example.test" : "Auditneedle";
        }
        var created = await CreateAsync(client, resource, payload);
        await CreateAsync(client, resource, Payload(resource, seed, 2, "Unrelated"));

        using var response = await client.GetAsync($"/api/{resource}?search=aUdItNeEdLe&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ObjectAsync(response);
        Assert.Equal(1, Field(page, "totalItems").GetInt32());
        Assert.Equal(1, Field(page, "totalPages").GetInt32());
        var item = Assert.Single(Field(page, "data").EnumerateArray());
        Assert.Equal(Field(created, "id").GetInt32(), Field(item, "id").GetInt32());
    }

    [Theory]
    [InlineData("prefixed")]
    [InlineData("tag-only")]
    [InlineData("flat")]
    public async Task Evolucoes_WhenPaginationUsesQueryPrefix_NextLinkAdvancesToNextDocument(string binding)
    {
        var seed = fixture.ResetDatabase();
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var tag = $"audit-{Guid.NewGuid():N}";
        var pageSize = binding == "tag-only" ? 10 : 1;
        var total = pageSize + 1;
        for (var index = 0; index < total; index++)
            await CreateAsync(client, $"pets/{seed.PetId}/evolucoes", new() { ["texto"] = $"Record {index}", ["tags"] = new[] { tag } });
        var prefix = binding == "flat" ? string.Empty : "query.";
        var query = binding == "tag-only"
            ? $"query.tag={tag}"
            : $"{prefix}page=1&{prefix}pageSize=1&{prefix}direction=asc&{prefix}tag={tag}";

        var first = await client.GetFromJsonAsync<PagedResponseDto<EvolucaoClinicaResponseDto>>(
            $"/api/pets/{seed.PetId}/evolucoes?{query}");

        Assert.NotNull(first);
        Assert.Equal(1, first.Page);
        Assert.Equal(pageSize, first.PageSize);
        Assert.Equal(total, first.TotalItems);
        var firstIds = first.Data.Select(document => document.Id).ToArray();
        Assert.Equal(pageSize, firstIds.Length);
        var next = Assert.Single(first.Links, link => link.Rel == "next");
        var second = await client.GetFromJsonAsync<PagedResponseDto<EvolucaoClinicaResponseDto>>(next.Href);
        Assert.NotNull(second);
        Assert.Equal(2, second.Page);
        Assert.Equal(pageSize, second.PageSize);
        Assert.Equal(total, second.TotalItems);
        Assert.DoesNotContain(Assert.Single(second.Data).Id, firstIds);
    }

    [Fact]
    public async Task OpenApi_WhenQueryTypesHaveDifferentRules_DoesNotShareUnrelatedStringConstraints()
    {
        using var client = fixture.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paths = Field(await ObjectAsync(response), "paths");

        foreach (var resource in new[] { "tutores", "funcionarios", "pets", "consultas", "vacinas", "medicamentos", "consultamedicamentos" })
        {
            var operation = Field(Field(paths, $"/api/{resource}"), "get");
            foreach (var name in new[] { "search", "sortBy" })
            {
                var parameter = Field(operation, "parameters").EnumerateArray()
                    .Single(item => string.Equals(Field(item, "name").GetString(), name, StringComparison.OrdinalIgnoreCase));
                var schema = Field(parameter, "schema");
                Assert.False(schema.TryGetProperty("pattern", out _), $"{resource}.{name} must accept values other than asc/desc.");
                Assert.Equal(name == "search" ? 100 : 30, Field(schema, "maxLength").GetInt32());
            }
        }
        var evolucoes = Field(Field(paths, "/api/pets/{petId}/evolucoes"), "get");
        var direction = Field(evolucoes, "parameters").EnumerateArray()
            .Single(item => string.Equals(Field(item, "name").GetString(), "direction", StringComparison.OrdinalIgnoreCase));
        var directionSchema = Field(direction, "schema");
        Assert.Equal("^(asc|desc)$", Field(directionSchema, "pattern").GetString());
        Assert.False(directionSchema.TryGetProperty("maxLength", out _), "Direction must not inherit Tag's length constraint.");
    }

    [Fact]
    public async Task OpenApi_WhenClinicalGetCanRejectParameters_DescribesBadRequestAndProblemMediaType()
    {
        using var client = fixture.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paths = Field(await ObjectAsync(response), "paths");
        var routes = new[]
        {
            "/api/tutores", "/api/funcionarios", "/api/pets", "/api/consultas", "/api/vacinas", "/api/medicamentos",
            "/api/consultamedicamentos", "/api/pets/{petId}/evolucoes", "/api/pets/{petId}/evolucoes/{id}"
        };
        foreach (var route in routes)
        {
            var responses = Field(Field(Field(paths, route), "get"), "responses");
            Assert.True(responses.TryGetProperty("400", out _), $"{route} rejects invalid query/path values with 400.");
            foreach (var status in responses.EnumerateObject().Where(item => int.Parse(item.Name) >= 400))
            {
                Assert.True(status.Value.TryGetProperty("content", out var content), $"{route} {status.Name} has a ProblemDetails body.");
                Assert.True(content.TryGetProperty("application/problem+json", out _), $"{route} {status.Name} uses application/problem+json.");
            }
        }
    }

    private static Dictionary<string, object?> Payload(string resource, SeedData seed, int number, string label) => resource switch
    {
        "tutores" => new()
        {
            ["nome"] = label, ["email"] = $"{label.Replace(" ", "-")}@example.test", ["telefone"] = "11999999999"
        },
        "funcionarios" => new()
        {
            ["nome"] = label, ["email"] = $"{label.Replace(" ", "-")}@example.test", ["telefone"] = "11999999999",
            ["setor"] = label, ["cargo"] = label
        },
        "pets" => new()
        {
            ["nome"] = label, ["especie"] = label, ["raca"] = label, ["dataNascimento"] = new DateTime(2020, 1, number),
            ["peso"] = 5m, ["tutorId"] = seed.DonoId
        },
        "consultas" => new()
        {
            ["tipo"] = label, ["descricao"] = label, ["valor"] = 100m, ["status"] = number == 1 ? "A" : "R",
            ["data"] = new DateTime(2030, 1, number), ["petId"] = seed.PetId, ["funcionarioId"] = seed.FuncionarioId
        },
        "vacinas" => new()
        {
            ["nome"] = label, ["data"] = new DateTime(2030, 1, number), ["status"] = number == 1 ? "A" : "P", ["petId"] = seed.PetId
        },
        "medicamentos" => new() { ["nome"] = label },
        "consultamedicamentos" => new()
        {
            ["consultaId"] = seed.ConsultaId, ["medicamentoId"] = seed.MedicamentoId, ["dosagem"] = label
        },
        _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unknown audit resource.")
    };

    private static async Task<JsonElement> CreateAsync(HttpClient client, string resource, Dictionary<string, object?> payload)
    {
        using var response = await client.PostAsJsonAsync($"/api/{resource}", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ObjectAsync(response);
    }

    private static async Task<JsonElement> ObjectAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static JsonElement Field(JsonElement value, string name) => value.EnumerateObject()
        .Single(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static int Compare(JsonElement left, JsonElement right) => left.ValueKind == JsonValueKind.Number
        ? left.GetDecimal().CompareTo(right.GetDecimal())
        : string.Compare(left.GetString(), right.GetString(), StringComparison.Ordinal);
}
