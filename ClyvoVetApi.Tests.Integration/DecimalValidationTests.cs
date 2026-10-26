using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClyvoVetApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVetApi.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class DecimalValidationTests(IntegrationTestFixture fixture)
{
    [Theory]
    [InlineData("pets", "POST", "Peso")]
    [InlineData("pets", "PUT", "Peso")]
    [InlineData("consultas", "POST", "Valor")]
    [InlineData("consultas", "PUT", "Valor")]
    public async Task Write_WhenValueWouldBeRounded_ReturnsValidationProblemAndPreservesData(
        string resource, string method, string field)
    {
        // Arrange
        var seed = fixture.ResetDatabase();
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var id = resource == "pets" ? seed.PetId : seed.ConsultaId;
        var route = $"/api/{resource}" + (method == "PUT" ? $"/{id}" : "");
        using var request = new HttpRequestMessage(new HttpMethod(method), route)
        {
            Content = JsonContent.Create(Payload(resource, seed, 12.345m))
        };

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(document.RootElement.GetProperty("errors").EnumerateObject(),
            error => string.Equals(error.Name, field, StringComparison.OrdinalIgnoreCase));
        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await context.Pets.CountAsync());
        Assert.Equal(1, await context.Consultas.CountAsync());
        Assert.Equal(25.50m, await context.Pets.Select(pet => pet.Peso).SingleAsync());
        Assert.Equal(150m, await context.Consultas.Select(consulta => consulta.Valor).SingleAsync());
    }

    [Theory]
    [InlineData("pets", "POST", "peso")]
    [InlineData("pets", "PUT", "peso")]
    [InlineData("consultas", "POST", "valor")]
    [InlineData("consultas", "PUT", "valor")]
    public async Task Write_WhenExtraDecimalPlacesAreOnlyZeros_PersistsTheExactValue(
        string resource, string method, string field)
    {
        // Arrange
        var seed = fixture.ResetDatabase();
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var id = resource == "pets" ? seed.PetId : seed.ConsultaId;
        var route = $"/api/{resource}" + (method == "PUT" ? $"/{id}" : "");
        using var request = new HttpRequestMessage(new HttpMethod(method), route)
        {
            Content = JsonContent.Create(Payload(resource, seed, 12.3400m))
        };

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(method == "POST" ? HttpStatusCode.Created : HttpStatusCode.OK, response.StatusCode);
        using var written = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(12.34m, written.RootElement.GetProperty(field).GetDecimal());
        var resourceId = written.RootElement.GetProperty("id").GetInt32();
        using var fetched = JsonDocument.Parse(await client.GetStringAsync($"/api/{resource}/{resourceId}"));
        Assert.Equal(12.34m, fetched.RootElement.GetProperty(field).GetDecimal());
    }

    private static object Payload(string resource, SeedData seed, decimal value) => resource == "pets"
        ? new { nome = "Pet decimal", especie = "Canina", raca = "SRD", dataNascimento = "2020-01-01", peso = value, tutorId = seed.DonoId }
        : new { tipo = "Rotina", descricao = "Consulta decimal", data = "2026-01-01", valor = value, status = "A", petId = seed.PetId, funcionarioId = seed.FuncionarioId };
}
