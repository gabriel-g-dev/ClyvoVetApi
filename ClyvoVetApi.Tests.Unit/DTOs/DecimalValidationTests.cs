using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ClyvoVetApi.DTOs.Request;

namespace ClyvoVetApi.Tests.Unit.DTOs;

public class DecimalValidationTests
{
    [Theory]
    [InlineData("0.01", true)]
    [InlineData("999.99", true)]
    [InlineData("12.3400", true)]
    [InlineData("12.345", false)]
    [InlineData("0.011", false)]
    [InlineData("999.989", false)]
    public void PetRequest_ValidatesWeightWithoutSilentRounding(string value, bool expected)
    {
        // Arrange
        var request = new PetRequestDto
        {
            Nome = "Rex", Especie = "Canina", Raca = "SRD", DataNascimento = new DateTime(2020, 1, 1),
            Peso = decimal.Parse(value, CultureInfo.InvariantCulture), DonoId = 1
        };
        var errors = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(request, new ValidationContext(request), errors, true);

        // Assert
        Assert.Equal(expected, valid);
        if (!expected)
            Assert.Contains(errors, error => error.MemberNames.Contains(nameof(PetRequestDto.Peso)));
    }

    [Theory]
    [InlineData("0.00", true)]
    [InlineData("99999999.99", true)]
    [InlineData("100.2500", true)]
    [InlineData("100.255", false)]
    [InlineData("0.001", false)]
    [InlineData("99999999.989", false)]
    public void ConsultaRequest_ValidatesAmountWithoutSilentRounding(string value, bool expected)
    {
        // Arrange
        var request = new ConsultaRequestDto
        {
            Tipo = "Rotina", Descricao = "Consulta", Data = new DateTime(2026, 1, 1),
            Valor = decimal.Parse(value, CultureInfo.InvariantCulture), Status = "A", PetId = 1, FuncionarioId = 1
        };
        var errors = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(request, new ValidationContext(request), errors, true);

        // Assert
        Assert.Equal(expected, valid);
        if (!expected)
            Assert.Contains(errors, error => error.MemberNames.Contains(nameof(ConsultaRequestDto.Valor)));
    }
}
