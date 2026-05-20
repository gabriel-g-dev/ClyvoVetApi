namespace ClyvoVetApi.DTOs.Response;

public class ConsultaResponseDto
{
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Veterinario { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
    public int PetId { get; set; }
    public string PetNome { get; set; } = string.Empty;
}
