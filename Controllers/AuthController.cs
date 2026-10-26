using ClyvoVetApi.Auth;
using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVetApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService service, ILogger<AuthController> logger) : ControllerBase
{
    private readonly IAuthService _service = service;
    private readonly ILogger<AuthController> _logger = logger;

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    public IActionResult Login([FromBody] LoginRequestDto dto)
    {
        var result = _service.Authenticate(dto);

        if (result == null)
        {
            _logger.LogWarning("Tentativa de login rejeitada.");
            return Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciais inválidas", detail: "Usuário ou senha incorretos.");
        }

        _logger.LogInformation("Login realizado com sucesso.");
        return Ok(result);
    }
}
