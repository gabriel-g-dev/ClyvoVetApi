using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;
using ClyvoVetApi.Observability;
using ClyvoVetApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVetApi.Repositories;

public class PetRepository(AppDbContext context) : IPetRepository
{
    private readonly AppDbContext _context = context;

    public Task<(IEnumerable<Pet> Items, int Total)> GetAllAsync(int page, int pageSize) =>
        GetAllAsync(page, pageSize, new ListQueryOptions());

    public async Task<(IEnumerable<Pet> Items, int Total)> GetAllAsync(int page, int pageSize, ListQueryOptions options)
    {
        options.Validate(page, pageSize, "id", "nome", "especie", "raca", "datanascimento", "peso");
        IQueryable<Pet> query = _context.Pets
            .Include(p => p.Dono)
            .AsNoTracking();
        if (options.Search is not null)
        {
            var search = options.Search.ToLowerInvariant();
            query = query.Where(p => p.Nome.ToLower().Contains(search) || p.Especie.ToLower().Contains(search) || p.Raca.ToLower().Contains(search));
        }

        var total = await query.CountAsync();
        var ordered = options.SortBy switch
        {
            "nome" => QueryOrdering.By(query, p => p.Nome, options.Descending),
            "especie" => QueryOrdering.By(query, p => p.Especie, options.Descending),
            "raca" => QueryOrdering.By(query, p => p.Raca, options.Descending),
            "datanascimento" => QueryOrdering.By(query, p => p.DataNascimento, options.Descending),
            "peso" => QueryOrdering.By(query, p => p.Peso, options.Descending),
            _ => QueryOrdering.By(query, p => p.Id, options.Descending)
        };
        var items = await ordered.ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<Pet?> GetByIdAsync(int id)
    {
        using var activity = Telemetry.Source.StartActivity("PetRepository.GetById");
        activity?.SetTag("db.system", "oracle");
        activity?.SetTag("pet.id", id);

        return await _context.Pets
            .Include(p => p.Dono)
            .Include(p => p.Consultas)
                .ThenInclude(c => c.Funcionario)
            .Include(p => p.Vacinas)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Pet>> GetByEspecieAsync(string especie) =>
        await _context.Pets
            .Include(p => p.Dono)
            .AsNoTracking()
            .Where(p => p.Especie.ToLower() == especie.ToLower())
            .ToListAsync();

    public async Task<IEnumerable<Pet>> GetByRacaAsync(string raca) =>
        await _context.Pets
            .Include(p => p.Dono)
            .AsNoTracking()
            .Where(p => p.Raca.ToLower() == raca.ToLower())
            .ToListAsync();

    public async Task<Pet> CreateAsync(Pet pet)
    {
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();
        return pet;
    }

    public async Task<Pet> UpdateAsync(Pet pet)
    {
        _context.Pets.Update(pet);
        await _context.SaveChangesAsync();
        return pet;
    }

    public async Task DeleteAsync(Pet pet)
    {
        _context.Pets.Remove(pet);
        await _context.SaveChangesAsync();
    }
}
