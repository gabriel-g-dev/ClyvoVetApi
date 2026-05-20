using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.DTOs.Request;

public class ConsultaRequestDto
{
    [Required(ErrorMessage = "Data da consulta é obrigatória.")]
    public DateTime Data { get; set; }

    [Required(ErrorMessage = "Descrição da consulta é obrigatória.")]
    [StringLength(250, ErrorMessage = "Descrição deve ter no máximo 250 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Veterinário é obrigatório.")]
    [StringLength(100, ErrorMessage = "Veterinário deve ter no máximo 100 caracteres.")]
    public string Veterinario { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres.")]
    public string? Observacoes { get; set; }

    [Required(ErrorMessage = "ID do pet é obrigatório.")]
    public int PetId { get; set; }
}
