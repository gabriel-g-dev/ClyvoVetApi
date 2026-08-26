using System.IdentityModel.Tokens.Jwt;
using ClyvoVetApi.Auth;
using ClyvoVetApi.DTOs.Request;
using Xunit;

namespace ClyvoVetApi.Tests.Unit.Auth;

public class AuthServiceTests
{
    private const string UsuarioValido = "clyvovet";
    private const string SenhaValida = "senha-de-teste-123";

    private readonly AuthOptions _options = new()
    {
        Issuer = "ClyvoVetApi",
        Audience = "ClyvoVetApiClients",
        ExpirationMinutes = 45,
        SigningKey = "chave-de-teste-com-mais-de-32-caracteres-para-hmac",
        Username = UsuarioValido,
        Password = SenhaValida
    };

    private static LoginRequestDto Login(string username, string password) =>
        new() { Username = username, Password = password };

    [Fact]
    public void Authenticate_WhenCredentialsAreValid_ReturnsBearerToken()
    {
        var service = new AuthService(_options);

        var resultado = service.Authenticate(Login(UsuarioValido, SenhaValida));

        Assert.NotNull(resultado);
        Assert.Equal("Bearer", resultado.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(resultado.AccessToken));
    }

    [Fact]
    public void Authenticate_WhenPasswordIsWrong_ReturnsNull()
    {
        var service = new AuthService(_options);

        var resultado = service.Authenticate(Login(UsuarioValido, "senha-errada"));

        Assert.Null(resultado);
    }

    [Fact]
    public void Authenticate_WhenUsernameIsWrong_ReturnsNull()
    {
        var service = new AuthService(_options);

        var resultado = service.Authenticate(Login("invasor", SenhaValida));

        Assert.Null(resultado);
    }

    [Fact]
    public void Authenticate_WhenPasswordDiffersOnlyInCase_ReturnsNull()
    {
        var service = new AuthService(_options);

        var resultado = service.Authenticate(Login(UsuarioValido, SenhaValida.ToUpperInvariant()));

        Assert.Null(resultado);
    }

    [Theory]
    [InlineData("", SenhaValida)]
    [InlineData(UsuarioValido, "")]
    [InlineData("", "")]
    public void Authenticate_WhenConfiguredCredentialsAreMissing_ReturnsNull(string usuario, string senha)
    {
        var options = new AuthOptions
        {
            SigningKey = _options.SigningKey,
            Username = usuario,
            Password = senha
        };
        var service = new AuthService(options);

        var resultado = service.Authenticate(Login(usuario, senha));

        Assert.Null(resultado);
    }

    [Fact]
    public void Authenticate_WhenCredentialsAreValid_UsesExpirationFromOptions()
    {
        var service = new AuthService(_options);
        var antes = DateTime.UtcNow;

        var resultado = service.Authenticate(Login(UsuarioValido, SenhaValida));

        Assert.NotNull(resultado);
        var esperado = antes.AddMinutes(_options.ExpirationMinutes);
        Assert.InRange(resultado.ExpiresAt, esperado.AddSeconds(-5), esperado.AddSeconds(5));
    }

    [Fact]
    public void Authenticate_WhenCredentialsAreValid_IssuesTokenWithConfiguredIssuerAndAudience()
    {
        var service = new AuthService(_options);

        var resultado = service.Authenticate(Login(UsuarioValido, SenhaValida));

        Assert.NotNull(resultado);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado.AccessToken);
        Assert.Equal(_options.Issuer, token.Issuer);
        Assert.Contains(_options.Audience, token.Audiences);
        Assert.Equal(UsuarioValido, token.Subject);
    }

    [Fact]
    public void Authenticate_WhenCalledTwice_IssuesTokensWithDistinctIdentifiers()
    {
        var service = new AuthService(_options);
        var handler = new JwtSecurityTokenHandler();

        var primeiro = service.Authenticate(Login(UsuarioValido, SenhaValida));
        var segundo = service.Authenticate(Login(UsuarioValido, SenhaValida));

        Assert.NotNull(primeiro);
        Assert.NotNull(segundo);

        var jtiPrimeiro = handler.ReadJwtToken(primeiro.AccessToken).Id;
        var jtiSegundo = handler.ReadJwtToken(segundo.AccessToken).Id;

        Assert.NotEqual(jtiPrimeiro, jtiSegundo);
    }

    [Fact]
    public void Authenticate_WhenCredentialsAreValid_DoesNotLeakPasswordIntoToken()
    {
        var service = new AuthService(_options);

        var resultado = service.Authenticate(Login(UsuarioValido, SenhaValida));

        Assert.NotNull(resultado);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado.AccessToken);
        Assert.DoesNotContain(token.Claims, claim => claim.Value == SenhaValida);
    }
}
