using ClyvoVetApi.DTOs.Validation;
using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.DTOs.Request;

public class VacinaRequestDto
{
    [Required(ErrorMessage = "Nome da vacina é obrigatório.")]
    [StringLength(50, ErrorMessage = "Nome da vacina deve ter no máximo 50 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data da vacina é obrigatória.")]
    [MinimumDate(1900, 1, 1, ErrorMessage = "Informe uma data válida a partir de 1900.")]
    public DateTime Data { get; set; }

    [Required(ErrorMessage = "Status da vacina é obrigatório.")]
    [RegularExpression("^[PA]$", ErrorMessage = "Status da vacina deve ser 'P' (Pendente) ou 'A' (Aplicada).")]
    public string Status { get; set; } = "P";

    [Required(ErrorMessage = "ID do pet é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = "O ID deve ser maior que zero.")]
    public int PetId { get; set; }
}
