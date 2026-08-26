using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using ClyvoVetApi.Tests.Unit.Fixtures;
using Moq;
using Xunit;

namespace ClyvoVetApi.Tests.Unit.Services;

public class PetServiceTests : IClassFixture<TestDataFixture>
{
    private readonly TestDataFixture _data;
    private readonly Mock<IPetRepository> _repositoryMock;
    private readonly Mock<IDonoRepository> _donoRepositoryMock;
    private readonly PetService _service;

    public PetServiceTests(TestDataFixture data)
    {
        _data = data;
        _repositoryMock = new Mock<IPetRepository>();
        _donoRepositoryMock = new Mock<IDonoRepository>();
        _service = new PetService(_repositoryMock.Object, _donoRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsPagedResponse()
    {
        var pets = new List<Pet>
        {
            _data.NovoPet(),
            _data.NovoPet(id: 2, nome: "Mingau", especie: "Gato", raca: "Persa", peso: 4.2m)
        };
        _repositoryMock.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((pets, 2));

        var result = await _service.GetAllAsync(1, 10);

        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WhenPetExists_ReturnsPetDetails()
    {
        var pet = _data.NovoPet(dono: _data.NovoDono());
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Rex", result.Nome);
        Assert.Equal("Dono 1", result.DonoNome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPetHasHistory_ReturnsConsultasAndVacinas()
    {
        var pet = _data.NovoPet(
            dono: _data.NovoDono(),
            consultas: [_data.NovaConsulta(tipo: "Rotina", descricao: "Checkup", funcionario: _data.NovoFuncionario())],
            vacinas: [_data.NovaVacina(nome: "V10")]);

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        var result = await _service.GetByIdAsync(1);

        var consulta = Assert.Single(result.Consultas);
        Assert.Equal("Dra. Ana", consulta.FuncionarioNome);
        Assert.Equal("Rex", consulta.PetNome);

        var vacina = Assert.Single(result.Vacinas);
        Assert.Equal("V10", vacina.Nome);

        Assert.NotNull(result.Dono);
        Assert.Equal("dono1@email.com", result.Dono.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPetDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Pet?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByEspecieAsync_WhenCalled_ReturnsPetsOfEspecie()
    {
        var pets = new List<Pet> { _data.NovoPet(especie: "Cachorro") };
        _repositoryMock.Setup(r => r.GetByEspecieAsync("Cachorro")).ReturnsAsync(pets);

        var result = await _service.GetByEspecieAsync("Cachorro");

        var pet = Assert.Single(result);
        Assert.Equal("Cachorro", pet.Especie);
    }

    [Fact]
    public async Task GetByRacaAsync_WhenCalled_ReturnsPetsOfRaca()
    {
        var pets = new List<Pet> { _data.NovoPet(raca: "Labrador") };
        _repositoryMock.Setup(r => r.GetByRacaAsync("Labrador")).ReturnsAsync(pets);

        var result = await _service.GetByRacaAsync("Labrador");

        var pet = Assert.Single(result);
        Assert.Equal("Labrador", pet.Raca);
    }

    [Fact]
    public async Task CreateAsync_WhenDonoDoesNotExist_ThrowsBusinessException()
    {
        var dto = new PetRequestDto { Nome = "Rex", Especie = "Cachorro", Raca = "Labrador", DonoId = 99, Peso = 10.5m };
        _donoRepositoryMock.Setup(d => d.GetByIdAsync(dto.DonoId)).ReturnsAsync((Dono?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Pet>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenDonoExists_ReturnsCreatedPet()
    {
        var dto = new PetRequestDto { Nome = "Rex", Especie = "Cachorro", Raca = "Labrador", DonoId = 1, Peso = 10.5m };
        var dono = _data.NovoDono();

        _donoRepositoryMock.Setup(d => d.GetByIdAsync(dto.DonoId)).ReturnsAsync(dono);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Pet>()))
            .ReturnsAsync((Pet p) => { p.Id = 5; return p; });

        var result = await _service.CreateAsync(dto);

        Assert.Equal(5, result.Id);
        Assert.Equal("Rex", result.Nome);
        Assert.Equal("Dono 1", result.DonoNome);
    }

    [Fact]
    public async Task UpdateAsync_WhenPetDoesNotExist_ThrowsNotFoundException()
    {
        var dto = new PetRequestDto { Nome = "Rex", DonoId = 1, Peso = 10.5m };
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Pet?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(99, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenDonoDoesNotExist_ThrowsBusinessException()
    {
        var pet = _data.NovoPet();
        var dto = new PetRequestDto { Nome = "Rex Novo", DonoId = 99, Peso = 12.0m };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);
        _donoRepositoryMock.Setup(d => d.GetByIdAsync(dto.DonoId)).ReturnsAsync((Dono?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(1, dto));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Pet>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenDataIsValid_ReturnsUpdatedPet()
    {
        var pet = _data.NovoPet();
        var dono = _data.NovoDono();
        var dto = new PetRequestDto { Nome = "Rex Alterado", Especie = "Cachorro", Raca = "Golden", DonoId = 1, Peso = 11.5m };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);
        _donoRepositoryMock.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(dono);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Pet>())).ReturnsAsync((Pet p) => p);

        var result = await _service.UpdateAsync(1, dto);

        Assert.Equal("Rex Alterado", result.Nome);
        Assert.Equal("Golden", result.Raca);
        Assert.Equal(11.5m, result.Peso);
    }

    [Fact]
    public async Task DeleteAsync_WhenPetDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Pet?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_WhenPetExists_CallsRepositoryDelete()
    {
        var pet = _data.NovoPet();
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        await _service.DeleteAsync(1);

        _repositoryMock.Verify(r => r.DeleteAsync(pet), Times.Once);
    }
}
