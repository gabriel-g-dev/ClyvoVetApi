using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using Moq;
using Xunit;

namespace ClyvoVetApi.Tests.Services;

public class TutorServiceTests
{
    private readonly Mock<ITutorRepository> _repositoryMock;
    private readonly TutorService _service;

    public TutorServiceTests()
    {
        _repositoryMock = new Mock<ITutorRepository>();
        _service = new TutorService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResponse()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var tutores = new List<Tutor>
        {
            new() { Id = 1, Nome = "Tutor 1", Email = "tutor1@email.com", Telefone = "11999999999" },
            new() { Id = 2, Nome = "Tutor 2", Email = "tutor2@email.com", Telefone = "11988888888" }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(page, pageSize))
            .ReturnsAsync((tutores, 2));

    // Act
    var result = await _service.GetAllAsync(page, pageSize);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(page, result.Page);
    Assert.Equal(pageSize, result.PageSize);
    Assert.Equal(2, result.TotalItems);
    Assert.Equal(2, result.Data.Count());
}

[Fact]
public async Task GetByIdAsync_WhenTutorExists_ReturnsTutorDetails()
{
    // Arrange
    var id = 1;
    var tutor = new Tutor { Id = id, Nome = "Tutor 1", Email = "tutor1@email.com" };
    _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(tutor);

    // Act
    var result = await _service.GetByIdAsync(id);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(id, result.Id);
    Assert.Equal("Tutor 1", result.Nome);
}

[Fact]
public async Task GetByIdAsync_WhenTutorDoesNotExist_ThrowsNotFoundException()
{
    // Arrange
    var id = 99;
    _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Tutor?)null);

    // Act & Assert
    await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(id));
}

[Fact]
public async Task GetByEmailAsync_WhenTutorExists_ReturnsTutorDetails()
{
    // Arrange
    var email = "tutor1@email.com";
    var tutor = new Tutor { Id = 1, Nome = "Tutor 1", Email = email };
    _repositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(tutor);

    // Act
    var result = await _service.GetByEmailAsync(email);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(email, result.Email);
}

[Fact]
public async Task GetByEmailAsync_WhenTutorDoesNotExist_ThrowsNotFoundException()
{
    // Arrange
    var email = "notfound@email.com";
    _repositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((Tutor?)null);

    // Act & Assert
    await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByEmailAsync(email));
}

[Fact]
public async Task CreateAsync_WhenEmailAlreadyExists_ThrowsBusinessException()
{
    // Arrange
    var dto = new TutorRequestDto { Nome = "Novo", Email = "tutor1@email.com", Telefone = "11999999999" };
    _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(new Tutor());

    // Act & Assert
    await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
}

[Fact]
public async Task CreateAsync_WhenSuccessful_ReturnsDetails()
{
    // Arrange
    var dto = new TutorRequestDto { Nome = "Novo Tutor", Email = "novo@email.com", Telefone = "11999999999" };
    _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Tutor?)null);
    _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Tutor>()))
        .ReturnsAsync((Tutor t) => { t.Id = 10; return t; });

    // Act
    var result = await _service.CreateAsync(dto);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(10, result.Id);
    Assert.Equal("Novo Tutor", result.Nome);
}

[Fact]
public async Task UpdateAsync_WhenTutorDoesNotExist_ThrowsNotFoundException()
{
    // Arrange
    var id = 99;
    var dto = new TutorRequestDto { Nome = "Nome", Email = "email@email.com", Telefone = "123" };
    _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Tutor?)null);

    // Act & Assert
    await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(id, dto));
}

[Fact]
public async Task UpdateAsync_WhenEmailAlreadyExistsForAnotherTutor_ThrowsBusinessException()
{
    // Arrange
    var id = 1;
    var tutor = new Tutor { Id = id, Nome = "Tutor 1", Email = "tutor1@email.com" };
    var dto = new TutorRequestDto { Nome = "Tutor Alterado", Email = "outro@email.com" };
    
    _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(tutor);
    _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(new Tutor { Id = 2, Email = dto.Email });

    // Act & Assert
    await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(id, dto));
}

[Fact]
public async Task UpdateAsync_WhenSuccessful_ReturnsDetails()
{
    // Arrange
    var id = 1;
    var tutor = new Tutor { Id = id, Nome = "Tutor 1", Email = "tutor1@email.com" };
    var dto = new TutorRequestDto { Nome = "Tutor 1 Alterado", Email = "tutor1@email.com", Telefone = "11999999999" };

    _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(tutor);
    _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Tutor>())).ReturnsAsync((Tutor t) => t);

    // Act
    var result = await _service.UpdateAsync(id, dto);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Tutor 1 Alterado", result.Nome);
}

[Fact]
public async Task DeleteAsync_WhenTutorDoesNotExist_ThrowsNotFoundException()
{
    // Arrange
    var id = 99;
    _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Tutor?)null);

    // Act & Assert
    await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(id));
}

[Fact]
public async Task DeleteAsync_WhenSuccessful_CallsRepositoryDelete()
{
    // Arrange
    var id = 1;
    var tutor = new Tutor { Id = id, Nome = "Tutor 1" };
    _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(tutor);

    // Act
    await _service.DeleteAsync(id);

    // Assert
    _repositoryMock.Verify(r => r.DeleteAsync(tutor), Times.Once);
}
}
