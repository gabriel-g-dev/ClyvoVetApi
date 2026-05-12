using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PetsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/pets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pet>>> GetPets()
    {
        return Ok(await _context.Pets.Include(p => p.Tutor).ToListAsync());
    }

    // GET: api/pets/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Pet>> GetPet(int id)
    {
        var pet = await _context.Pets.Include(p => p.Tutor)
                                     .Include(p => p.Consultas)
                                     .Include(p => p.Vacinas)
                                     .FirstOrDefaultAsync(p => p.Id == id);
        if (pet == null)
            return NotFound(new { mensagem = "Pet não encontrado." });

        return Ok(pet);
    }

    // GET: api/pets/especie/{especie}
    [HttpGet("especie/{especie}")]
    public async Task<ActionResult> GetPetsByEspecie(string especie)
    {
        var pets = await _context.Pets.Include(p => p.Tutor)
                                      .Where(p => p.Especie.ToLower() == especie.ToLower())
                                      .ToListAsync();
        return Ok(pets);
    }

    // GET: api/pets/raca/{raca}
    [HttpGet("raca/{raca}")]
    public async Task<ActionResult> GetPetsByRaca(string raca)
    {
        var pets = await _context.Pets.Include(p => p.Tutor)
                                      .Where(p => p.Raca.ToLower() == raca.ToLower())
                                      .ToListAsync();
        return Ok(pets);
    }

    // GET: api/pets/{id}/vacinas
    [HttpGet("{id}/vacinas")]
    public async Task<ActionResult> GetVacinasDoPet(int id)
    {
        var pet = await _context.Pets.Include(p => p.Vacinas)
                                     .FirstOrDefaultAsync(p => p.Id == id);
        if (pet == null)
            return NotFound(new { mensagem = "Pet não encontrado." });

        return Ok(pet.Vacinas);
    }

    // GET: api/pets/{id}/consultas
    [HttpGet("{id}/consultas")]
    public async Task<ActionResult> GetConsultasDoPet(int id)
    {
        var pet = await _context.Pets.Include(p => p.Consultas)
                                     .FirstOrDefaultAsync(p => p.Id == id);
        if (pet == null)
            return NotFound(new { mensagem = "Pet não encontrado." });

        return Ok(pet.Consultas);
    }

    // POST: api/pets
    [HttpPost]
    public async Task<ActionResult<Pet>> PostPet(Pet pet)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPet), new { id = pet.Id }, pet);
    }

    // PUT: api/pets/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPet(int id, Pet pet)
    {
        if (id != pet.Id)
            return BadRequest(new { mensagem = "ID da URL diferente do corpo da requisição." });

        _context.Entry(pet).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Pets.Any(p => p.Id == id))
                return NotFound(new { mensagem = "Pet não encontrado." });
            throw;
        }

        return NoContent();
    }

    // DELETE: api/pets/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePet(int id)
    {
        var pet = await _context.Pets.FindAsync(id);
        if (pet == null)
            return NotFound(new { mensagem = "Pet não encontrado." });

        _context.Pets.Remove(pet);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}