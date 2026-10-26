using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.DTOs.Request;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Usuário é obrigatório.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    public string Password { get; set; } = string.Empty;
}
