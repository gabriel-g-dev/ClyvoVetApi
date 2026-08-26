using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using Moq;
using Xunit;

namespace ClyvoVetApi.Tests.Unit.Services;

public class FuncionarioServiceTests
{
    private readonly Mock<IFuncionarioRepository> _repositoryMock;
    private readonly FuncionarioService _service;

    public FuncionarioServiceTests()
    {
        _repositoryMock = new Mock<IFuncionarioRepository>();
        _service = new FuncionarioService(_repositoryMock.Object);
    }

    private static Funcionario NovoFuncionario(int id = 1) => new()
    {
        Id = id,
        Nome = "Dra. Ana Souza",
        Setor = "Clínica Geral",
        Cargo = "Veterinária",
        Email = "ana@clyvovet.com",
        Telefone = "11988888888"
    };

    private static FuncionarioRequestDto NovoRequest(string email = "ana@clyvovet.com") => new()
    {
        Nome = "Dra. Ana Souza",
        Setor = "Clínica Geral",
        Cargo = "Veterinária",
        Email = email,
        Telefone = "11988888888"
    };

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsPagedResponse()
    {
        var funcionarios = new List<Funcionario> { NovoFuncionario(1), NovoFuncionario(2) };
        _repositoryMock.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((funcionarios, 2));

        var result = await _service.GetAllAsync(1, 10);

        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WhenFuncionarioExists_ReturnsFuncionario()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(NovoFuncionario());

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Dra. Ana Souza", result.Nome);
        Assert.Equal("Veterinária", result.Cargo);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFuncionarioDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Funcionario?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByEmailAsync_WhenFuncionarioExists_ReturnsFuncionario()
    {
        _repositoryMock.Setup(r => r.GetByEmailAsync("ana@clyvovet.com")).ReturnsAsync(NovoFuncionario());

        var result = await _service.GetByEmailAsync("ana@clyvovet.com");

        Assert.Equal("ana@clyvovet.com", result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenFuncionarioDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByEmailAsync("naoexiste@clyvovet.com")).ReturnsAsync((Funcionario?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByEmailAsync("naoexiste@clyvovet.com"));
    }

    [Fact]
    public async Task GetBySetorAsync_WhenCalled_ReturnsFuncionariosDoSetor()
    {
        _repositoryMock.Setup(r => r.GetBySetorAsync("Clínica Geral")).ReturnsAsync([NovoFuncionario()]);

        var result = await _service.GetBySetorAsync("Clínica Geral");

        var funcionario = Assert.Single(result);
        Assert.Equal("Clínica Geral", funcionario.Setor);
    }

    [Fact]
    public async Task GetByCargoAsync_WhenCalled_ReturnsFuncionariosDoCargo()
    {
        _repositoryMock.Setup(r => r.GetByCargoAsync("Veterinária")).ReturnsAsync([NovoFuncionario()]);

        var result = await _service.GetByCargoAsync("Veterinária");

        var funcionario = Assert.Single(result);
        Assert.Equal("Veterinária", funcionario.Cargo);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailAlreadyExists_ThrowsBusinessException()
    {
        var dto = NovoRequest();
        _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(NovoFuncionario());

        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Funcionario>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailIsAvailable_ReturnsCreatedFuncionario()
    {
        var dto = NovoRequest("novo@clyvovet.com");
        _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Funcionario?)null);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Funcionario>()))
            .ReturnsAsync((Funcionario f) => { f.Id = 10; return f; });

        var result = await _service.CreateAsync(dto);

        Assert.Equal(10, result.Id);
        Assert.Equal("novo@clyvovet.com", result.Email);
        Assert.Equal("Clínica Geral", result.Setor);
    }

    [Fact]
    public async Task UpdateAsync_WhenFuncionarioDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Funcionario?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(99, NovoRequest()));
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailBelongsToAnotherFuncionario_ThrowsBusinessException()
    {
        var funcionario = NovoFuncionario();
        var dto = NovoRequest("outro@clyvovet.com");

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(funcionario);
        _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(NovoFuncionario(2));

        await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(1, dto));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Funcionario>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailIsUnchanged_SkipsDuplicateCheck()
    {
        var funcionario = NovoFuncionario();
        var dto = NovoRequest();
        dto.Nome = "Dra. Ana Souza Lima";

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(funcionario);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Funcionario>())).ReturnsAsync((Funcionario f) => f);

        var result = await _service.UpdateAsync(1, dto);

        Assert.Equal("Dra. Ana Souza Lima", result.Nome);
        _repositoryMock.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenNewEmailIsAvailable_ReturnsUpdatedFuncionario()
    {
        var funcionario = NovoFuncionario();
        var dto = NovoRequest("ana.souza@clyvovet.com");

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(funcionario);
        _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Funcionario?)null);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Funcionario>())).ReturnsAsync((Funcionario f) => f);

        var result = await _service.UpdateAsync(1, dto);

        Assert.Equal("ana.souza@clyvovet.com", result.Email);
    }

    [Fact]
    public async Task DeleteAsync_WhenFuncionarioDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Funcionario?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_WhenFuncionarioExists_CallsRepositoryDelete()
    {
        var funcionario = NovoFuncionario();
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(funcionario);

        await _service.DeleteAsync(1);

        _repositoryMock.Verify(r => r.DeleteAsync(funcionario), Times.Once);
    }
}
