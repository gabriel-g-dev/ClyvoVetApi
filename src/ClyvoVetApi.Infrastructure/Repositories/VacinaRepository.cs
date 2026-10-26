using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;
using ClyvoVetApi.Observability;
using ClyvoVetApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVetApi.Repositories;

public class VacinaRepository(AppDbContext context) : IVacinaRepository
{
    private readonly AppDbContext _context = context;

    public Task<(IEnumerable<Vacina> Items, int Total)> GetAllAsync(int page, int pageSize) =>
        GetAllAsync(page, pageSize, new ListQueryOptions());

    public async Task<(IEnumerable<Vacina> Items, int Total)> GetAllAsync(int page, int pageSize, ListQueryOptions options)
    {
        options.Validate(page, pageSize, "id", "nome", "data", "status");
        IQueryable<Vacina> query = _context.Vacinas
            .Include(v => v.Pet)
            .AsNoTracking();
        if (options.Search is not null)
        {
            var search = options.Search.ToLowerInvariant();
            query = query.Where(v => v.Nome.ToLower().Contains(search));
        }

        var total = await query.CountAsync();
        var ordered = options.SortBy switch
        {
            "nome" => QueryOrdering.By(query, v => v.Nome, options.Descending),
            "data" => QueryOrdering.By(query, v => v.Data, options.Descending),
            "status" => QueryOrdering.By(query, v => v.Status, options.Descending),
            _ => QueryOrdering.By(query, v => v.Id, options.Descending)
        };
        var items = await ordered.ThenBy(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<Vacina?> GetByIdAsync(int id) =>
        await _context.Vacinas
            .Include(v => v.Pet)
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<IEnumerable<Vacina>> GetPendentesAsync()
    {
        using var activity = Telemetry.Source.StartActivity("VacinaRepository.GetPendentes");
        activity?.SetTag("db.system", "oracle");

        var vacinas = await _context.Vacinas
            .Include(v => v.Pet)
            .AsNoTracking()
            .Where(v => v.Status == "P")
            .ToListAsync();

        activity?.SetTag("vacinas.count", vacinas.Count);
        return vacinas;
    }

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
            .Where(v => v.Data <= limite && v.Status == "P")
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
