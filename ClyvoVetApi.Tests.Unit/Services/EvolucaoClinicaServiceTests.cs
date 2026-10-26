using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using Moq;

namespace ClyvoVetApi.Tests.Unit.Services;

public class EvolucaoClinicaServiceTests
{
    private readonly Mock<IEvolucaoClinicaRepository> _repository = new();
    private readonly Mock<IPetRepository> _pets = new();
    private EvolucaoClinicaService Service => new(_repository.Object, _pets.Object);

    [Fact]
    public async Task CreateAsync_WhenPetExists_WritesOnlyDocumentWithAuthenticatedAuthor()
    {
        _pets.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new Pet { Id = 7 });
        EvolucaoClinica? stored = null;
        _repository.Setup(x => x.InsertAsync(It.IsAny<EvolucaoClinica>(), It.IsAny<CancellationToken>()))
            .Callback<EvolucaoClinica, CancellationToken>((item, _) => stored = item).Returns(Task.CompletedTask);

        var result = await Service.CreateAsync(7, new() { Texto = "Retorno sem intercorrências.", Tags = ["retorno"] }, "clinico");

        Assert.NotNull(stored);
        Assert.Equal(stored.Id, result.Id);
        Assert.Equal("clinico", result.Autor);
        Assert.Equal(DateTimeKind.Utc, result.RegistradoEmUtc.Kind);
        _pets.Verify(x => x.CreateAsync(It.IsAny<Pet>()), Times.Never);
        _pets.Verify(x => x.UpdateAsync(It.IsAny<Pet>()), Times.Never);
        _repository.Verify(x => x.InsertAsync(It.IsAny<EvolucaoClinica>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenPetIsMissing_DoesNotWriteDocument()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service.CreateAsync(99, new() { Texto = "Retorno" }, "clinico"));

        _repository.Verify(x => x.InsertAsync(It.IsAny<EvolucaoClinica>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenMongoIsUnavailable_PropagatesFailureWithoutRelationalWrite()
    {
        _pets.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new Pet { Id = 7 });
        _repository.Setup(x => x.InsertAsync(It.IsAny<EvolucaoClinica>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DependencyUnavailableException("Mongo indisponível"));

        await Assert.ThrowsAsync<DependencyUnavailableException>(() => Service.CreateAsync(7, new() { Texto = "Retorno" }, "clinico"));

        _pets.Verify(x => x.UpdateAsync(It.IsAny<Pet>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service.GetByIdAsync(1, Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsDocumentBelongingToPet()
    {
        var document = new EvolucaoClinica(Guid.NewGuid().ToString("N"), 1, "Retorno", "clinico", DateTime.UtcNow);
        _repository.Setup(x => x.GetByIdAsync(1, document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);

        var result = await Service.GetByIdAsync(1, document.Id);

        Assert.Equal(document.Id, result.Id);
        Assert.Equal(1, result.PetId);
    }

    [Theory]
    [InlineData(0, "asc")]
    [InlineData(1000001, "asc")]
    [InlineData(1, "drop")]
    public async Task GetByPetAsync_WhenQueryIsInvalid_DoesNotQueryRepository(int page, string direction)
    {
        await Assert.ThrowsAsync<BusinessException>(() => Service.GetByPetAsync(1, new() { Page = page, Direction = direction }));

        _repository.Verify(x => x.GetByPetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetByPetAsync_WhenPageSizeIsInvalid_RejectsQuery(int pageSize)
    {
        await Assert.ThrowsAsync<BusinessException>(() => Service.GetByPetAsync(1, new() { PageSize = pageSize }));
    }

    [Fact]
    public async Task GetByPetAsync_WhenPeriodIsReversed_RejectsQuery()
    {
        await Assert.ThrowsAsync<BusinessException>(() => Service.GetByPetAsync(1,
            new() { Inicio = DateTimeOffset.UtcNow, Fim = DateTimeOffset.UtcNow.AddDays(-1) }));
    }

    [Fact]
    public async Task GetByPetAsync_WhenTagIsBlank_RejectsQuery()
    {
        await Assert.ThrowsAsync<BusinessException>(() => Service.GetByPetAsync(1, new() { Tag = " " }));
    }

    [Fact]
    public async Task GetByPetAsync_WhenFiltersAreValid_ForwardsUtcPeriodAndReturnsMaterializedPage()
    {
        var inicio = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.FromHours(-3));
        var fim = inicio.AddDays(1);
        var document = new EvolucaoClinica(Guid.NewGuid().ToString("N"), 1, "Retorno", "clinico", inicio.UtcDateTime);
        _repository.Setup(x => x.GetByPetAsync(1, 2, 5, "retorno", inicio.UtcDateTime, fim.UtcDateTime,
            false, It.IsAny<CancellationToken>())).ReturnsAsync((new[] { document }, 6));

        var result = await Service.GetByPetAsync(1, new()
        {
            Page = 2, PageSize = 5, Tag = " retorno ", Inicio = inicio, Fim = fim, Direction = "asc"
        });

        Assert.Equal(6, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(document.Id, Assert.Single(result.Data).Id);
    }
}
