using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class VacinasController(IVacinaService service) : ControllerBase
{
    private readonly IVacinaService _service = service;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponseDto<VacinaResponseDto>))]
    public async Task<IActionResult> GetVacinas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VacinaResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetVacina(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("pendentes")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<VacinaResponseDto>))]
    public async Task<IActionResult> GetVacinasPendentes()
    {
        var result = await _service.GetPendentesAsync();
        return Ok(result);
    }

    [HttpGet("nome/{nome}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<VacinaResponseDto>))]
    public async Task<IActionResult> GetVacinasByNome(string nome)
    {
        var result = await _service.GetByNomeAsync(nome);
        return Ok(result);
    }

    [HttpGet("proximas")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<VacinaResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetProximasVacinas([FromQuery] int dias = 30)
    {
        var result = await _service.GetProximasAsync(dias);
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(VacinaResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> PostVacina([FromBody] VacinaRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetVacina), new { id = result.Id }, result);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VacinaResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> PutVacina(int id, [FromBody] VacinaRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteVacina(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
