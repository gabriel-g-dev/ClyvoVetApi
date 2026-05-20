namespace ClyvoVetApi.DTOs.Response;

public class VacinaResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataAplicacao { get; set; }
    public DateTime? ProximaDose { get; set; }
    public bool Aplicada { get; set; }
    public int PetId { get; set; }
    public string PetNome { get; set; } = string.Empty;
}
