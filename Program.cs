using System.Security.Cryptography;
using System.Text;
using ClyvoVetApi.Auth;
using ClyvoVetApi.Data;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.HealthChecks;
using ClyvoVetApi.Http;
using ClyvoVetApi.Logging;
using ClyvoVetApi.Middleware;
using ClyvoVetApi.Mongo;
using ClyvoVetApi.Observability;
using ClyvoVetApi.Repositories;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services;
using ClyvoVetApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection;
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

if (builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

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
            .AddAspNetCoreInstrumentation(options => options.RecordException = false)
            .AddSource(Telemetry.ServiceName)
            .AddProcessor(new SafeTelemetryProcessor());

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
else if (Encoding.UTF8.GetByteCount(authOptions.SigningKey) < 32)
    throw new InvalidOperationException("Auth:SigningKey deve conter pelo menos 32 bytes para HMAC-SHA256.");

if (authOptions.ExpirationMinutes is < 1 or > 1440)
    throw new InvalidOperationException("Auth:ExpirationMinutes deve estar entre 1 e 1440.");
if (authOptions.Username.Length > 100)
    throw new InvalidOperationException("Auth:Username deve conter no máximo 100 caracteres para identificar o autor clínico.");

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
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.Headers.WWWAuthenticate = "Bearer";
                await Results.Problem(statusCode: 401, title: "Autenticação necessária",
                    detail: "Informe um token Bearer válido.",
                    extensions: new Dictionary<string, object?> { ["traceId"] = context.HttpContext.TraceIdentifier })
                    .ExecuteAsync(context.HttpContext);
            },
            OnForbidden = context => Results.Problem(statusCode: 403, title: "Acesso negado",
                detail: "O token não possui permissão de acesso clínico.",
                extensions: new Dictionary<string, object?> { ["traceId"] = context.HttpContext.TraceIdentifier })
                .ExecuteAsync(context.HttpContext)
        };
    });

builder.Services.AddAuthorization(options => options.AddPolicy("ClinicalAccess", policy =>
    policy.RequireAuthenticatedUser().RequireClaim("scope", "clinical")));

builder.Services.AddControllers(options => options.Filters.Add<HateoasResultFilter>());
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
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.Models.OpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Obtenha o token em POST /api/auth/login e informe Authorization: Bearer {token}."
        };
        return Task.CompletedTask;
    });
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any())
        {
            operation.Security = [new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                [new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = []
            }];
            operation.Responses.TryAdd("401", new() { Description = "Token ausente, inválido ou expirado." });
            operation.Responses.TryAdd("403", new() { Description = "Token sem a permissão clínica." });
        }
        return Task.CompletedTask;
    });
    options.AddOperationTransformer<OpenApiContractTransformer>();
});

var connectionString = builder.Configuration.GetConnectionString("OracleConnection");

if (string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        "ConnectionStrings:OracleConnection nao configurada. Defina a variavel de ambiente "
        + "ConnectionStrings__OracleConnection antes de iniciar a aplicacao (veja .env.example).");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseOracle(connectionString,
    oracle => oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion21)));

builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("Aplicação ASP.NET Core em execução."),
        tags: ["live"])
    .AddCheck<OracleHealthCheck>("oracle", tags: ["ready"])
    .AddCheck<MongoHealthCheck>("mongo", tags: ["ready"]);

var mongoOptions = builder.Configuration.GetSection("Mongo").Get<MongoOptions>() ?? new MongoOptions();
mongoOptions.Validate();
builder.Services.AddSingleton(mongoOptions);
builder.Services.AddSingleton<MongoClinicalContext>();
builder.Services.AddScoped<IEvolucaoClinicaRepository, MongoEvolucaoClinicaRepository>();
builder.Services.AddScoped<IEvolucaoClinicaService, EvolucaoClinicaService>();

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
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.Items[CorrelationIdMiddleware.PropertyName]
        ?? context.HttpContext.TraceIdentifier);

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
