using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using Moq;
using Xunit;

namespace ClyvoVetApi.Tests.Unit.Services;

public class MedicamentoServiceTests
{
    private readonly Mock<IMedicamentoRepository> _repositoryMock;
    private readonly MedicamentoService _service;

    public MedicamentoServiceTests()
    {
        _repositoryMock = new Mock<IMedicamentoRepository>();
        _service = new MedicamentoService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsPagedResponse()
    {
        var medicamentos = new List<Medicamento>
        {
            new() { Id = 1, Nome = "Amoxicilina" },
            new() { Id = 2, Nome = "Dipirona" }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((medicamentos, 2));

        var result = await _service.GetAllAsync(1, 10);

        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WhenMedicamentoExists_ReturnsMedicamento()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Medicamento { Id = 1, Nome = "Amoxicilina" });

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Amoxicilina", result.Nome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMedicamentoDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Medicamento?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByNomeAsync_WhenCalled_ReturnsMatchingMedicamentos()
    {
        var medicamentos = new List<Medicamento> { new() { Id = 1, Nome = "Amoxicilina" } };
        _repositoryMock.Setup(r => r.GetByNomeAsync("Amox")).ReturnsAsync(medicamentos);

        var result = await _service.GetByNomeAsync("Amox");

        var medicamento = Assert.Single(result);
        Assert.Equal("Amoxicilina", medicamento.Nome);
    }

    [Fact]
    public async Task CreateAsync_WhenCalled_ReturnsCreatedMedicamento()
    {
        var dto = new MedicamentoRequestDto { Nome = "Amoxicilina" };
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Medicamento>()))
            .ReturnsAsync((Medicamento m) => { m.Id = 10; return m; });

        var result = await _service.CreateAsync(dto);

        Assert.Equal(10, result.Id);
        Assert.Equal("Amoxicilina", result.Nome);
        _repositoryMock.Verify(r => r.CreateAsync(It.Is<Medicamento>(m => m.Nome == "Amoxicilina")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenMedicamentoDoesNotExist_ThrowsNotFoundException()
    {
        var dto = new MedicamentoRequestDto { Nome = "Amoxicilina" };
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Medicamento?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(99, dto));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Medicamento>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenMedicamentoExists_ReturnsUpdatedMedicamento()
    {
        var medicamento = new Medicamento { Id = 1, Nome = "Amoxicilina" };
        var dto = new MedicamentoRequestDto { Nome = "Amoxicilina 500mg" };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(medicamento);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Medicamento>())).ReturnsAsync((Medicamento m) => m);

        var result = await _service.UpdateAsync(1, dto);

        Assert.Equal("Amoxicilina 500mg", result.Nome);
    }

    [Fact]
    public async Task DeleteAsync_WhenMedicamentoDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Medicamento?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_WhenMedicamentoExists_CallsRepositoryDelete()
    {
        var medicamento = new Medicamento { Id = 1, Nome = "Amoxicilina" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(medicamento);

        await _service.DeleteAsync(1);

        _repositoryMock.Verify(r => r.DeleteAsync(medicamento), Times.Once);
    }
}
