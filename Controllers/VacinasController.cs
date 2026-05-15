using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VacinasController : ControllerBase
{
    private readonly AppDbContext _context;

    public VacinasController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lista todas as vacinas
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vacina>>> GetVacinas()
    {
        return Ok(await _context.Vacinas.Include(v => v.Pet).ToListAsync());
    }

    /// <summary>
    /// Busca uma vacina pelo seu ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Vacina>> GetVacina(int id)
    {
        var vacina = await _context.Vacinas.Include(v => v.Pet)
                                           .FirstOrDefaultAsync(v => v.Id == id);
        if (vacina == null)
            return NotFound(new { mensagem = "Vacina não encontrada." });

        return Ok(vacina);
    }

    /// <summary>
    /// Lista vacinas pendentes
    /// </summary>
    [HttpGet("pendentes")]
    public async Task<ActionResult> GetVacinasPendentes()
    {
        var vacinas = await _context.Vacinas.Include(v => v.Pet)
                                            .Where(v => !v.Aplicada)
                                            .ToListAsync();
        return Ok(vacinas);
    }

    /// <summary>
    /// Busca vacinas pelo nome
    /// </summary>
    [HttpGet("nome/{nome}")]
    public async Task<ActionResult> GetVacinasByNome(string nome)
    {
        var vacinas = await _context.Vacinas.Include(v => v.Pet)
                                            .Where(v => v.Nome.ToLower()
                                            .Contains(nome.ToLower()))
                                            .ToListAsync();
        return Ok(vacinas);
    }

    /// <summary>
    /// Lista as próximas vacinas dentro de um número de dias
    /// </summary>
    [HttpGet("proximas")]
    public async Task<ActionResult> GetProximasVacinas([FromQuery] int dias = 30)
    {
        var limite = DateTime.Now.AddDays(dias);
        var vacinas = await _context.Vacinas.Include(v => v.Pet)
                                            .Where(v => v.ProximaDose <= limite && !v.Aplicada)
                                            .ToListAsync();
        return Ok(vacinas);
    }

    /// <summary>
    /// Cadastra uma nova vacina
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Vacina>> PostVacina(Vacina vacina)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _context.Vacinas.Add(vacina);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVacina), new { id = vacina.Id }, vacina);
    }

    /// <summary>
    /// Atualiza as informações de uma vacina
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> PutVacina(int id, Vacina vacina)
    {
        if (id != vacina.Id)
            return BadRequest(new { mensagem = "ID da URL diferente do corpo da requisição." });

        _context.Entry(vacina).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Vacinas.Any(v => v.Id == id))
                return NotFound(new { mensagem = "Vacina não encontrada." });
            throw;
        }

        return NoContent();
    }

    /// <summary>
    /// Remove uma vacina pelo seu ID
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVacina(int id)
    {
        var vacina = await _context.Vacinas.FindAsync(id);
        if (vacina == null)
            return NotFound(new { mensagem = "Vacina não encontrada." });

        _context.Vacinas.Remove(vacina);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}