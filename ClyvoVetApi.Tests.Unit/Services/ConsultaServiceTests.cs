using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using Moq;
using Xunit;

namespace ClyvoVetApi.Tests.Unit.Services;

public class ConsultaServiceTests
{
    private readonly Mock<IConsultaRepository> _repositoryMock;
    private readonly Mock<IPetRepository> _petRepositoryMock;
    private readonly Mock<IFuncionarioRepository> _funcionarioRepositoryMock;
    private readonly ConsultaService _service;

    public ConsultaServiceTests()
    {
        _repositoryMock = new Mock<IConsultaRepository>();
        _petRepositoryMock = new Mock<IPetRepository>();
        _funcionarioRepositoryMock = new Mock<IFuncionarioRepository>();
        _service = new ConsultaService(_repositoryMock.Object, _petRepositoryMock.Object, _funcionarioRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsPagedResponse()
    {
        var consultas = new List<Consulta>
        {
            new() { Id = 1, Tipo = "Geral", Valor = 150.00m, Descricao = "Rotina", Status = "R", Data = DateTime.Now, PetId = 1, FuncionarioId = 1 },
            new() { Id = 2, Tipo = "Retorno", Valor = 100.00m, Descricao = "Checkup", Status = "A", Data = DateTime.Now, PetId = 1, FuncionarioId = 1 }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((consultas, 2));

        var result = await _service.GetAllAsync(1, 10);

        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WhenConsultaExists_ReturnsConsulta()
    {
        var consulta = new Consulta
        {
            Id = 1,
            Tipo = "Geral",
            Valor = 150.00m,
            Descricao = "Rotina",
            Status = "R",
            Data = DateTime.Now,
            Pet = new Pet { Id = 1, Nome = "Rex" }
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(consulta);

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Rex", result.PetNome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenConsultaDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Consulta?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByFuncionarioIdAsync_WhenCalled_ReturnsConsultasDoFuncionario()
    {
        var consultas = new List<Consulta>
        {
            new() { Id = 1, Tipo = "Geral", Descricao = "Rotina", Status = "R", Data = DateTime.Now, PetId = 1, FuncionarioId = 7 }
        };
        _repositoryMock.Setup(r => r.GetByFuncionarioIdAsync(7)).ReturnsAsync(consultas);

        var result = await _service.GetByFuncionarioIdAsync(7);

        var consulta = Assert.Single(result);
        Assert.Equal(7, consulta.FuncionarioId);
    }

    [Fact]
    public async Task GetByPeriodoAsync_WhenStartIsAfterEnd_ThrowsBusinessException()
    {
        var inicio = DateTime.Now.AddDays(1);
        var fim = DateTime.Now;

        await Assert.ThrowsAsync<BusinessException>(() => _service.GetByPeriodoAsync(inicio, fim));
        _repositoryMock.Verify(r => r.GetByPeriodoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GetByPeriodoAsync_WhenPeriodIsValid_ReturnsConsultas()
    {
        var inicio = DateTime.Now.AddDays(-7);
        var fim = DateTime.Now;
        var consultas = new List<Consulta>
        {
            new() { Id = 1, Tipo = "Geral", Descricao = "Rotina", Status = "R", Data = DateTime.Now.AddDays(-1), PetId = 1, FuncionarioId = 1 }
        };
        _repositoryMock.Setup(r => r.GetByPeriodoAsync(inicio, fim)).ReturnsAsync(consultas);

        var result = await _service.GetByPeriodoAsync(inicio, fim);

        Assert.Single(result);
    }

    [Fact]
    public async Task CreateAsync_WhenPetDoesNotExist_ThrowsBusinessException()
    {
        var dto = new ConsultaRequestDto { Tipo = "Geral", Valor = 100m, Descricao = "Rotina", Status = "A", Data = DateTime.Now, PetId = 99, FuncionarioId = 1 };
        _petRepositoryMock.Setup(p => p.GetByIdAsync(dto.PetId)).ReturnsAsync((Pet?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Consulta>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenFuncionarioDoesNotExist_ThrowsBusinessException()
    {
        var dto = new ConsultaRequestDto { Tipo = "Geral", Valor = 100m, Descricao = "Rotina", Status = "A", Data = DateTime.Now, PetId = 1, FuncionarioId = 99 };
        _petRepositoryMock.Setup(p => p.GetByIdAsync(dto.PetId)).ReturnsAsync(new Pet { Id = 1 });
        _funcionarioRepositoryMock.Setup(f => f.GetByIdAsync(dto.FuncionarioId)).ReturnsAsync((Funcionario?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Consulta>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenDataIsValid_ReturnsCreatedConsulta()
    {
        var dto = new ConsultaRequestDto { Tipo = "Geral", Valor = 150m, Descricao = "Rotina", Status = "A", Data = DateTime.Now, PetId = 1, FuncionarioId = 1 };
        var pet = new Pet { Id = 1, Nome = "Rex" };
        var funcionario = new Funcionario { Id = 1, Nome = "Dra. Ana" };

        _petRepositoryMock.Setup(p => p.GetByIdAsync(dto.PetId)).ReturnsAsync(pet);
        _funcionarioRepositoryMock.Setup(f => f.GetByIdAsync(dto.FuncionarioId)).ReturnsAsync(funcionario);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Consulta>()))
            .ReturnsAsync((Consulta c) => { c.Id = 10; return c; });

        var result = await _service.CreateAsync(dto);

        Assert.Equal(10, result.Id);
        Assert.Equal("Rex", result.PetNome);
        Assert.Equal("Dra. Ana", result.FuncionarioNome);
    }

    [Fact]
    public async Task UpdateAsync_WhenConsultaDoesNotExist_ThrowsNotFoundException()
    {
        var dto = new ConsultaRequestDto { Tipo = "Geral", Valor = 150m, Descricao = "Rotina", Status = "A", Data = DateTime.Now, PetId = 1, FuncionarioId = 1 };
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Consulta?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(99, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenPetDoesNotExist_ThrowsBusinessException()
    {
        var consulta = new Consulta { Id = 1, Tipo = "Geral", Valor = 150m, Descricao = "Rotina", Status = "A", Data = DateTime.Now, PetId = 1, FuncionarioId = 1 };
        var dto = new ConsultaRequestDto { Tipo = "Geral", Valor = 150m, Descricao = "Rotina", Status = "A", Data = DateTime.Now, PetId = 99, FuncionarioId = 1 };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(consulta);
        _petRepositoryMock.Setup(p => p.GetByIdAsync(dto.PetId)).ReturnsAsync((Pet?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenFuncionarioDoesNotExist_ThrowsBusinessException()
    {
        var consulta = new Consulta { Id = 1, Tipo = "Geral", Valor = 150m, Descricao = "Rotina", Status = "A", Data = DateTime.Now, PetId = 1, FuncionarioId = 1 };
        var dto = new ConsultaRequestDto { Tipo = "Geral", Valor = 150m, Descricao = "Rotina", Status = "A", Data = DateTime.Now, PetId = 1, FuncionarioId = 99 };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(consulta);
        _petRepositoryMock.Setup(p => p.GetByIdAsync(dto.PetId)).ReturnsAsync(new Pet { Id = 1 });
        _funcionarioRepositoryMock.Setup(f => f.GetByIdAsync(dto.FuncionarioId)).ReturnsAsync((Funcionario?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenDataIsValid_ReturnsUpdatedConsulta()
    {
        var consulta = new Consulta { Id = 1, Tipo = "Geral", Valor = 150m, Descricao = "Rotina", Status = "A", Data = DateTime.Now, PetId = 1, FuncionarioId = 1 };
        var pet = new Pet { Id = 1, Nome = "Rex" };
        var funcionario = new Funcionario { Id = 1, Nome = "Dra. Ana" };
        var dto = new ConsultaRequestDto { Tipo = "Geral", Valor = 200m, Descricao = "Rotina Alterada", Status = "R", Data = DateTime.Now, PetId = 1, FuncionarioId = 1 };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(consulta);
        _petRepositoryMock.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(pet);
        _funcionarioRepositoryMock.Setup(f => f.GetByIdAsync(1)).ReturnsAsync(funcionario);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Consulta>())).ReturnsAsync((Consulta c) => c);

        var result = await _service.UpdateAsync(1, dto);

        Assert.Equal("Rotina Alterada", result.Descricao);
        Assert.Equal("R", result.Status);
        Assert.Equal(200m, result.Valor);
    }

    [Fact]
    public async Task DeleteAsync_WhenConsultaDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Consulta?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_WhenConsultaExists_CallsRepositoryDelete()
    {
        var consulta = new Consulta { Id = 1, Descricao = "Rotina" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(consulta);

        await _service.DeleteAsync(1);

        _repositoryMock.Verify(r => r.DeleteAsync(consulta), Times.Once);
    }
}
