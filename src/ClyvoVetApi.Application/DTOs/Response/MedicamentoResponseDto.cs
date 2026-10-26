namespace ClyvoVetApi.DTOs.Response;

public class MedicamentoResponseDto : ResourceResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
}
