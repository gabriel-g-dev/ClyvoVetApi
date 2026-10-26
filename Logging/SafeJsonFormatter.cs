using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Parsing;

namespace ClyvoVetApi.Logging;

/// <summary>Applies the same output policy to application and framework JSON log events.</summary>
public sealed class SafeJsonFormatter : ITextFormatter
{
    private readonly JsonFormatter _formatter = new();
    private static readonly MessageTemplate FailureTemplate = new MessageTemplateParser()
        .Parse("Falha {ExceptionType} registrada por {SourceContext}.");
    private static readonly HashSet<string> SafeExceptionProperties = new(StringComparer.Ordinal)
    {
        "SourceContext", "CorrelationId", "Method", "Path", "RequestMethod", "RequestPath",
        "RequestId", "EventId", "ActionId", "ActionName", "ConnectionId", "ContextType"
    };
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "pwd", "signingkey", "accesstoken", "access_token", "authorization", "secret",
        "connectionstring", "querystring", "username"
    };

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var hasException = logEvent.Exception is not null;
        var properties = logEvent.Properties
            .Where(property => !hasException || SafeExceptionProperties.Contains(property.Key))
            .Select(property => SanitizeProperty(property.Key, property.Value)).ToList();
        if (hasException)
            properties.Add(new LogEventProperty("ExceptionType", new ScalarValue(logEvent.Exception!.GetType().FullName)));

        var sanitized = new LogEvent(logEvent.Timestamp, logEvent.Level,
            hasException ? new SafeLoggedException(logEvent.Exception!) : null,
            hasException ? FailureTemplate : new MessageTemplateParser().Parse(SensitiveDataMasker.MaskSensitiveText(logEvent.MessageTemplate.Text)),
            properties, logEvent.TraceId ?? default, logEvent.SpanId ?? default);
        _formatter.Format(sanitized, output);
    }

    private static LogEventPropertyValue Sanitize(LogEventPropertyValue value) => value switch
    {
        ScalarValue { Value: string text } => new ScalarValue(SensitiveDataMasker.MaskSensitiveText(text)),
        ScalarValue { Value: Exception exception } => new ScalarValue(exception.GetType().FullName),
        SequenceValue sequence => new SequenceValue(sequence.Elements.Select(Sanitize)),
        StructureValue structure => new StructureValue(structure.Properties
            .Select(property => SanitizeProperty(property.Name, property.Value)), structure.TypeTag),
        DictionaryValue dictionary => new DictionaryValue(dictionary.Elements.Select(pair =>
            new KeyValuePair<ScalarValue, LogEventPropertyValue>((ScalarValue)Sanitize(pair.Key),
                pair.Key.Value is string key && SensitiveProperties.Contains(key) ? new ScalarValue("[redacted]") : Sanitize(pair.Value)))),
        _ => value
    };

    private static LogEventProperty SanitizeProperty(string name, LogEventPropertyValue value) =>
        new(name, SensitiveProperties.Contains(name) ? new ScalarValue("[redacted]") : Sanitize(value));

    // Exception messages/Data can contain SQL, credentials or clinical text. Retain types and call stacks only.
    private sealed class SafeLoggedException : Exception
    {
        private readonly string _safeStack;

        public SafeLoggedException(Exception original)
        {
            var frames = new List<string>();
            for (var current = original; current is not null && frames.Count < 8; current = current.InnerException)
                frames.Add($"{current.GetType().FullName}\n{current.StackTrace}");
            _safeStack = SensitiveDataMasker.MaskSensitiveText(string.Join("\nCaused by: ", frames));
        }

        public override string ToString() => _safeStack;
    }
}
