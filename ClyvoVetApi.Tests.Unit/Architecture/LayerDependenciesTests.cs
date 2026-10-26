using ClyvoVetApi.Models;
using ClyvoVetApi.Services;

namespace ClyvoVetApi.Tests.Unit.Architecture;

public class LayerDependenciesTests
{
    [Fact]
    public void DomainAndApplication_DoNotReferenceWebOrPersistenceFrameworks()
    {
        var assemblies = new[] { typeof(Pet).Assembly, typeof(PetService).Assembly };

        var dependencies = assemblies.SelectMany(assembly => assembly.GetReferencedAssemblies()).Select(name => name.Name ?? "");

        Assert.DoesNotContain(dependencies, name => name.StartsWith("Microsoft.AspNetCore")
            || name.StartsWith("Microsoft.EntityFrameworkCore") || name.StartsWith("Oracle") || name.StartsWith("MongoDB"));
        Assert.NotEqual(assemblies[0], assemblies[1]);
    }
}
