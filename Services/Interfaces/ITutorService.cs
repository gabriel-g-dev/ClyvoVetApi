using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;

namespace ClyvoVetApi.Services.Interfaces;

public interface ITutorService
{
    Task<PagedResponseDto<TutorResponseDto>> GetAllAsync(int page, int pageSize);
    Task<TutorDetailsResponseDto> GetByIdAsync(int id);
    Task<TutorDetailsResponseDto> GetByEmailAsync(string email);
    Task<TutorDetailsResponseDto> CreateAsync(TutorRequestDto dto);
    Task<TutorDetailsResponseDto> UpdateAsync(int id, TutorRequestDto dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<PetDto>> GetPetsByTutorIdAsync(int tutorId);
}
