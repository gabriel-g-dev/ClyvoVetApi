using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using ClyvoVetApi.DTOs.Request;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting;

namespace ClyvoVetApi.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class TelemetryPrivacyTests
{
    private const string EmailCanary = "private.audit+canary@example.test";

    [Theory]
    [InlineData("tutores")]
    [InlineData("funcionarios")]
    public async Task EmailLookup_DoesNotExportTheEmailInRequestSpanOrLogs(string resource)
    {
        using var factory = new PrivacyFactory();
        using var client = await EvolucoesEndpointsTests.CreateAuthenticatedClientAsync(factory);
        await EvolucoesEndpointsTests.CreatePetAsync(factory);
        using var collector = new SpanCollector();

        var response = await client.GetAsync($"/api/{resource}/email/{Uri.EscapeDataString(EmailCanary)}");
        var request = await collector.WaitForAsync(resource == "tutores"
            ? "GET api/tutores/email/{email}" : "GET api/Funcionarios/email/{email}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var exported = string.Join('\n', request.TagObjects.Select(tag => $"{tag.Key}={tag.Value}"));
        AssertDoesNotContainEmail(exported);
        Assert.Contains("url.path", request.TagObjects.Select(tag => tag.Key));
        Assert.NotEmpty(factory.Logs.Lines);
        AssertDoesNotContainEmail(string.Join('\n', factory.Logs.Lines));
    }

    [Fact]
    public async Task RejectedLogin_DoesNotWriteTheSubmittedUsernameOrPasswordToLogs()
    {
        using var factory = new PrivacyFactory();
        using var client = factory.CreateClient();
        const string passwordCanary = "private-password-canary-93841";

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = EmailCanary, Password = passwordCanary
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var logs = string.Join('\n', factory.Logs.Lines);
        Assert.Contains("login rejeitada", logs, StringComparison.OrdinalIgnoreCase);
        AssertDoesNotContainEmail(logs);
        Assert.DoesNotContain(passwordCanary, logs);
    }

    [Fact]
    public async Task UnexpectedException_DoesNotWriteItsSensitiveMessageToLogsOrResponse()
    {
        using var factory = new PrivacyFactory();
        using var client = factory.CreateClient();
        const string secretCanary = "private-exception-secret-71283";
        using var collector = new SpanCollector();

        var response = await client.GetAsync($"/__privacy/exception?email={Uri.EscapeDataString(EmailCanary)}&secret={secretCanary}");
        var body = await response.Content.ReadAsStringAsync();
        var span = await collector.WaitForAsync("GET __privacy/exception");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        AssertDoesNotContainEmail(body);
        Assert.DoesNotContain(secretCanary, body);
        var logs = string.Join('\n', factory.Logs.Lines);
        Assert.Contains("InvalidOperationException", logs);
        Assert.Contains("CorrelationId", logs);
        AssertDoesNotContainEmail(logs);
        Assert.DoesNotContain(secretCanary, logs);
        var telemetry = string.Join('\n', span.TagObjects.Select(tag => $"{tag.Key}={tag.Value}"))
            + span.StatusDescription + string.Join('\n', span.Events.SelectMany(item => item.Tags).Select(tag => $"{tag.Key}={tag.Value}"));
        Assert.Equal(System.Diagnostics.ActivityStatusCode.Error, span.Status);
        AssertDoesNotContainEmail(telemetry);
        Assert.DoesNotContain(secretCanary, telemetry);
    }

    private static void AssertDoesNotContainEmail(string output)
    {
        Assert.DoesNotContain(EmailCanary, output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Uri.EscapeDataString(EmailCanary), output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private.audit", output, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class PrivacyFactory : ClyvoVetWebApplicationFactory
    {
        public JsonLogCollector Logs { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices((context, services) =>
            {
                // Exercise the formatter selected by production configuration, including framework logs.
                var type = Type.GetType(context.Configuration["Serilog:WriteTo:0:Args:formatter"]!, throwOnError: true)!;
                var constructor = type.GetConstructors().First(candidate => candidate.GetParameters().All(parameter => parameter.IsOptional));
                Logs.Formatter = (ITextFormatter)constructor.Invoke(constructor.GetParameters().Select(parameter => parameter.DefaultValue).ToArray());
                var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(Logs).CreateLogger();
                services.RemoveAll<ILoggerFactory>();
                services.AddSingleton<ILoggerFactory>(new SerilogLoggerFactory(logger, dispose: false));
                services.RemoveAll<Serilog.ILogger>();
                services.AddSingleton<Serilog.ILogger>(logger);
                services.AddControllers().AddApplicationPart(typeof(PrivacyProbeController).Assembly);
            });
        }
    }

    private sealed class JsonLogCollector : ILogEventSink
    {
        public ITextFormatter Formatter { get; set; } = null!;
        public ConcurrentQueue<string> Lines { get; } = new();

        public void Emit(LogEvent logEvent)
        {
            using var writer = new StringWriter();
            Formatter.Format(logEvent, writer);
            Lines.Enqueue(writer.ToString());
        }
    }
}

[ApiController]
public class PrivacyProbeController : ControllerBase
{
    [HttpGet("/__privacy/exception")]
    public IActionResult Throw(string email, string secret) =>
        throw new InvalidOperationException($"A dependency failed for {email}; Password={secret}");
}
