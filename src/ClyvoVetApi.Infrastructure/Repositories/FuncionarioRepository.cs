using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVetApi.Repositories;

public class FuncionarioRepository(AppDbContext context) : IFuncionarioRepository
{
    private readonly AppDbContext _context = context;

    public Task<(IEnumerable<Funcionario> Items, int Total)> GetAllAsync(int page, int pageSize) =>
        GetAllAsync(page, pageSize, new ListQueryOptions());

    public async Task<(IEnumerable<Funcionario> Items, int Total)> GetAllAsync(int page, int pageSize, ListQueryOptions options)
    {
        options.Validate(page, pageSize, "id", "nome", "email", "setor", "cargo");
        IQueryable<Funcionario> query = _context.Funcionarios
            .AsNoTracking();
        if (options.Search is not null)
        {
            var search = options.Search.ToLowerInvariant();
            query = query.Where(f => f.Nome.ToLower().Contains(search) || f.Email.ToLower().Contains(search) || f.Setor.ToLower().Contains(search) || f.Cargo.ToLower().Contains(search));
        }

        var total = await query.CountAsync();
        var ordered = options.SortBy switch
        {
            "nome" => QueryOrdering.By(query, f => f.Nome, options.Descending),
            "email" => QueryOrdering.By(query, f => f.Email, options.Descending),
            "setor" => QueryOrdering.By(query, f => f.Setor, options.Descending),
            "cargo" => QueryOrdering.By(query, f => f.Cargo, options.Descending),
            _ => QueryOrdering.By(query, f => f.Id, options.Descending)
        };
        var items = await ordered.ThenBy(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<Funcionario?> GetByIdAsync(int id) =>
        await _context.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == id);

    public async Task<Funcionario?> GetByEmailAsync(string email) =>
        await _context.Funcionarios
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Email == email);

    public async Task<IEnumerable<Funcionario>> GetBySetorAsync(string setor) =>
        await _context.Funcionarios
            .AsNoTracking()
            .Where(f => f.Setor.ToLower() == setor.ToLower())
            .ToListAsync();

    public async Task<IEnumerable<Funcionario>> GetByCargoAsync(string cargo) =>
        await _context.Funcionarios
            .AsNoTracking()
            .Where(f => f.Cargo.ToLower() == cargo.ToLower())
            .ToListAsync();

    public async Task<Funcionario> CreateAsync(Funcionario funcionario)
    {
        _context.Funcionarios.Add(funcionario);
        await _context.SaveChangesAsync();
        return funcionario;
    }

    public async Task<Funcionario> UpdateAsync(Funcionario funcionario)
    {
        _context.Funcionarios.Update(funcionario);
        await _context.SaveChangesAsync();
        return funcionario;
    }

    public async Task DeleteAsync(Funcionario funcionario)
    {
        _context.Funcionarios.Remove(funcionario);
        await _context.SaveChangesAsync();
    }
}
