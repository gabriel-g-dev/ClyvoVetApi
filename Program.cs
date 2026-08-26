using System.Security.Cryptography;
using System.Text;
using ClyvoVetApi.Auth;
using ClyvoVetApi.Data;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.HealthChecks;
using ClyvoVetApi.Logging;
using ClyvoVetApi.Middleware;
using ClyvoVetApi.Observability;
using ClyvoVetApi.Repositories;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using ClyvoVetApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
var useConsoleExporter = builder.Configuration.GetValue<bool>("OpenTelemetry:ConsoleExporter");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(Telemetry.ServiceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddSource(Telemetry.ServiceName);

        if (useConsoleExporter)
            tracing.AddConsoleExporter();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(Telemetry.ServiceName);

        if (useConsoleExporter)
            metrics.AddConsoleExporter();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
    });

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
var signingKeyWasGenerated = string.IsNullOrWhiteSpace(authOptions.SigningKey);

if (signingKeyWasGenerated)
    authOptions.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

builder.Services.AddSingleton(authOptions);
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authOptions.Issuer,
            ValidAudience = authOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "ClyvoVet API",
            Version = "v1",
            Description = "API para gestão da jornada contínua de saúde do pet - FIAP Challenge 2026"
        };
        return Task.CompletedTask;
    });
});

var connectionString = builder.Configuration.GetConnectionString("OracleConnection");

if (string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        "ConnectionStrings:OracleConnection nao configurada. Defina a variavel de ambiente "
        + "ConnectionStrings__OracleConnection antes de iniciar a aplicacao (veja .env.example).");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseOracle(connectionString));

builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("Aplicação ASP.NET Core em execução."),
        tags: ["live"])
    .AddCheck<OracleHealthCheck>("oracle", tags: ["ready"]);

builder.Services.AddScoped<IDonoRepository, DonoRepository>();
builder.Services.AddScoped<IFuncionarioRepository, FuncionarioRepository>();
builder.Services.AddScoped<IMedicamentoRepository, MedicamentoRepository>();
builder.Services.AddScoped<IConsultaMedicamentoRepository, ConsultaMedicamentoRepository>();
builder.Services.AddScoped<IPetRepository, PetRepository>();
builder.Services.AddScoped<IConsultaRepository, ConsultaRepository>();
builder.Services.AddScoped<IVacinaRepository, VacinaRepository>();

builder.Services.AddScoped<IDonoService, DonoService>();
builder.Services.AddScoped<IFuncionarioService, FuncionarioService>();
builder.Services.AddScoped<IMedicamentoService, MedicamentoService>();
builder.Services.AddScoped<IConsultaMedicamentoService, ConsultaMedicamentoService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<IVacinaService, VacinaService>();
builder.Services.AddScoped<IIntelligenceService, IntelligenceService>();

if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<AlertScheduler>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestMetricsMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "Requisição {RequestMethod} {RequestPath} concluída com {StatusCode} em {Elapsed:0.0000} ms";

    options.GetMessageTemplateProperties = (httpContext, requestPath, elapsedMs, statusCode) =>
    [
        new("RequestMethod", new ScalarValue(httpContext.Request.Method)),
        new("RequestPath", new ScalarValue(SensitiveDataMasker.MaskEmails(requestPath))),
        new("StatusCode", new ScalarValue(statusCode)),
        new("Elapsed", new ScalarValue(elapsedMs))
    ];
});

app.UseExceptionHandler();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

if (signingKeyWasGenerated)
    app.Logger.LogWarning("Auth:SigningKey nao configurada. Uma chave temporaria foi gerada e os tokens serao invalidados ao reiniciar a aplicacao.");

app.Logger.LogInformation("ClyvoVet API iniciada no ambiente {Environment}.", app.Environment.EnvironmentName);

app.Run();

public partial class Program;
