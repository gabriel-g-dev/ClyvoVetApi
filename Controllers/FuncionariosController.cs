using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FuncionariosController(IFuncionarioService service) : ControllerBase
{
    private readonly IFuncionarioService _service = service;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponseDto<FuncionarioResponseDto>))]
    public async Task<IActionResult> GetFuncionarios(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FuncionarioResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetFuncionario(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FuncionarioResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetFuncionarioByEmail(string email)
    {
        var result = await _service.GetByEmailAsync(email);
        return Ok(result);
    }

    [HttpGet("setor/{setor}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FuncionarioResponseDto>))]
    public async Task<IActionResult> GetFuncionariosBySetor(string setor)
    {
        var result = await _service.GetBySetorAsync(setor);
        return Ok(result);
    }

    [HttpGet("cargo/{cargo}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FuncionarioResponseDto>))]
    public async Task<IActionResult> GetFuncionariosByCargo(string cargo)
    {
        var result = await _service.GetByCargoAsync(cargo);
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(FuncionarioResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> PostFuncionario([FromBody] FuncionarioRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetFuncionario), new { id = result.Id }, result);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FuncionarioResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> PutFuncionario(int id, [FromBody] FuncionarioRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteFuncionario(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
