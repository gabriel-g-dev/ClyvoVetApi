using ClyvoVetApi.Logging;
using Xunit;

namespace ClyvoVetApi.Tests.Unit.Logging;

public class SensitiveDataMaskerTests
{
    [Theory]
    [InlineData("tutor@clyvovet.com", "t***@***.com")]
    [InlineData("ana.silva@empresa.com.br", "a***@***.br")]
    [InlineData("contato+tag@dominio.org", "c***@***.org")]
    public void MaskEmails_WhenTextIsAnEmail_KeepsOnlyFirstLetterAndTopLevelDomain(string entrada, string esperado)
    {
        var resultado = SensitiveDataMasker.MaskEmails(entrada);

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void MaskEmails_WhenEmailIsInsideSentence_PreservesSurroundingText()
    {
        var resultado = SensitiveDataMasker.MaskEmails("GET /api/tutores/email/tutor@clyvovet.com");

        Assert.Equal("GET /api/tutores/email/t***@***.com", resultado);
    }

    [Fact]
    public void MaskEmails_WhenTextHasSeveralEmails_MasksEveryOne()
    {
        var resultado = SensitiveDataMasker.MaskEmails("de ana@clinica.com para joao@tutor.com");

        Assert.Equal("de a***@***.com para j***@***.com", resultado);
        Assert.DoesNotContain("ana@clinica.com", resultado);
        Assert.DoesNotContain("joao@tutor.com", resultado);
    }

    [Fact]
    public void MaskEmails_WhenTextHasNoEmail_ReturnsTextUnchanged()
    {
        const string entrada = "GET /api/pets/42 concluida com 200";

        var resultado = SensitiveDataMasker.MaskEmails(entrada);

        Assert.Equal(entrada, resultado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MaskEmails_WhenTextIsNullOrEmpty_ReturnsEmptyString(string? entrada)
    {
        var resultado = SensitiveDataMasker.MaskEmails(entrada);

        Assert.Equal(string.Empty, resultado);
    }
}
