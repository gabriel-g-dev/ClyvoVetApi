using System.Diagnostics;
using ClyvoVetApi.Logging;
using ClyvoVetApi.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVetApi.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado", exception.Message),
            BusinessException => (StatusCodes.Status400BadRequest, "Violação de regra de negócio", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor", "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.")
        };

        var correlationId = context.Items[CorrelationIdMiddleware.PropertyName] as string
            ?? Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;

        var path = SensitiveDataMasker.MaskEmails(context.Request.Path);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Falha inesperada ao processar {Method} {Path}.",
                context.Request.Method,
                path);
        }
        else
        {
            _logger.LogWarning(
                "Requisição {Method} {Path} rejeitada com {StatusCode}: {Motivo}",
                context.Request.Method,
                path,
                statusCode,
                SensitiveDataMasker.MaskEmails(exception.Message));
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = $"{context.Request.Method} {context.Request.Path}"
        };

        problemDetails.Extensions["traceId"] = correlationId;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
