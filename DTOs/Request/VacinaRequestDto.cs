using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.DTOs.Request;

public class VacinaRequestDto
{
    [Required(ErrorMessage = "Nome da vacina é obrigatório.")]
    [StringLength(100, ErrorMessage = "Nome da vacina deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data de aplicação é obrigatória.")]
    public DateTime DataAplicacao { get; set; }

    public DateTime? ProximaDose { get; set; }

    [Required(ErrorMessage = "Indicação de aplicação é obrigatória.")]
    public bool Aplicada { get; set; }

    [Required(ErrorMessage = "ID do pet é obrigatório.")]
    public int PetId { get; set; }
}
