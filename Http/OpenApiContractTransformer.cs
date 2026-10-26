using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace ClyvoVetApi.Http;

public sealed class OpenApiContractTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var parameter in operation.Parameters ?? [])
        {
            var description = context.Description.ParameterDescriptions.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (description is null)
                continue;

            // The .NET 9 exporter shares primitive schemas; later annotations can change earlier parameters.
            // Rebuild each primitive from its own MVC metadata instead of copying that shared schema.
            var schema = CreatePrimitiveSchema(description);
            if (schema is null)
                continue;

            foreach (var attribute in description.ModelMetadata?.ValidatorMetadata.OfType<ValidationAttribute>() ?? [])
            {
                switch (attribute)
                {
                    case StringLengthAttribute length:
                        schema.MinLength = length.MinimumLength;
                        schema.MaxLength = length.MaximumLength;
                        break;
                    case MinLengthAttribute minimum:
                        schema.MinLength = minimum.Length;
                        break;
                    case MaxLengthAttribute maximum when maximum.Length >= 0:
                        schema.MaxLength = maximum.Length;
                        break;
                    case RegularExpressionAttribute expression:
                        schema.Pattern = expression.Pattern;
                        break;
                    case RangeAttribute range:
                        schema.Minimum = Convert.ToDecimal(range.Minimum, CultureInfo.InvariantCulture);
                        schema.Maximum = Convert.ToDecimal(range.Maximum, CultureInfo.InvariantCulture);
                        break;
                    case EmailAddressAttribute:
                        schema.Format = "email";
                        break;
                }
            }
            schema.Default = GetDefaultValue(description) switch
            {
                string value => new OpenApiString(value),
                bool value => new OpenApiBoolean(value),
                int value => new OpenApiInteger(value),
                long value => new OpenApiLong(value),
                float value => new OpenApiFloat(value),
                double value => new OpenApiDouble(value),
                decimal value => new OpenApiDouble((double)value),
                DateTime value => new OpenApiDateTime(value),
                DateTimeOffset value => new OpenApiDateTime(value),
                _ => null
            };
            parameter.Schema = schema;
        }

        foreach (var (status, response) in operation.Responses ?? new OpenApiResponses())
        {
            if (!int.TryParse(status, out var code) || code is < 400 or > 599)
                continue;

            var schema = response.Content?.Values.Select(content => content.Schema).FirstOrDefault(value => value is not null)
                ?? new OpenApiSchema
                {
                    Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = nameof(ProblemDetails) }
                };
            response.Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new() { Schema = schema }
            };
        }
        return Task.CompletedTask;
    }

    private static OpenApiSchema? CreatePrimitiveSchema(ApiParameterDescription description)
    {
        var modelType = description.ModelMetadata?.ModelType ?? description.Type;
        var underlyingType = Nullable.GetUnderlyingType(modelType);
        var type = underlyingType ?? modelType;
        (string? Type, string? Format) primitive = Type.GetTypeCode(type) switch
        {
            TypeCode.String => ("string", null),
            TypeCode.Boolean => ("boolean", null),
            TypeCode.Int16 or TypeCode.Int32 or TypeCode.Byte => ("integer", "int32"),
            TypeCode.Int64 => ("integer", "int64"),
            TypeCode.Single => ("number", "float"),
            TypeCode.Double => ("number", "double"),
            TypeCode.Decimal => ("number", null),
            TypeCode.DateTime => ("string", "date-time"),
            _ when type == typeof(DateTimeOffset) => ("string", "date-time"),
            _ when type == typeof(Guid) => ("string", "uuid"),
            _ => (null, null)
        };
        return primitive.Type is null ? null : new OpenApiSchema
        {
            Type = primitive.Type,
            Format = primitive.Format,
            Nullable = underlyingType is not null || (type == typeof(string) && description.ModelMetadata?.IsRequired == false)
        };
    }

    private static object? GetDefaultValue(ApiParameterDescription description)
    {
        var metadata = description.ModelMetadata;
        if (metadata?.ContainerType is { IsAbstract: false } container
            && container.GetConstructor(Type.EmptyTypes) is not null
            && metadata.PropertyGetter is { } getter)
            return getter(Activator.CreateInstance(container)!);

        return description.DefaultValue;
    }
}
