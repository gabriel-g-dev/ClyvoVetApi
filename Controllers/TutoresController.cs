using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TutoresController : ControllerBase
{
    private readonly AppDbContext _context;

    public TutoresController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/tutores
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Tutor>>> GetTutores()
    {
        return Ok(await _context.Tutores.Include(t => t.Pets).ToListAsync());
    }

    // GET: api/tutores/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Tutor>> GetTutor(int id)
    {
        var tutor = await _context.Tutores.Include(t => t.Pets)
                                          .FirstOrDefaultAsync(t => t.Id == id);
        if (tutor == null)
            return NotFound(new { mensagem = "Tutor não encontrado." });

        return Ok(tutor);
    }

    // GET: api/tutores/email/{email}
    [HttpGet("email/{email}")]
    public async Task<ActionResult<Tutor>> GetTutorByEmail(string email)
    {
        var tutor = await _context.Tutores.Include(t => t.Pets)
                                          .FirstOrDefaultAsync(t => t.Email == email);
        if (tutor == null)
            return NotFound(new { mensagem = "Tutor não encontrado com esse email." });

        return Ok(tutor);
    }

    // GET: api/tutores/{id}/pets
    [HttpGet("{id}/pets")]
    public async Task<ActionResult> GetPetsDoTutor(int id)
    {
        var tutor = await _context.Tutores.Include(t => t.Pets)
                                          .FirstOrDefaultAsync(t => t.Id == id);
        if (tutor == null)
            return NotFound(new { mensagem = "Tutor não encontrado." });

        return Ok(tutor.Pets);
    }

    // POST: api/tutores
    [HttpPost]
    public async Task<ActionResult<Tutor>> PostTutor(Tutor tutor)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _context.Tutores.Add(tutor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTutor), new { id = tutor.Id }, tutor);
    }

    // PUT: api/tutores/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTutor(int id, Tutor tutor)
    {
        if (id != tutor.Id)
            return BadRequest(new { mensagem = "ID da URL diferente do corpo da requisição." });

        _context.Entry(tutor).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Tutores.Any(t => t.Id == id))
                return NotFound(new { mensagem = "Tutor não encontrado." });
            throw;
        }

        return NoContent();
    }

    // DELETE: api/tutores/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTutor(int id)
    {
        var tutor = await _context.Tutores.FindAsync(id);
        if (tutor == null)
            return NotFound(new { mensagem = "Tutor não encontrado." });

        _context.Tutores.Remove(tutor);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}