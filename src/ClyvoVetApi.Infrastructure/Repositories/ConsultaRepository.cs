using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;
using ClyvoVetApi.Observability;
using ClyvoVetApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVetApi.Repositories;

public class ConsultaRepository(AppDbContext context) : IConsultaRepository
{
    private readonly AppDbContext _context = context;

    public Task<(IEnumerable<Consulta> Items, int Total)> GetAllAsync(int page, int pageSize) =>
        GetAllAsync(page, pageSize, new ListQueryOptions());

    public async Task<(IEnumerable<Consulta> Items, int Total)> GetAllAsync(int page, int pageSize, ListQueryOptions options)
    {
        options.Validate(page, pageSize, "id", "data", "tipo", "status", "valor");
        IQueryable<Consulta> query = _context.Consultas
            .Include(c => c.Pet)
            .Include(c => c.Funcionario)
            .AsNoTracking();
        if (options.Search is not null)
        {
            var search = options.Search.ToLowerInvariant();
            query = query.Where(c => c.Tipo.ToLower().Contains(search) || c.Descricao.ToLower().Contains(search));
        }

        var total = await query.CountAsync();
        var ordered = options.SortBy switch
        {
            "data" => QueryOrdering.By(query, c => c.Data, options.Descending),
            "tipo" => QueryOrdering.By(query, c => c.Tipo, options.Descending),
            "status" => QueryOrdering.By(query, c => c.Status, options.Descending),
            "valor" => QueryOrdering.By(query, c => c.Valor, options.Descending),
            _ => QueryOrdering.By(query, c => c.Id, options.Descending)
        };
        var items = await ordered.ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<Consulta?> GetByIdAsync(int id) =>
        await _context.Consultas
            .Include(c => c.Pet)
            .Include(c => c.Funcionario)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<Consulta>> GetByFuncionarioIdAsync(int funcionarioId) =>
        await _context.Consultas
            .Include(c => c.Pet)
            .Include(c => c.Funcionario)
            .AsNoTracking()
            .Where(c => c.FuncionarioId == funcionarioId)
            .ToListAsync();

    public async Task<IEnumerable<Consulta>> GetByPeriodoAsync(DateTime inicio, DateTime fim) =>
        await _context.Consultas
            .Include(c => c.Pet)
            .Include(c => c.Funcionario)
            .AsNoTracking()
            .Where(c => c.Data >= inicio && c.Data <= fim)
            .ToListAsync();

    public async Task<Consulta> CreateAsync(Consulta consulta)
    {
        using var activity = Telemetry.Source.StartActivity("ConsultaRepository.Create");
        activity?.SetTag("db.system", "oracle");

        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync();

        activity?.SetTag("consulta.id", consulta.Id);
        return consulta;
    }

    public async Task<Consulta> UpdateAsync(Consulta consulta)
    {
        _context.Consultas.Update(consulta);
        await _context.SaveChangesAsync();
        return consulta;
    }

    public async Task DeleteAsync(Consulta consulta)
    {
        _context.Consultas.Remove(consulta);
        await _context.SaveChangesAsync();
    }
}
