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

    // GET: api/vacinas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vacina>>> GetVacinas()
    {
        return Ok(await _context.Vacinas.Include(v => v.Pet).ToListAsync());
    }

    // GET: api/vacinas/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Vacina>> GetVacina(int id)
    {
        var vacina = await _context.Vacinas.Include(v => v.Pet)
                                           .FirstOrDefaultAsync(v => v.Id == id);
        if (vacina == null)
            return NotFound(new { mensagem = "Vacina não encontrada." });

        return Ok(vacina);
    }

    // GET: api/vacinas/pendentes
    [HttpGet("pendentes")]
    public async Task<ActionResult> GetVacinasPendentes()
    {
        var vacinas = await _context.Vacinas.Include(v => v.Pet)
                                            .Where(v => !v.Aplicada)
                                            .ToListAsync();
        return Ok(vacinas);
    }

    // GET: api/vacinas/nome/{nome}
    [HttpGet("nome/{nome}")]
    public async Task<ActionResult> GetVacinasByNome(string nome)
    {
        var vacinas = await _context.Vacinas.Include(v => v.Pet)
                                            .Where(v => v.Nome.ToLower()
                                            .Contains(nome.ToLower()))
                                            .ToListAsync();
        return Ok(vacinas);
    }

    // GET: api/vacinas/proximas?dias=30
    [HttpGet("proximas")]
    public async Task<ActionResult> GetProximasVacinas([FromQuery] int dias = 30)
    {
        var limite = DateTime.Now.AddDays(dias);
        var vacinas = await _context.Vacinas.Include(v => v.Pet)
                                            .Where(v => v.ProximaDose <= limite && !v.Aplicada)
                                            .ToListAsync();
        return Ok(vacinas);
    }

    // POST: api/vacinas
    [HttpPost]
    public async Task<ActionResult<Vacina>> PostVacina(Vacina vacina)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _context.Vacinas.Add(vacina);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVacina), new { id = vacina.Id }, vacina);
    }

    // PUT: api/vacinas/5
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

    // DELETE: api/vacinas/5
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