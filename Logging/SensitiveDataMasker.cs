using System.Text.RegularExpressions;

namespace ClyvoVetApi.Logging;

public static class SensitiveDataMasker
{
    private static readonly Regex EmailPattern =
        new(@"[\w.\-+%]+(?:@|%40)[\w.\-%]+\.\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CredentialPattern = new(
        @"\b(password|pwd|signingkey|access_token|accesstoken|authorization)\s*[:=]\s*[^;\r\n,]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex QueryParameterPattern = new(
        @"(?<=[?&])([A-Za-z0-9_.-]+)=[^&#\s]*", RegexOptions.Compiled);

    public static string MaskEmails(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return EmailPattern.Replace(text, match => Mask(Uri.UnescapeDataString(match.Value)));
    }

    public static string MaskSensitiveText(string? text) =>
        QueryParameterPattern.Replace(CredentialPattern.Replace(MaskEmails(text), "$1=[redacted]"), "$1=[redacted]");

    private static string Mask(string email)
    {
        var domain = email[(email.IndexOf('@') + 1)..];
        var topLevelDomain = domain[domain.LastIndexOf('.')..];

        return $"{email[0]}***@***{topLevelDomain}";
    }
}
