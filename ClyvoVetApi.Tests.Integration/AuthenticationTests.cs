using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ClyvoVetApi.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class AuthenticationTests(IntegrationTestFixture fixture)
{
    private readonly IntegrationTestFixture _fixture = fixture;

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsToken()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = ClyvoVetWebApplicationFactory.Username,
            Password = ClyvoVetWebApplicationFactory.Password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(content);
        Assert.False(string.IsNullOrWhiteSpace(content.AccessToken));
        Assert.Equal("Bearer", content.TokenType);
        Assert.True(content.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WhenPasswordIsWrong_ReturnsUnauthorized()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = ClyvoVetWebApplicationFactory.Username,
            Password = "senha-errada"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenUserDoesNotExist_ReturnsUnauthorized()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "invasor",
            Password = ClyvoVetWebApplicationFactory.Password
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenBodyIsInvalid_ReturnsBadRequest()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenSuccessful_DoesNotLeakConfiguration()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = ClyvoVetWebApplicationFactory.Username,
            Password = ClyvoVetWebApplicationFactory.Password
        });

        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(ClyvoVetWebApplicationFactory.Password, body);
        Assert.DoesNotContain(ClyvoVetWebApplicationFactory.SigningKey, body);
        Assert.DoesNotContain("Username", body);
    }

    [Fact]
    public async Task PostPet_WhenTokenIsMissing_ReturnsUnauthorized()
    {
        _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/pets", NovoPet(1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostPet_WhenTokenIsMalformed_ReturnsUnauthorized()
    {
        var seed = _fixture.ResetDatabase();
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");

        var response = await client.PostAsJsonAsync("/api/pets", NovoPet(seed.DonoId));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostPet_WhenTokenIsExpired_ReturnsUnauthorized()
    {
        var seed = _fixture.ResetDatabase();
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ExpiredToken());

        var response = await client.PostAsJsonAsync("/api/pets", NovoPet(seed.DonoId));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostPet_WhenTokenIsSignedWithAnotherKey_ReturnsUnauthorized()
    {
        var seed = _fixture.ResetDatabase();
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenSignedWith(OutraChave()));

        var response = await client.PostAsJsonAsync("/api/pets", NovoPet(seed.DonoId));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostPet_WhenTokenIsValid_CreatesPet()
    {
        var seed = _fixture.ResetDatabase();
        var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/pets", NovoPet(seed.DonoId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetPets_WhenTokenIsMissing_RemainsPublic()
    {
        _fixture.ResetDatabase();
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/pets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static object NovoPet(int donoId) => new
    {
        nome = "Bidu",
        especie = "Cachorro",
        raca = "Poodle",
        dataNascimento = "2021-05-10",
        peso = 8.4m,
        tutorId = donoId
    };

    private static string ExpiredToken() =>
        TokenSignedWith(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ClyvoVetWebApplicationFactory.SigningKey)),
            DateTime.UtcNow.AddMinutes(-10));

    private static SymmetricSecurityKey OutraChave() =>
        new(RandomNumberGenerator.GetBytes(32));

    private static string TokenSignedWith(SymmetricSecurityKey key, DateTime? expires = null)
    {
        var token = new JwtSecurityToken(
            issuer: ClyvoVetWebApplicationFactory.Issuer,
            audience: ClyvoVetWebApplicationFactory.Audience,
            expires: expires ?? DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
