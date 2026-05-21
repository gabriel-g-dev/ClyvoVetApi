using ClyvoVetApi.Data;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVetApi.Repositories;

public class TutorRepository(AppDbContext context) : ITutorRepository
{
    private readonly AppDbContext _context = context;

    public async Task<(IEnumerable<Tutor> Items, int Total)> GetAllAsync(int page, int pageSize)
    {
        var total = await _context.Tutores.CountAsync();
        var items = await _context.Tutores
            .Include(t => t.Pets)
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<Tutor?> GetByIdAsync(int id) =>
        await _context.Tutores
            .Include(t => t.Pets)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Tutor?> GetByEmailAsync(string email) =>
        await _context.Tutores
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Email == email);

    public async Task<Tutor> CreateAsync(Tutor tutor)
    {
        _context.Tutores.Add(tutor);
        await _context.SaveChangesAsync();
        return tutor;
    }

    public async Task<Tutor> UpdateAsync(Tutor tutor)
    {
        _context.Tutores.Update(tutor);
        await _context.SaveChangesAsync();
        return tutor;
    }

    public async Task DeleteAsync(Tutor tutor)
    {
        _context.Tutores.Remove(tutor);
        await _context.SaveChangesAsync();
    }
}
