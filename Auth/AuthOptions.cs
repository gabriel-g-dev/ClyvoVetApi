namespace ClyvoVetApi.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "ClyvoVetApi";
    public string Audience { get; set; } = "ClyvoVetApiClients";
    public int ExpirationMinutes { get; set; } = 60;
    public string SigningKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
