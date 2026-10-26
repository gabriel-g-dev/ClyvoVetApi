using System.Collections;
using ClyvoVetApi.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace ClyvoVetApi.Http;

public sealed class HateoasResultFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (!HttpMethods.IsGet(context.HttpContext.Request.Method)
            || context.Result is not ObjectResult { StatusCode: null or 200 } result)
            return;

        var request = context.HttpContext.Request;
        string Path(string value) => $"{request.PathBase}{value}";
        var self = $"{request.PathBase}{request.Path}{request.QueryString}";
        context.HttpContext.Response.Headers.Link = $"<{self}>; rel=\"self\"";

        if (result.Value is IPagedResponse page)
        {
            var paginationParameter = context.ActionDescriptor.Parameters.FirstOrDefault(parameter =>
                parameter.ParameterType.GetProperty(nameof(IPagedResponse.Page)) is not null
                && parameter.ParameterType.GetProperty(nameof(IPagedResponse.PageSize)) is not null);
            var paginationPrefix = paginationParameter is not null
                && request.Query.Keys.Any(key => key.StartsWith($"{paginationParameter.Name}.", StringComparison.OrdinalIgnoreCase))
                ? $"{paginationParameter.Name}."
                : string.Empty;

            string PageUrl(int number)
            {
                var query = request.Query.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
                query[$"{paginationPrefix}page"] = new StringValues(number.ToString(System.Globalization.CultureInfo.InvariantCulture));
                query[$"{paginationPrefix}pageSize"] = new StringValues(page.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return $"{request.PathBase}{request.Path}{QueryString.Create(query)}";
            }

            var lastPage = Math.Max(1, page.TotalPages);
            page.Links = [new(self, "self"), new(PageUrl(1), "first"), new(PageUrl(lastPage), "last")];
            if (page.Page > 1)
                page.Links.Add(new(PageUrl(Math.Min(page.Page - 1, lastPage)), "prev"));
            if (page.Page < page.TotalPages)
                page.Links.Add(new(PageUrl(page.Page + 1), "next"));
            foreach (var item in page.Items)
                Enrich(item, Path);
        }
        else if (result.Value is IEnumerable items and not string)
        {
            var materialized = items.Cast<object>().ToList();
            foreach (var item in materialized)
                Enrich(item, Path);
            result.Value = materialized;
        }
        else
        {
            Enrich(result.Value, Path);
            if (result.Value is HealthDashboardResponseDto dashboard)
                dashboard.Links = [new(self, "self"), new(Path($"/api/pets/{context.RouteData.Values["id"]}"), "pet")];
        }
    }

    private static void Enrich(object? value, Func<string, string> path)
    {
        if (value is not ResourceResponseDto resource)
            return;

        (string? Self, string? Collection) routes = value switch
        {
            PetResponseDto p => ($"/api/pets/{p.Id}", "/api/pets"),
            PetDto p => ($"/api/pets/{p.Id}", "/api/pets"),
            DonoResponseDto d => ($"/api/tutores/{d.Id}", "/api/tutores"),
            FuncionarioResponseDto f => ($"/api/funcionarios/{f.Id}", "/api/funcionarios"),
            ConsultaResponseDto c => ($"/api/consultas/{c.Id}", "/api/consultas"),
            VacinaResponseDto v => ($"/api/vacinas/{v.Id}", "/api/vacinas"),
            MedicamentoResponseDto m => ($"/api/medicamentos/{m.Id}", "/api/medicamentos"),
            ConsultaMedicamentoResponseDto c => ($"/api/consultamedicamentos/{c.Id}", "/api/consultamedicamentos"),
            EvolucaoClinicaResponseDto e => ($"/api/pets/{e.PetId}/evolucoes/{e.Id}", $"/api/pets/{e.PetId}/evolucoes"),
            _ => (null, null)
        };
        if (routes.Self is null || routes.Collection is null)
            return;

        resource.Links = [new(path(routes.Self), "self"), new(path(routes.Collection), "collection")];
        switch (value)
        {
            case PetResponseDto p:
                resource.Links.AddRange([
                    new(path($"/api/tutores/{p.DonoId}"), "tutor"),
                    new(path($"/api/pets/{p.Id}/consultas"), "consultas"),
                    new(path($"/api/pets/{p.Id}/vacinas"), "vacinas"),
                    new(path($"/api/pets/{p.Id}/evolucoes"), "evolucoes"),
                    new(path($"/api/pets/{p.Id}/inteligencia-preventiva"), "inteligencia-preventiva")]);
                if (p is PetDetailsResponseDto details)
                {
                    Enrich(details.Dono, path);
                    details.Consultas = details.Consultas.ToList();
                    details.Vacinas = details.Vacinas.ToList();
                    foreach (var c in details.Consultas) Enrich(c, path);
                    foreach (var v in details.Vacinas) Enrich(v, path);
                }
                break;
            case PetDto p:
                resource.Links.Add(new(path($"/api/tutores/{p.DonoId}"), "tutor"));
                break;
            case DonoResponseDto d:
                resource.Links.Add(new(path($"/api/tutores/{d.Id}/pets"), "pets"));
                if (d is DonoDetailsResponseDto donoDetails)
                {
                    donoDetails.Pets = donoDetails.Pets.ToList();
                    foreach (var p in donoDetails.Pets) Enrich(p, path);
                }
                break;
            case EvolucaoClinicaResponseDto e:
                resource.Links.Add(new(path($"/api/pets/{e.PetId}"), "pet"));
                break;
            case ConsultaResponseDto c:
                resource.Links.AddRange([
                    new(path($"/api/pets/{c.PetId}"), "pet"),
                    new(path($"/api/funcionarios/{c.FuncionarioId}"), "funcionario"),
                    new(path($"/api/consultamedicamentos/consulta/{c.Id}"), "prescricoes")]);
                break;
            case VacinaResponseDto v:
                resource.Links.Add(new(path($"/api/pets/{v.PetId}"), "pet"));
                break;
            case ConsultaMedicamentoResponseDto c:
                resource.Links.AddRange([
                    new(path($"/api/consultas/{c.ConsultaId}"), "consulta"),
                    new(path($"/api/medicamentos/{c.MedicamentoId}"), "medicamento")]);
                break;
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
