namespace ClyvoVetApi.DTOs.Response;

public class PetResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raca { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public int TutorId { get; set; }
    public string TutorNome { get; set; } = string.Empty;
}
