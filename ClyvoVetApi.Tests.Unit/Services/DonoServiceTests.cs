using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using Moq;
using Xunit;

namespace ClyvoVetApi.Tests.Unit.Services;

public class DonoServiceTests
{
    private readonly Mock<IDonoRepository> _repositoryMock;
    private readonly DonoService _service;

    public DonoServiceTests()
    {
        _repositoryMock = new Mock<IDonoRepository>();
        _service = new DonoService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsPagedResponse()
    {
        var donos = new List<Dono>
        {
            new() { Id = 1, Nome = "Dono 1", Email = "dono1@email.com", Telefone = "11999999999" },
            new() { Id = 2, Nome = "Dono 2", Email = "dono2@email.com", Telefone = "11988888888" }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((donos, 2));

        var result = await _service.GetAllAsync(1, 10);

        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WhenDonoExists_ReturnsDonoDetails()
    {
        var dono = new Dono { Id = 1, Nome = "Dono 1", Email = "dono1@email.com" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dono);

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Dono 1", result.Nome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDonoHasPets_ReturnsPetsAndTotal()
    {
        var dono = new Dono
        {
            Id = 1,
            Nome = "Dono 1",
            Email = "dono1@email.com",
            Telefone = "11999999999",
            Pets = [new Pet { Id = 5, Nome = "Rex", Especie = "Cachorro", Raca = "Labrador", DonoId = 1, Peso = 10m }]
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dono);

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(1, result.TotalPets);
        var pet = Assert.Single(result.Pets);
        Assert.Equal("Rex", pet.Nome);
        Assert.Equal("Labrador", pet.Raca);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDonoDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Dono?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByEmailAsync_WhenDonoExists_ReturnsDonoDetails()
    {
        var email = "dono1@email.com";
        var dono = new Dono { Id = 1, Nome = "Dono 1", Email = email };
        _repositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(dono);

        var result = await _service.GetByEmailAsync(email);

        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenDonoDoesNotExist_ThrowsNotFoundException()
    {
        var email = "naoexiste@email.com";
        _repositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((Dono?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByEmailAsync(email));
    }

    [Fact]
    public async Task GetPetsByDonoIdAsync_WhenDonoExists_ReturnsPets()
    {
        var dono = new Dono
        {
            Id = 1,
            Nome = "Dono 1",
            Pets = [new Pet { Id = 5, Nome = "Rex", Especie = "Cachorro", DonoId = 1 }]
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dono);

        var result = await _service.GetPetsByDonoIdAsync(1);

        var pet = Assert.Single(result);
        Assert.Equal(5, pet.Id);
        Assert.Equal("Rex", pet.Nome);
    }

    [Fact]
    public async Task GetPetsByDonoIdAsync_WhenDonoDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Dono?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetPetsByDonoIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_WhenEmailAlreadyExists_ThrowsBusinessException()
    {
        var dto = new DonoRequestDto { Nome = "Novo", Email = "dono1@email.com", Telefone = "11999999999" };
        _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(new Dono());

        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Dono>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailIsAvailable_ReturnsCreatedDono()
    {
        var dto = new DonoRequestDto { Nome = "Novo Dono", Email = "novo@email.com", Telefone = "11999999999" };
        _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Dono?)null);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Dono>()))
            .ReturnsAsync((Dono d) => { d.Id = 10; return d; });

        var result = await _service.CreateAsync(dto);

        Assert.Equal(10, result.Id);
        Assert.Equal("Novo Dono", result.Nome);
    }

    [Fact]
    public async Task UpdateAsync_WhenDonoDoesNotExist_ThrowsNotFoundException()
    {
        var dto = new DonoRequestDto { Nome = "Nome", Email = "email@email.com", Telefone = "11999999999" };
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Dono?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(99, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailBelongsToAnotherDono_ThrowsBusinessException()
    {
        var dono = new Dono { Id = 1, Nome = "Dono 1", Email = "dono1@email.com" };
        var dto = new DonoRequestDto { Nome = "Dono Alterado", Email = "outro@email.com" };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dono);
        _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(new Dono { Id = 2, Email = dto.Email });

        await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailIsUnchanged_SkipsDuplicateCheck()
    {
        var dono = new Dono { Id = 1, Nome = "Dono 1", Email = "dono1@email.com" };
        var dto = new DonoRequestDto { Nome = "Dono 1 Alterado", Email = "dono1@email.com", Telefone = "11999999999" };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dono);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Dono>())).ReturnsAsync((Dono d) => d);

        var result = await _service.UpdateAsync(1, dto);

        Assert.Equal("Dono 1 Alterado", result.Nome);
        _repositoryMock.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenDonoDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Dono?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_WhenDonoExists_CallsRepositoryDelete()
    {
        var dono = new Dono { Id = 1, Nome = "Dono 1" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dono);

        await _service.DeleteAsync(1);

        _repositoryMock.Verify(r => r.DeleteAsync(dono), Times.Once);
    }
}
