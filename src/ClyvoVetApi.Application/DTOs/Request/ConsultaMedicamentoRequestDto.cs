using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.DTOs.Request;

public class ConsultaMedicamentoRequestDto
{
    [Required(ErrorMessage = "ID da consulta é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = "O ID deve ser maior que zero.")]
    public int ConsultaId { get; set; }

    [Required(ErrorMessage = "ID do medicamento é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = "O ID deve ser maior que zero.")]
    public int MedicamentoId { get; set; }

    [Required(ErrorMessage = "Dosagem é obrigatória.")]
    [StringLength(100, ErrorMessage = "Dosagem deve ter no máximo 100 caracteres.")]
    public string Dosagem { get; set; } = string.Empty;
}
