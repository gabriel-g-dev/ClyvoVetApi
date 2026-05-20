using ClyvoVetApi.Models;

namespace ClyvoVetApi.Repositories.Interfaces;

public interface ITutorRepository
{
    Task<(IEnumerable<Tutor> Items, int Total)> GetAllAsync(int page, int pageSize);
    Task<Tutor?> GetByIdAsync(int id);
    Task<Tutor?> GetByEmailAsync(string email);
    Task<Tutor> CreateAsync(Tutor tutor);
    Task<Tutor> UpdateAsync(Tutor tutor);
    Task DeleteAsync(Tutor tutor);
}
