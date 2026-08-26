using ClyvoVetApi.Models;

namespace ClyvoVetApi.Tests.Unit.Fixtures;

public class TestDataFixture
{
    public const int DonoPadraoId = 1;
    public const int FuncionarioPadraoId = 2;
    public const int PetPadraoId = 1;

    public Dono NovoDono(
        int id = DonoPadraoId,
        string nome = "Dono 1",
        string email = "dono1@email.com",
        string telefone = "11999999999") => new()
        {
            Id = id,
            Nome = nome,
            Email = email,
            Telefone = telefone
        };

    public Funcionario NovoFuncionario(
        int id = FuncionarioPadraoId,
        string nome = "Dra. Ana",
        string setor = "Clínica Geral",
        string cargo = "Veterinária") => new()
        {
            Id = id,
            Nome = nome,
            Setor = setor,
            Cargo = cargo,
            Email = "ana@clyvovet.com",
            Telefone = "11988888888"
        };

    public Pet NovoPet(
        int id = PetPadraoId,
        string nome = "Rex",
        string especie = "Cachorro",
        string raca = "Vira-lata",
        decimal peso = 10.5m,
        int donoId = DonoPadraoId,
        Dono? dono = null,
        ICollection<Consulta>? consultas = null,
        ICollection<Vacina>? vacinas = null) => new()
        {
            Id = id,
            Nome = nome,
            Especie = especie,
            Raca = raca,
            DataNascimento = new DateTime(2020, 1, 15),
            Peso = peso,
            DonoId = donoId,
            Dono = dono,
            Consultas = consultas ?? [],
            Vacinas = vacinas ?? []
        };

    public Consulta NovaConsulta(
        int id = 3,
        string tipo = "Geral",
        decimal valor = 150.00m,
        string descricao = "Rotina",
        string status = "R",
        DateTime? data = null,
        int petId = PetPadraoId,
        Pet? pet = null,
        int funcionarioId = FuncionarioPadraoId,
        Funcionario? funcionario = null) => new()
        {
            Id = id,
            Tipo = tipo,
            Valor = valor,
            Descricao = descricao,
            Status = status,
            Data = data ?? DateTime.Today,
            PetId = petId,
            Pet = pet,
            FuncionarioId = funcionarioId,
            Funcionario = funcionario
        };

    public Vacina NovaVacina(
        int id = 4,
        string nome = "Antirrábica",
        string status = "A",
        DateTime? data = null,
        int petId = PetPadraoId,
        Pet? pet = null) => new()
        {
            Id = id,
            Nome = nome,
            Status = status,
            Data = data ?? DateTime.Today,
            PetId = petId,
            Pet = pet
        };

    public Medicamento NovoMedicamento(int id = 5, string nome = "Amoxicilina") => new()
    {
        Id = id,
        Nome = nome
    };
}
