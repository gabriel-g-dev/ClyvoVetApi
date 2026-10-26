namespace ClyvoVetApi.DTOs.Response;

public class EvolucaoClinicaResponseDto : ResourceResponseDto
{
    public string Id { get; set; } = string.Empty;
    public int PetId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public DateTime RegistradoEmUtc { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = [];
}
