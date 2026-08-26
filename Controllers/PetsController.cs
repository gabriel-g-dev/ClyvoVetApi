using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PetsController(IPetService service, IIntelligenceService intelligenceService) : ControllerBase
{
    private readonly IPetService _service = service;
    private readonly IIntelligenceService _intelligenceService = intelligenceService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponseDto<PetResponseDto>))]
    public async Task<IActionResult> GetPets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PetDetailsResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetPet(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("especie/{especie}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PetResponseDto>))]
    public async Task<IActionResult> GetPetsByEspecie(string especie)
    {
        var result = await _service.GetByEspecieAsync(especie);
        return Ok(result);
    }

    [HttpGet("raca/{raca}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PetResponseDto>))]
    public async Task<IActionResult> GetPetsByRaca(string raca)
    {
        var result = await _service.GetByRacaAsync(raca);
        return Ok(result);
    }

    [HttpGet("{id:int}/vacinas")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<VacinaResponseDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetVacinasDoPet(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result.Vacinas);
    }

    [HttpGet("{id:int}/consultas")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ConsultaResponseDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetConsultasDoPet(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result.Consultas);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PetDetailsResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> PostPet([FromBody] PetRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetPet), new { id = result.Id }, result);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PetDetailsResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> PutPet(int id, [FromBody] PetRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> DeletePet(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}/inteligencia-preventiva")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HealthDashboardResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetIntelligencePreventiva(int id)
    {
        var result = await _intelligenceService.GetIntelligencePreventivaAsync(id);
        return Ok(result);
    }
}
