using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.DTOs.Request;

public class EvolucaoClinicaQueryDto
{
    [Range(1, 1000000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [StringLength(40, MinimumLength = 1)]
    public string? Tag { get; set; }

    public DateTimeOffset? Inicio { get; set; }
    public DateTimeOffset? Fim { get; set; }

    [RegularExpression("^(asc|desc)$", ErrorMessage = "A direção deve ser asc ou desc.")]
    public string Direction { get; set; } = "desc";
}
