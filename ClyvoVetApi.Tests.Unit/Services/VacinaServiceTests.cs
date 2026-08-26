using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using ClyvoVetApi.Tests.Unit.Fixtures;
using Moq;
using Xunit;

namespace ClyvoVetApi.Tests.Unit.Services;

public class VacinaServiceTests : IClassFixture<TestDataFixture>
{
    private readonly TestDataFixture _data;
    private readonly Mock<IVacinaRepository> _repositoryMock;
    private readonly Mock<IPetRepository> _petRepositoryMock;
    private readonly VacinaService _service;

    public VacinaServiceTests(TestDataFixture data)
    {
        _data = data;
        _repositoryMock = new Mock<IVacinaRepository>();
        _petRepositoryMock = new Mock<IPetRepository>();
        _service = new VacinaService(_repositoryMock.Object, _petRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsPagedResponse()
    {
        var vacinas = new List<Vacina>
        {
            _data.NovaVacina(id: 1),
            _data.NovaVacina(id: 2, nome: "V10", status: "P", data: DateTime.Today.AddDays(5))
        };
        _repositoryMock.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((vacinas, 2));

        var result = await _service.GetAllAsync(1, 10);

        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WhenVacinaExists_ReturnsVacina()
    {
        var vacina = _data.NovaVacina(id: 1, pet: _data.NovoPet());
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vacina);

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Rex", result.PetNome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenVacinaDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Vacina?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetPendentesAsync_WhenCalled_ReturnsOnlyPendingVacinas()
    {
        var vacinas = new List<Vacina>
        {
            _data.NovaVacina(id: 1, nome: "V10", status: "P", data: DateTime.Today.AddDays(5))
        };
        _repositoryMock.Setup(r => r.GetPendentesAsync()).ReturnsAsync(vacinas);

        var result = await _service.GetPendentesAsync();

        var vacina = Assert.Single(result);
        Assert.Equal("P", vacina.Status);
    }

    [Fact]
    public async Task GetByNomeAsync_WhenCalled_ReturnsMatchingVacinas()
    {
        var vacinas = new List<Vacina> { _data.NovaVacina(id: 1) };
        _repositoryMock.Setup(r => r.GetByNomeAsync("Antirrábica")).ReturnsAsync(vacinas);

        var result = await _service.GetByNomeAsync("Antirrábica");

        var vacina = Assert.Single(result);
        Assert.Equal("Antirrábica", vacina.Nome);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-30)]
    public async Task GetProximasAsync_WhenDaysIsNotPositive_ThrowsBusinessException(int dias)
    {
        await Assert.ThrowsAsync<BusinessException>(() => _service.GetProximasAsync(dias));
        _repositoryMock.Verify(r => r.GetProximasAsync(It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GetProximasAsync_WhenDaysIsPositive_ReturnsUpcomingVacinas()
    {
        var vacinas = new List<Vacina>
        {
            _data.NovaVacina(id: 1, nome: "V10", status: "P", data: DateTime.Today.AddDays(10))
        };
        _repositoryMock.Setup(r => r.GetProximasAsync(It.IsAny<DateTime>())).ReturnsAsync(vacinas);

        var result = await _service.GetProximasAsync(30);

        Assert.Single(result);
    }

    [Fact]
    public async Task CreateAsync_WhenPetDoesNotExist_ThrowsBusinessException()
    {
        var dto = new VacinaRequestDto { Nome = "Antirrábica", Status = "P", Data = DateTime.Now, PetId = 99 };
        _petRepositoryMock.Setup(p => p.GetByIdAsync(dto.PetId)).ReturnsAsync((Pet?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Vacina>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenPetExists_ReturnsCreatedVacina()
    {
        var dto = new VacinaRequestDto { Nome = "Antirrábica", Status = "A", Data = DateTime.Now, PetId = 1 };
        var pet = _data.NovoPet();

        _petRepositoryMock.Setup(p => p.GetByIdAsync(dto.PetId)).ReturnsAsync(pet);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Vacina>()))
            .ReturnsAsync((Vacina v) => { v.Id = 10; return v; });

        var result = await _service.CreateAsync(dto);

        Assert.Equal(10, result.Id);
        Assert.Equal("Rex", result.PetNome);
    }

    [Fact]
    public async Task UpdateAsync_WhenVacinaDoesNotExist_ThrowsNotFoundException()
    {
        var dto = new VacinaRequestDto { Nome = "Antirrábica", Status = "P", Data = DateTime.Now, PetId = 1 };
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Vacina?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(99, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenPetDoesNotExist_ThrowsBusinessException()
    {
        var vacina = _data.NovaVacina(id: 1, status: "P");
        var dto = new VacinaRequestDto { Nome = "Antirrábica", Status = "A", Data = DateTime.Now, PetId = 99 };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vacina);
        _petRepositoryMock.Setup(p => p.GetByIdAsync(dto.PetId)).ReturnsAsync((Pet?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(1, dto));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Vacina>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenDataIsValid_ReturnsUpdatedVacina()
    {
        var vacina = _data.NovaVacina(id: 1, status: "P");
        var pet = _data.NovoPet();
        var dto = new VacinaRequestDto { Nome = "Antirrábica Alterada", Status = "A", Data = DateTime.Now, PetId = 1 };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vacina);
        _petRepositoryMock.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(pet);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Vacina>())).ReturnsAsync((Vacina v) => v);

        var result = await _service.UpdateAsync(1, dto);

        Assert.Equal("Antirrábica Alterada", result.Nome);
        Assert.Equal("A", result.Status);
        Assert.Equal("Rex", result.PetNome);
    }

    [Fact]
    public async Task DeleteAsync_WhenVacinaDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Vacina?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_WhenVacinaExists_CallsRepositoryDelete()
    {
        var vacina = _data.NovaVacina(id: 1);
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vacina);

        await _service.DeleteAsync(1);

        _repositoryMock.Verify(r => r.DeleteAsync(vacina), Times.Once);
    }
}
