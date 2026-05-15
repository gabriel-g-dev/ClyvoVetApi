using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsultasController : ControllerBase
{
    private readonly AppDbContext _context;

    public ConsultasController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lista todas as consultas
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Consulta>>> GetConsultas()
    {
        return Ok(await _context.Consultas.Include(c => c.Pet).ToListAsync());
    }

    /// <summary>
    /// Busca uma consulta pelo seu ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Consulta>> GetConsulta(int id)
    {
        var consulta = await _context.Consultas.Include(c => c.Pet)
                                               .FirstOrDefaultAsync(c => c.Id == id);
        if (consulta == null)
            return NotFound(new { mensagem = "Consulta não encontrada." });

        return Ok(consulta);
    }

    /// <summary>
    /// Busca consultas por veterinário
    /// </summary>
    [HttpGet("veterinario/{nome}")]
    public async Task<ActionResult> GetConsultasByVeterinario(string nome)
    {
        var consultas = await _context.Consultas.Include(c => c.Pet)
                                                .Where(c => c.Veterinario.ToLower()
                                                .Contains(nome.ToLower()))
                                                .ToListAsync();
        return Ok(consultas);
    }

    /// <summary>
    /// Busca consultas por período
    /// </summary>
    [HttpGet("periodo")]
    public async Task<ActionResult> GetConsultasByPeriodo(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim)
    {
        var consultas = await _context.Consultas.Include(c => c.Pet)
                                                .Where(c => c.Data >= inicio && c.Data <= fim)
                                                .ToListAsync();
        return Ok(consultas);
    }

    /// <summary>
    /// Cadastra uma nova consulta
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Consulta>> PostConsulta(Consulta consulta)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetConsulta), new { id = consulta.Id }, consulta);
    }

    /// <summary>
    /// Atualiza as informações de uma consulta
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> PutConsulta(int id, Consulta consulta)
    {
        if (id != consulta.Id)
            return BadRequest(new { mensagem = "ID da URL diferente do corpo da requisição." });

        _context.Entry(consulta).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Consultas.Any(c => c.Id == id))
                return NotFound(new { mensagem = "Consulta não encontrada." });
            throw;
        }

        return NoContent();
    }

    /// <summary>
    /// Remove uma consulta pelo seu ID
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConsulta(int id)
    {
        var consulta = await _context.Consultas.FindAsync(id);
        if (consulta == null)
            return NotFound(new { mensagem = "Consulta não encontrada." });

        _context.Consultas.Remove(consulta);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}