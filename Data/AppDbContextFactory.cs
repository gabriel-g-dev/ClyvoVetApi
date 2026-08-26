using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClyvoVetApi.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConexaoDesignTime = "Data Source=design-time";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__OracleConnection")
            ?? ConexaoDesignTime;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseOracle(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
