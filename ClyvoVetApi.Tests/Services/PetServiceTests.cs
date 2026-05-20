using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using Moq;
using Xunit;

namespace ClyvoVetApi.Tests.Services;

public class PetServiceTests
{
    private readonly Mock<IPetRepository> _repositoryMock;
    private readonly Mock<ITutorRepository> _tutorRepositoryMock;
    private readonly PetService _service;

    public PetServiceTests()
    {
        _repositoryMock = new Mock<IPetRepository>();
        _tutorRepositoryMock = new Mock<ITutorRepository>();
        _service = new PetService(_repositoryMock.Object, _tutorRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResponse()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var pets = new List<Pet>
        {
            new() { Id = 1, Nome = "Rex", Especie = "Cachorro", Raca = "Vira-lata", TutorId = 1 },
            new() { Id = 2, Nome = "Mingau", Especie = "Gato", Raca = "Persa", TutorId = 1 }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(page, pageSize))
            .ReturnsAsync((pets, 2));

        // Act
        var result = await _service.GetAllAsync(page, pageSize);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(page, result.Page);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WhenPetExists_ReturnsPetDetails()
    {
        // Arrange
        var id = 1;
        var pet = new Pet { Id = id, Nome = "Rex", Especie = "Cachorro", Tutor = new Tutor { Id = 1, Nome = "Tutor" } };
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(pet);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Rex", result.Nome);
        Assert.Equal("Tutor", result.TutorNome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPetDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var id = 99;
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Pet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(id));
    }

    [Fact]
    public async Task CreateAsync_WhenTutorDoesNotExist_ThrowsBusinessException()
    {
        // Arrange
        var dto = new PetRequestDto { Nome = "Rex", Especie = "Cachorro", Raca = "Labrador", TutorId = 99 };
        _tutorRepositoryMock.Setup(t => t.GetByIdAsync(dto.TutorId)).ReturnsAsync((Tutor?)null);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WhenSuccessful_ReturnsDetails()
    {
        // Arrange
        var dto = new PetRequestDto { Nome = "Rex", Especie = "Cachorro", Raca = "Labrador", TutorId = 1 };
        var tutor = new Tutor { Id = 1, Nome = "Tutor 1" };
        
        _tutorRepositoryMock.Setup(t => t.GetByIdAsync(dto.TutorId)).ReturnsAsync(tutor);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Pet>()))
            .ReturnsAsync((Pet p) => { p.Id = 5; return p; });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal("Rex", result.Nome);
        Assert.Equal("Tutor 1", result.TutorNome);
    }

    [Fact]
    public async Task UpdateAsync_WhenPetDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var id = 99;
        var dto = new PetRequestDto { Nome = "Rex", TutorId = 1 };
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Pet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(id, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenTutorDoesNotExist_ThrowsBusinessException()
    {
        // Arrange
        var id = 1;
        var pet = new Pet { Id = id, Nome = "Rex", TutorId = 1 };
        var dto = new PetRequestDto { Nome = "Rex Novo", TutorId = 99 };

        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(pet);
        _tutorRepositoryMock.Setup(t => t.GetByIdAsync(dto.TutorId)).ReturnsAsync((Tutor?)null);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(id, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenSuccessful_ReturnsDetails()
    {
        // Arrange
        var id = 1;
        var pet = new Pet { Id = id, Nome = "Rex", TutorId = 1 };
        var tutor = new Tutor { Id = 1, Nome = "Tutor 1" };
        var dto = new PetRequestDto { Nome = "Rex Alterado", Especie = "Cachorro", Raca = "Golden", TutorId = 1 };

        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(pet);
        _tutorRepositoryMock.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(tutor);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Pet>())).ReturnsAsync((Pet p) => p);

        // Act
        var result = await _service.UpdateAsync(id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Rex Alterado", result.Nome);
    }

    [Fact]
    public async Task DeleteAsync_WhenPetDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var id = 99;
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Pet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_WhenSuccessful_CallsRepositoryDelete()
    {
        // Arrange
        var id = 1;
        var pet = new Pet { Id = id, Nome = "Rex" };
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(pet);

        // Act
        await _service.DeleteAsync(id);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(pet), Times.Once);
    }
}
