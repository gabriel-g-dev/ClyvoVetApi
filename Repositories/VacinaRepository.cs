using ClyvoVetApi.Data;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVetApi.Repositories;

public class VacinaRepository(AppDbContext context) : IVacinaRepository
{
    private readonly AppDbContext _context = context;

    public async Task<(IEnumerable<Vacina> Items, int Total)> GetAllAsync(int page, int pageSize)
    {
        var query = _context.Vacinas.Include(v => v.Pet).AsNoTracking();
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<Vacina?> GetByIdAsync(int id) =>
        await _context.Vacinas
            .Include(v => v.Pet)
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<IEnumerable<Vacina>> GetPendentesAsync() =>
        await _context.Vacinas
            .Include(v => v.Pet)
            .AsNoTracking()
            .Where(v => !v.Aplicada)
            .ToListAsync();

    public async Task<IEnumerable<Vacina>> GetByNomeAsync(string nome) =>
        await _context.Vacinas
            .Include(v => v.Pet)
            .AsNoTracking()
            .Where(v => v.Nome.ToLower().Contains(nome.ToLower()))
            .ToListAsync();

    public async Task<IEnumerable<Vacina>> GetProximasAsync(DateTime limite) =>
        await _context.Vacinas
            .Include(v => v.Pet)
            .AsNoTracking()
            .Where(v => v.ProximaDose <= limite && !v.Aplicada)
            .ToListAsync();

    public async Task<Vacina> CreateAsync(Vacina vacina)
    {
        _context.Vacinas.Add(vacina);
        await _context.SaveChangesAsync();
        return vacina;
    }

    public async Task<Vacina> UpdateAsync(Vacina vacina)
    {
        _context.Vacinas.Update(vacina);
        await _context.SaveChangesAsync();
        return vacina;
    }

    public async Task DeleteAsync(Vacina vacina)
    {
        _context.Vacinas.Remove(vacina);
        await _context.SaveChangesAsync();
    }
}
