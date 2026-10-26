using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVetApi.Repositories;

public class ConsultaMedicamentoRepository(AppDbContext context) : IConsultaMedicamentoRepository
{
    private readonly AppDbContext _context = context;

    public Task<(IEnumerable<ConsultaMedicamento> Items, int Total)> GetAllAsync(int page, int pageSize) =>
        GetAllAsync(page, pageSize, new ListQueryOptions());

    public async Task<(IEnumerable<ConsultaMedicamento> Items, int Total)> GetAllAsync(int page, int pageSize, ListQueryOptions options)
    {
        options.Validate(page, pageSize, "id", "consultaid", "medicamentoid", "dosagem");
        IQueryable<ConsultaMedicamento> query = _context.ConsultasMedicamentos
            .Include(cm => cm.Consulta)
            .Include(cm => cm.Medicamento)
            .AsNoTracking();
        if (options.Search is not null)
        {
            var search = options.Search.ToLowerInvariant();
            query = query.Where(cm => cm.Dosagem.ToLower().Contains(search) || (cm.Medicamento != null && cm.Medicamento.Nome.ToLower().Contains(search)));
        }

        var total = await query.CountAsync();
        var ordered = options.SortBy switch
        {
            "consultaid" => QueryOrdering.By(query, cm => cm.ConsultaId, options.Descending),
            "medicamentoid" => QueryOrdering.By(query, cm => cm.MedicamentoId, options.Descending),
            "dosagem" => QueryOrdering.By(query, cm => cm.Dosagem, options.Descending),
            _ => QueryOrdering.By(query, cm => cm.Id, options.Descending)
        };
        var items = await ordered.ThenBy(cm => cm.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<ConsultaMedicamento?> GetByIdAsync(int id) =>
        await _context.ConsultasMedicamentos
            .Include(cm => cm.Consulta)
            .Include(cm => cm.Medicamento)
            .FirstOrDefaultAsync(cm => cm.Id == id);

    public async Task<IEnumerable<ConsultaMedicamento>> GetByConsultaIdAsync(int consultaId) =>
        await _context.ConsultasMedicamentos
            .Include(cm => cm.Consulta)
            .Include(cm => cm.Medicamento)
            .AsNoTracking()
            .Where(cm => cm.ConsultaId == consultaId)
            .ToListAsync();

    public async Task<ConsultaMedicamento> CreateAsync(ConsultaMedicamento consultaMedicamento)
    {
        _context.ConsultasMedicamentos.Add(consultaMedicamento);
        await _context.SaveChangesAsync();
        return consultaMedicamento;
    }

    public async Task<ConsultaMedicamento> UpdateAsync(ConsultaMedicamento consultaMedicamento)
    {
        _context.ConsultasMedicamentos.Update(consultaMedicamento);
        await _context.SaveChangesAsync();
        return consultaMedicamento;
    }

    public async Task DeleteAsync(ConsultaMedicamento consultaMedicamento)
    {
        _context.ConsultasMedicamentos.Remove(consultaMedicamento);
        await _context.SaveChangesAsync();
    }
}
