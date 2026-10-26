using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVetApi.Repositories;

public class DonoRepository(AppDbContext context) : IDonoRepository
{
    private readonly AppDbContext _context = context;

    public Task<(IEnumerable<Dono> Items, int Total)> GetAllAsync(int page, int pageSize) =>
        GetAllAsync(page, pageSize, new ListQueryOptions());

    public async Task<(IEnumerable<Dono> Items, int Total)> GetAllAsync(int page, int pageSize, ListQueryOptions options)
    {
        options.Validate(page, pageSize, "id", "nome", "email");
        IQueryable<Dono> query = _context.Donos
            .Include(d => d.Pets)
            .AsNoTracking();
        if (options.Search is not null)
        {
            var search = options.Search.ToLowerInvariant();
            query = query.Where(d => d.Nome.ToLower().Contains(search) || d.Email.ToLower().Contains(search));
        }

        var total = await query.CountAsync();
        var ordered = options.SortBy switch
        {
            "nome" => QueryOrdering.By(query, d => d.Nome, options.Descending),
            "email" => QueryOrdering.By(query, d => d.Email, options.Descending),
            _ => QueryOrdering.By(query, d => d.Id, options.Descending)
        };
        var items = await ordered.ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<Dono?> GetByIdAsync(int id) =>
        await _context.Donos
            .Include(d => d.Pets)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<Dono?> GetByEmailAsync(string email) =>
        await _context.Donos
            .Include(d => d.Pets)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Email == email);

    public async Task<Dono> CreateAsync(Dono dono)
    {
        _context.Donos.Add(dono);
        await _context.SaveChangesAsync();
        return dono;
    }

    public async Task<Dono> UpdateAsync(Dono dono)
    {
        _context.Donos.Update(dono);
        await _context.SaveChangesAsync();
        return dono;
    }

    public async Task DeleteAsync(Dono dono)
    {
        _context.Donos.Remove(dono);
        await _context.SaveChangesAsync();
    }
}
