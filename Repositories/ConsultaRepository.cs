using ClyvoVetApi.Data;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVetApi.Repositories;

public class ConsultaRepository(AppDbContext context) : IConsultaRepository
{
    private readonly AppDbContext _context = context;

    public async Task<(IEnumerable<Consulta> Items, int Total)> GetAllAsync(int page, int pageSize)
    {
        var total = await _context.Consultas.CountAsync();
        var items = await _context.Consultas
            .Include(c => c.Pet)
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<Consulta?> GetByIdAsync(int id) =>
        await _context.Consultas
            .Include(c => c.Pet)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<Consulta>> GetByVeterinarioAsync(string nome) =>
        await _context.Consultas
            .Include(c => c.Pet)
            .AsNoTracking()
            .Where(c => c.Veterinario.ToLower().Contains(nome.ToLower()))
            .ToListAsync();

    public async Task<IEnumerable<Consulta>> GetByPeriodoAsync(DateTime inicio, DateTime fim) =>
        await _context.Consultas
            .Include(c => c.Pet)
            .AsNoTracking()
            .Where(c => c.Data >= inicio && c.Data <= fim)
            .ToListAsync();

    public async Task<Consulta> CreateAsync(Consulta consulta)
    {
        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync();
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
