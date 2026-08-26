using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;

namespace ClyvoVetApi.Auth;

public interface IAuthService
{
    LoginResponseDto? Authenticate(LoginRequestDto request);
}
