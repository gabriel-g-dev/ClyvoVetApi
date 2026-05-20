namespace ClyvoVetApi.DTOs.Response;

public class PetDetailsResponseDto : PetResponseDto
{
    public TutorResponseDto? Tutor { get; set; }
    public IEnumerable<ConsultaResponseDto> Consultas { get; set; } = [];
    public IEnumerable<VacinaResponseDto> Vacinas { get; set; } = [];
}
