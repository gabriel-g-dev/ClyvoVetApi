using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.DTOs.Request;

public class TutorRequestDto
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
    [StringLength(150, ErrorMessage = "E-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefone é obrigatório.")]
    [Phone(ErrorMessage = "Telefone em formato inválido.")]
    [StringLength(20, ErrorMessage = "Telefone deve ter no máximo 20 caracteres.")]
    public string Telefone { get; set; } = string.Empty;
}
