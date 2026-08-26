using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using Moq;
using Xunit;

namespace ClyvoVetApi.Tests.Unit.Services;

public class IntelligenceServiceTests
{
    private readonly Mock<IPetRepository> _petRepositoryMock;
    private readonly IntelligenceService _service;

    public IntelligenceServiceTests()
    {
        _petRepositoryMock = new Mock<IPetRepository>();
        _service = new IntelligenceService(_petRepositoryMock.Object);
    }

    [Fact]
    public async Task GetIntelligencePreventivaAsync_WhenPetDoesNotExist_ThrowsNotFoundException()
    {
        _petRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Pet?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetIntelligencePreventivaAsync(99));

        Assert.Contains("Pet com ID 99 não foi encontrado", exception.Message);
    }

    [Theory]
    [InlineData(0, 100, "EXCELENTE")]
    [InlineData(1, 85, "EXCELENTE")]
    [InlineData(2, 70, "ATENÇÃO")]
    [InlineData(4, 40, "CRÍTICO")]
    public async Task GetIntelligencePreventivaAsync_WithOverdueVaccines_CalculatesScoreAndStatus(
        int vacinasAtrasadas,
        int scoreEsperado,
        string statusEsperado)
    {
        var vacinas = Enumerable.Range(1, vacinasAtrasadas)
            .Select(i => new Vacina { Status = "P", Data = DateTime.Today.AddDays(-i * 10) })
            .ToList();

        var pet = new Pet
        {
            Id = 1,
            Nome = "Bidu",
            Consultas = [new Consulta { Status = "R", Data = DateTime.Today }],
            Vacinas = vacinas
        };
        _petRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        var result = await _service.GetIntelligencePreventivaAsync(1);

        Assert.Equal(scoreEsperado, result.Score);
        Assert.Equal(statusEsperado, result.Status);
    }

    [Fact]
    public async Task GetIntelligencePreventivaAsync_WhenPetIsHealthy_RecommendsKeepingRoutine()
    {
        var pet = new Pet
        {
            Id = 1,
            Nome = "Bidu",
            Consultas = [new Consulta { Status = "R", Data = DateTime.Today }],
            Vacinas = [new Vacina { Status = "A", Data = DateTime.Today.AddMonths(-6) }]
        };
        _petRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        var result = await _service.GetIntelligencePreventivaAsync(1);

        Assert.Equal(100, result.Score);
        Assert.Equal("EXCELENTE", result.Status);
        Assert.Contains(result.Recomendacoes, r => r.Contains("Excelente!"));
    }

    [Fact]
    public async Task GetIntelligencePreventivaAsync_WhenVaccineIsOverdue_WarnsAboutDelay()
    {
        var pet = new Pet
        {
            Id = 1,
            Nome = "Bidu",
            Consultas = [new Consulta { Status = "R", Data = DateTime.Today }],
            Vacinas = [new Vacina { Status = "P", Data = DateTime.Today.AddDays(-5) }]
        };
        _petRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        var result = await _service.GetIntelligencePreventivaAsync(1);

        Assert.Contains(result.Recomendacoes, r => r.Contains("1 vacina(s) em atraso"));
    }

    [Fact]
    public async Task GetIntelligencePreventivaAsync_WhenVaccineIsDueWithinSevenDays_WarnsAboutSchedule()
    {
        var pet = new Pet
        {
            Id = 1,
            Nome = "Bidu",
            Consultas = [new Consulta { Status = "R", Data = DateTime.Today }],
            Vacinas = [new Vacina { Status = "P", Data = DateTime.Today.AddDays(3) }]
        };
        _petRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        var result = await _service.GetIntelligencePreventivaAsync(1);

        Assert.Equal(95, result.Score);
        Assert.Contains(result.Recomendacoes, r => r.Contains("agendada(s) para os próximos dias"));
    }

    [Fact]
    public async Task GetIntelligencePreventivaAsync_WhenPetHasNoConsultations_RecommendsFirstAppointment()
    {
        var pet = new Pet
        {
            Id = 1,
            Nome = "Bidu",
            Consultas = [],
            Vacinas = [new Vacina { Status = "A", Data = DateTime.Today.AddMonths(-6) }]
        };
        _petRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        var result = await _service.GetIntelligencePreventivaAsync(1);

        Assert.Equal(80, result.Score);
        Assert.Contains(result.Recomendacoes, r => r.Contains("Nenhuma consulta preventiva foi registrada"));
    }

    [Fact]
    public async Task GetIntelligencePreventivaAsync_WhenOnlyScheduledConsultationsExist_RecommendsRoutineVisit()
    {
        var pet = new Pet
        {
            Id = 1,
            Nome = "Bidu",
            Consultas = [new Consulta { Status = "A", Data = DateTime.Today.AddDays(5) }],
            Vacinas = []
        };
        _petRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        var result = await _service.GetIntelligencePreventivaAsync(1);

        Assert.Equal(80, result.Score);
        Assert.Contains(result.Recomendacoes, r => r.Contains("Nenhuma consulta foi realizada ainda"));
    }

    [Fact]
    public async Task GetIntelligencePreventivaAsync_WhenLastConsultationIsOlderThanSixMonths_RecommendsCheckup()
    {
        var pet = new Pet
        {
            Id = 1,
            Nome = "Bidu",
            Consultas = [new Consulta { Status = "R", Data = DateTime.Today.AddMonths(-7) }],
            Vacinas = [new Vacina { Status = "A", Data = DateTime.Today.AddMonths(-6) }]
        };
        _petRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        var result = await _service.GetIntelligencePreventivaAsync(1);

        Assert.Equal(90, result.Score);
        Assert.Contains(result.Recomendacoes, r => r.Contains("Já faz mais de 6 meses"));
    }

    [Fact]
    public async Task GetIntelligencePreventivaAsync_WhenPenaltiesExceedLimit_ClampsScoreToZero()
    {
        var vacinas = Enumerable.Range(1, 6)
            .Select(i => new Vacina { Status = "P", Data = DateTime.Today.AddDays(-i * 10) })
            .ToList();

        var pet = new Pet { Id = 1, Nome = "Bidu", Consultas = [], Vacinas = vacinas };
        _petRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        var result = await _service.GetIntelligencePreventivaAsync(1);

        Assert.Equal(0, result.Score);
        Assert.Equal("CRÍTICO", result.Status);
        Assert.Contains(result.Recomendacoes, r => r.Contains("está CRÍTICO"));
    }
}
