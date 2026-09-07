using ClyvoVetApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;

namespace ClyvoVetApi.Tests.Integration;

public class ClyvoVetWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string Username = "clyvovet-tests";
    public const string Password = "senha-de-teste-123";
    public const string SigningKey = "chave-de-teste-com-mais-de-32-caracteres-para-hmac";
    public const string Issuer = "ClyvoVetApi";
    public const string Audience = "ClyvoVetApiClients";

    private readonly SqliteConnection _connection;

    public ClyvoVetWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("Auth:Username", Username);
        builder.UseSetting("Auth:Password", Password);
        builder.UseSetting("Auth:SigningKey", SigningKey);
        builder.UseSetting("Auth:Issuer", Issuer);
        builder.UseSetting("Auth:Audience", Audience);
        builder.UseSetting("Auth:ExpirationMinutes", "60");
        builder.UseSetting("OpenTelemetry:OtlpEndpoint", string.Empty);
        builder.UseSetting("OpenTelemetry:ConsoleExporter", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ILoggerFactory>();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

            services.RemoveAll<Serilog.ILogger>();
            services.AddSingleton<Serilog.ILogger>(SilentLogger());

            var oracleConfiguration = services.Single(
                d => d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>));

            services.Remove(oracleConfiguration);
            ConfigureDatabase(services);
        });
    }

    protected virtual void ConfigureDatabase(IServiceCollection services) =>
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

    private static Serilog.ILogger SilentLogger() =>
        new LoggerConfiguration().CreateLogger();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }
}
