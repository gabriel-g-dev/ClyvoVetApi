namespace ClyvoVetApi.DTOs.Response;

public class TutorResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public int TotalPets { get; set; }
}
