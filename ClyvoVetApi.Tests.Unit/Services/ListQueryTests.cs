using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using Moq;

namespace ClyvoVetApi.Tests.Unit.Services;

public class ListQueryTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(1_000_001, 10)]
    public async Task PetService_WhenPageIsInvalid_RejectsBeforeCallingRepository(int page, int size)
    {
        var repository = new Mock<IPetRepository>(MockBehavior.Strict);
        var service = new PetService(repository.Object, Mock.Of<IDonoRepository>());

        await Assert.ThrowsAsync<BusinessException>(() => service.GetAllAsync(page, size));

        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PetService_WhenQueryIsValid_NormalizesAndPassesOptionsToRepository()
    {
        var repository = new Mock<IPetRepository>();
        var options = new ListQueryOptions { Search = " Gato ", SortBy = " NOME ", Descending = true };
        repository.Setup(r => r.GetAllAsync(2, 5, options)).ReturnsAsync((new List<Pet> { new() { Id = 8, Nome = "Mingau" } }, 6));
        var service = new PetService(repository.Object, Mock.Of<IDonoRepository>());

        var result = await service.GetAllAsync(2, 5, options);

        Assert.Equal("Gato", options.Search);
        Assert.Equal("nome", options.SortBy);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(8, Assert.Single(result.Data).Id);
        repository.Verify(r => r.GetAllAsync(2, 5, options), Times.Once);
    }

    [Theory]
    [InlineData("senha")]
    [InlineData("nome desc")]
    [InlineData("nome;drop table pets")]
    public async Task PetService_WhenSortIsNotAllowed_RejectsBeforeCallingRepository(string field)
    {
        var repository = new Mock<IPetRepository>(MockBehavior.Strict);
        var service = new PetService(repository.Object, Mock.Of<IDonoRepository>());

        await Assert.ThrowsAsync<BusinessException>(() => service.GetAllAsync(1, 10, new ListQueryOptions { SortBy = field }));

        repository.VerifyNoOtherCalls();
    }
}
