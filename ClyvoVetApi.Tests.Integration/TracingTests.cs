using Xunit;

namespace ClyvoVetApi.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class TracingTests(IntegrationTestFixture fixture)
{
    private readonly IntegrationTestFixture _fixture = fixture;

    [Fact]
    public async Task GetIntelligencePreventiva_WhenCalled_CreatesSpansForServiceAndRepository()
    {
        var seed = _fixture.ResetDatabase();
        using var collector = new SpanCollector();
        var client = _fixture.CreateClient();

        await client.GetAsync($"/api/pets/{seed.PetId}/inteligencia-preventiva");

        Assert.Contains(collector.Spans, span => span.DisplayName == "IntelligenceService.GetIntelligencePreventiva");
        Assert.Contains(collector.Spans, span => span.DisplayName == "PetRepository.GetById");
    }

    [Fact]
    public async Task GetIntelligencePreventiva_WhenCalled_SharesTraceIdAcrossLayers()
    {
        var seed = _fixture.ResetDatabase();
        using var collector = new SpanCollector();
        var client = _fixture.CreateClient();

        await client.GetAsync($"/api/pets/{seed.PetId}/inteligencia-preventiva");

        var request = collector.Single("GET api/Pets/{id:int}/inteligencia-preventiva");
        var service = collector.Single("IntelligenceService.GetIntelligencePreventiva");
        var repository = collector.Single("PetRepository.GetById");

        Assert.Equal(request.TraceId, service.TraceId);
        Assert.Equal(request.TraceId, repository.TraceId);
        Assert.Equal(request.SpanId, service.ParentSpanId);
        Assert.Equal(service.SpanId, repository.ParentSpanId);
    }

    [Fact]
    public async Task GetVacinasPendentes_WhenCalled_CreatesSpansForServiceAndRepository()
    {
        _fixture.ResetDatabase();
        using var collector = new SpanCollector();
        var client = _fixture.CreateClient();

        await client.GetAsync("/api/vacinas/pendentes");

        var service = collector.Single("VacinaService.GetPendentes");
        var repository = collector.Single("VacinaRepository.GetPendentes");

        Assert.Equal(service.TraceId, repository.TraceId);
        Assert.Equal(service.SpanId, repository.ParentSpanId);
    }

    [Fact]
    public async Task GetPet_WhenClientSendsCorrelationId_TagsTheRequestSpan()
    {
        var seed = _fixture.ResetDatabase();
        using var collector = new SpanCollector();
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "trace-com-correlacao");

        await client.GetAsync($"/api/pets/{seed.PetId}");

        var request = collector.Single("GET api/Pets/{id:int}");
        Assert.Equal("trace-com-correlacao", request.GetTagItem("clyvovet.correlation_id"));
    }

    [Fact]
    public async Task GetPet_WhenCalled_DoesNotTagSpansWithPersonalData()
    {
        var seed = _fixture.ResetDatabase();
        using var collector = new SpanCollector();
        var client = _fixture.CreateClient();

        await client.GetAsync($"/api/pets/{seed.PetId}");

        var valores = collector.Spans
            .SelectMany(span => span.Tags)
            .Select(tag => tag.Value ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(valores, valor => valor.Contains('@'));
        Assert.DoesNotContain(valores, valor => valor.Contains(ClyvoVetWebApplicationFactory.Password));
        Assert.DoesNotContain(valores, valor => valor.Contains(ClyvoVetWebApplicationFactory.SigningKey));
    }
}
