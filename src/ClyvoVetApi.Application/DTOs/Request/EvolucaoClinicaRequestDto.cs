using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.DTOs.Request;

public class EvolucaoClinicaRequestDto
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Texto { get; set; } = string.Empty;

    [MaxLength(10)]
    public string[] Tags { get; set; } = [];
}
