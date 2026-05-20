namespace ClyvoVetApi.DTOs.Response;

public class TutorDetailsResponseDto : TutorResponseDto
{
    public IEnumerable<PetDto> Pets { get; set; } = [];
}
