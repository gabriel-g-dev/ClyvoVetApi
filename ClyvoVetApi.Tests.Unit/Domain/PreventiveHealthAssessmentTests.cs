using ClyvoVetApi.Models;
using ClyvoVetApi.Prevention;

namespace ClyvoVetApi.Tests.Unit.Domain;

public class PreventiveHealthAssessmentTests
{
    private static readonly DateTime Today = new(2026, 10, 25);

    [Theory]
    [InlineData(-1, "P", 85)]
    [InlineData(0, "P", 95)]
    [InlineData(7, "P", 95)]
    [InlineData(8, "P", 100)]
    [InlineData(-100, "A", 100)]
    public void Evaluate_VaccineDueDateAndStatus_RespectsPreventiveWindow(int days, string status, int expectedScore)
    {
        var pet = new Pet
        {
            Consultas = [new Consulta { Status = "R", Data = Today }],
            Vacinas = [new Vacina { Status = status, Data = Today.AddDays(days) }]
        };

        var result = PreventiveHealthAssessment.Evaluate(pet, Today);

        Assert.Equal(expectedScore, result.Score);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-1, 90)]
    public void Evaluate_LastCompletedAppointment_UsesSixMonthBoundary(int daysFromBoundary, int expectedScore)
    {
        var pet = new Pet
        {
            Consultas = [new Consulta { Status = "R", Data = Today.AddMonths(-6).AddDays(daysFromBoundary) }]
        };

        var result = PreventiveHealthAssessment.Evaluate(pet, Today);

        Assert.Equal(expectedScore, result.Score);
    }

    [Fact]
    public void Evaluate_CancelledAndScheduledAppointments_DoNotCountAsCompleted()
    {
        var pet = new Pet
        {
            Consultas = [new Consulta { Status = "C", Data = Today }, new Consulta { Status = "A", Data = Today }]
        };

        var result = PreventiveHealthAssessment.Evaluate(pet, Today);

        Assert.Equal(80, result.Score);
        Assert.Contains(result.Recomendacoes, recommendation => recommendation.Contains("Nenhuma consulta foi realizada"));
    }

    [Fact]
    public void Evaluate_ManyOverdueVaccines_ClampsScoreAndRecommendsCare()
    {
        var pet = new Pet
        {
            Vacinas = Enumerable.Range(1, 10).Select(_ => new Vacina { Status = "P", Data = Today.AddDays(-1) }).ToList()
        };

        var result = PreventiveHealthAssessment.Evaluate(pet, Today);

        Assert.Equal(0, result.Score);
        Assert.Equal("CRÍTICO", result.Status);
        Assert.Contains(result.Recomendacoes, recommendation => recommendation.Contains("veterinário de confiança"));
    }
}
