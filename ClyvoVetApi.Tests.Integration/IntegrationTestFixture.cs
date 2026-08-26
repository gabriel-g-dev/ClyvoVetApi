using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClyvoVetApi.Data;
using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVetApi.Tests.Integration;

public record SeedData(int DonoId, int FuncionarioId, int PetId, int ConsultaId, int VacinaId, int MedicamentoId);

public class IntegrationTestFixture : IDisposable
{
    public ClyvoVetWebApplicationFactory Factory { get; } = new();

    public HttpClient CreateClient() => Factory.CreateClient();

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = Factory.CreateClient();
        var token = await GetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<string> GetAccessTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = ClyvoVetWebApplicationFactory.Username,
            Password = ClyvoVetWebApplicationFactory.Password
        });

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return content!.AccessToken;
    }

    public SeedData ResetDatabase()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var dono = new Dono { Nome = "Tutor Teste", Email = "tutor@clyvovet.com", Telefone = "11999999999" };
        var funcionario = new Funcionario
        {
            Nome = "Dra. Ana",
            Setor = "Clínica Geral",
            Cargo = "Veterinária",
            Email = "ana@clyvovet.com",
            Telefone = "11988888888"
        };
        var medicamento = new Medicamento { Nome = "Amoxicilina" };

        context.AddRange(dono, funcionario, medicamento);
        context.SaveChanges();

        var pet = new Pet
        {
            Nome = "Rex",
            Especie = "Cachorro",
            Raca = "Labrador",
            DataNascimento = new DateTime(2020, 1, 15),
            Peso = 25.50m,
            DonoId = dono.Id
        };
        context.Add(pet);
        context.SaveChanges();

        var consulta = new Consulta
        {
            Tipo = "Consulta de Rotina",
            Valor = 150.00m,
            Descricao = "Checkup anual",
            Status = "A",
            Data = DateTime.Today.AddDays(3),
            PetId = pet.Id,
            FuncionarioId = funcionario.Id
        };
        var vacina = new Vacina
        {
            Nome = "Antirrábica",
            Data = DateTime.Today.AddDays(10),
            Status = "P",
            PetId = pet.Id
        };

        context.AddRange(consulta, vacina);
        context.SaveChanges();

        return new SeedData(dono.Id, funcionario.Id, pet.Id, consulta.Id, vacina.Id, medicamento.Id);
    }

    public void Dispose()
    {
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
