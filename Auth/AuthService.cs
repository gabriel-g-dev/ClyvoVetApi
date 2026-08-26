using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using Microsoft.IdentityModel.Tokens;

namespace ClyvoVetApi.Auth;

public class AuthService(AuthOptions options) : IAuthService
{
    private readonly AuthOptions _options = options;

    public LoginResponseDto? Authenticate(LoginRequestDto request)
    {
        if (!CredentialsMatch(request.Username, request.Password))
            return null;

        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, request.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            expires: expiresAt,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new LoginResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt
        };
    }

    private bool CredentialsMatch(string username, string password)
    {
        if (string.IsNullOrEmpty(_options.Username) || string.IsNullOrEmpty(_options.Password))
            return false;

        return AreEqual(username, _options.Username) && AreEqual(password, _options.Password);
    }

    private static bool AreEqual(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right));
}
