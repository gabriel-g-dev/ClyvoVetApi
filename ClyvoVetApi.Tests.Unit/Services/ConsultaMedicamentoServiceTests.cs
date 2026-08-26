using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using Moq;
using Xunit;

namespace ClyvoVetApi.Tests.Unit.Services;

public class ConsultaMedicamentoServiceTests
{
    private readonly Mock<IConsultaMedicamentoRepository> _repositoryMock;
    private readonly Mock<IConsultaRepository> _consultaRepositoryMock;
    private readonly Mock<IMedicamentoRepository> _medicamentoRepositoryMock;
    private readonly ConsultaMedicamentoService _service;

    public ConsultaMedicamentoServiceTests()
    {
        _repositoryMock = new Mock<IConsultaMedicamentoRepository>();
        _consultaRepositoryMock = new Mock<IConsultaRepository>();
        _medicamentoRepositoryMock = new Mock<IMedicamentoRepository>();
        _service = new ConsultaMedicamentoService(
            _repositoryMock.Object,
            _consultaRepositoryMock.Object,
            _medicamentoRepositoryMock.Object);
    }

    private static ConsultaMedicamentoRequestDto NovoRequest() => new()
    {
        ConsultaId = 1,
        MedicamentoId = 2,
        Dosagem = "500mg a cada 12h"
    };

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsPagedResponse()
    {
        var prescricoes = new List<ConsultaMedicamento>
        {
            new() { Id = 1, ConsultaId = 1, MedicamentoId = 2, Dosagem = "500mg" },
            new() { Id = 2, ConsultaId = 1, MedicamentoId = 3, Dosagem = "250mg" }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((prescricoes, 2));

        var result = await _service.GetAllAsync(1, 10);

        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WhenPrescricaoExists_ReturnsPrescricao()
    {
        var prescricao = new ConsultaMedicamento
        {
            Id = 1,
            ConsultaId = 1,
            MedicamentoId = 2,
            Dosagem = "500mg",
            Medicamento = new Medicamento { Id = 2, Nome = "Amoxicilina" }
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prescricao);

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("500mg", result.Dosagem);
        Assert.Equal("Amoxicilina", result.MedicamentoNome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPrescricaoDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ConsultaMedicamento?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByConsultaIdAsync_WhenCalled_ReturnsPrescricoesDaConsulta()
    {
        var prescricoes = new List<ConsultaMedicamento>
        {
            new() { Id = 1, ConsultaId = 7, MedicamentoId = 2, Dosagem = "500mg" }
        };
        _repositoryMock.Setup(r => r.GetByConsultaIdAsync(7)).ReturnsAsync(prescricoes);

        var result = await _service.GetByConsultaIdAsync(7);

        var prescricao = Assert.Single(result);
        Assert.Equal(7, prescricao.ConsultaId);
    }

    [Fact]
    public async Task CreateAsync_WhenConsultaDoesNotExist_ThrowsBusinessException()
    {
        var dto = NovoRequest();
        _consultaRepositoryMock.Setup(c => c.GetByIdAsync(dto.ConsultaId)).ReturnsAsync((Consulta?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<ConsultaMedicamento>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenMedicamentoDoesNotExist_ThrowsBusinessException()
    {
        var dto = NovoRequest();
        _consultaRepositoryMock.Setup(c => c.GetByIdAsync(dto.ConsultaId)).ReturnsAsync(new Consulta { Id = 1 });
        _medicamentoRepositoryMock.Setup(m => m.GetByIdAsync(dto.MedicamentoId)).ReturnsAsync((Medicamento?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<ConsultaMedicamento>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenConsultaAndMedicamentoExist_ReturnsCreatedPrescricao()
    {
        var dto = NovoRequest();
        _consultaRepositoryMock.Setup(c => c.GetByIdAsync(dto.ConsultaId)).ReturnsAsync(new Consulta { Id = 1 });
        _medicamentoRepositoryMock.Setup(m => m.GetByIdAsync(dto.MedicamentoId))
            .ReturnsAsync(new Medicamento { Id = 2, Nome = "Amoxicilina" });
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<ConsultaMedicamento>()))
            .ReturnsAsync((ConsultaMedicamento cm) => { cm.Id = 10; return cm; });

        var result = await _service.CreateAsync(dto);

        Assert.Equal(10, result.Id);
        Assert.Equal("500mg a cada 12h", result.Dosagem);
        Assert.Equal("Amoxicilina", result.MedicamentoNome);
    }

    [Fact]
    public async Task UpdateAsync_WhenPrescricaoDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ConsultaMedicamento?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(99, NovoRequest()));
    }

    [Fact]
    public async Task UpdateAsync_WhenConsultaDoesNotExist_ThrowsBusinessException()
    {
        var dto = NovoRequest();
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ConsultaMedicamento { Id = 1 });
        _consultaRepositoryMock.Setup(c => c.GetByIdAsync(dto.ConsultaId)).ReturnsAsync((Consulta?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(1, dto));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ConsultaMedicamento>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenMedicamentoDoesNotExist_ThrowsBusinessException()
    {
        var dto = NovoRequest();
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ConsultaMedicamento { Id = 1 });
        _consultaRepositoryMock.Setup(c => c.GetByIdAsync(dto.ConsultaId)).ReturnsAsync(new Consulta { Id = 1 });
        _medicamentoRepositoryMock.Setup(m => m.GetByIdAsync(dto.MedicamentoId)).ReturnsAsync((Medicamento?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(1, dto));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ConsultaMedicamento>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenDataIsValid_ReturnsUpdatedPrescricao()
    {
        var prescricao = new ConsultaMedicamento { Id = 1, ConsultaId = 1, MedicamentoId = 2, Dosagem = "250mg" };
        var dto = NovoRequest();

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prescricao);
        _consultaRepositoryMock.Setup(c => c.GetByIdAsync(dto.ConsultaId)).ReturnsAsync(new Consulta { Id = 1 });
        _medicamentoRepositoryMock.Setup(m => m.GetByIdAsync(dto.MedicamentoId))
            .ReturnsAsync(new Medicamento { Id = 2, Nome = "Amoxicilina" });
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConsultaMedicamento>()))
            .ReturnsAsync((ConsultaMedicamento cm) => cm);

        var result = await _service.UpdateAsync(1, dto);

        Assert.Equal("500mg a cada 12h", result.Dosagem);
        Assert.Equal("Amoxicilina", result.MedicamentoNome);
    }

    [Fact]
    public async Task DeleteAsync_WhenPrescricaoDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ConsultaMedicamento?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_WhenPrescricaoExists_CallsRepositoryDelete()
    {
        var prescricao = new ConsultaMedicamento { Id = 1, Dosagem = "500mg" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prescricao);

        await _service.DeleteAsync(1);

        _repositoryMock.Verify(r => r.DeleteAsync(prescricao), Times.Once);
    }
}
