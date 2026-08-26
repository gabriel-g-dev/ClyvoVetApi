using System.Text.RegularExpressions;

namespace ClyvoVetApi.Logging;

public static class SensitiveDataMasker
{
    private static readonly Regex EmailPattern =
        new(@"[\w\.\-\+]+@[\w\.\-]+\.\w+", RegexOptions.Compiled);

    public static string MaskEmails(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return EmailPattern.Replace(text, match => Mask(match.Value));
    }

    private static string Mask(string email)
    {
        var domain = email[(email.IndexOf('@') + 1)..];
        var topLevelDomain = domain[domain.LastIndexOf('.')..];

        return $"{email[0]}***@***{topLevelDomain}";
    }
}
