using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Tests.Unit.Domain;

public class EvolucaoClinicaTests
{
    [Fact]
    public void Constructor_WhenValid_NormalizesTextAndTagsWithoutExposingMutableState()
    {
        var tags = new[] { " rotina ", "ROTINA", "retorno" };

        var evolucao = Create(texto: "  Animal ativo.  ", autor: "  Dra. Ana  ", tags: tags);
        tags[0] = "alterada";

        Assert.Equal("Animal ativo.", evolucao.Texto);
        Assert.Equal("Dra. Ana", evolucao.Autor);
        Assert.Equal(["rotina", "retorno"], evolucao.Tags);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenPetIsInvalid_RejectsEvolution(int petId)
    {
        Assert.Throws<BusinessException>(() => Create(petId: petId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-guid")]
    public void Constructor_WhenIdIsInvalid_RejectsEvolution(string id)
    {
        Assert.Throws<BusinessException>(() => Create(id: id));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WhenTextIsBlank_RejectsEvolution(string texto)
    {
        Assert.Throws<BusinessException>(() => Create(texto: texto));
    }

    [Fact]
    public void Constructor_WhenTextExceedsLimit_RejectsEvolution()
    {
        Assert.Throws<BusinessException>(() => Create(texto: new string('a', 4001)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WhenAuthorIsBlank_RejectsEvolution(string autor)
    {
        Assert.Throws<BusinessException>(() => Create(autor: autor));
    }

    [Fact]
    public void Constructor_WhenAuthorExceedsLimit_RejectsEvolution()
    {
        Assert.Throws<BusinessException>(() => Create(autor: new string('a', 101)));
    }

    [Fact]
    public void Constructor_WhenDateIsNotUtc_RejectsEvolution()
    {
        Assert.Throws<BusinessException>(() => Create(data: DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WhenTagIsBlank_RejectsEvolution(string tag)
    {
        Assert.Throws<BusinessException>(() => Create(tags: [tag]));
    }

    [Fact]
    public void Constructor_WhenTagExceedsLimit_RejectsEvolution()
    {
        Assert.Throws<BusinessException>(() => Create(tags: [new string('a', 41)]));
    }

    [Fact]
    public void Constructor_WhenTooManyDistinctTags_RejectsEvolution()
    {
        var tags = Enumerable.Range(1, 11).Select(x => $"tag-{x}");

        Assert.Throws<BusinessException>(() => Create(tags: tags));
    }

    private static EvolucaoClinica Create(int petId = 1, string? id = null, string texto = "Animal ativo.",
        string autor = "Dra. Ana", DateTime? data = null, IEnumerable<string>? tags = null) =>
        new(id ?? Guid.NewGuid().ToString("N"), petId, texto, autor, data ?? DateTime.UtcNow, tags);
}
