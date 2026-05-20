using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TutoresController(ITutorService service) : ControllerBase
{
    private readonly ITutorService _service = service;

    /// <summary>
    /// Lista todos os tutores com paginação
    /// </summary>
    /// <param name="page">Número da página (inicia em 1)</param>
    /// <param name="pageSize">Tamanho da página (padrão 10)</param>
    /// <returns>Retorna uma lista paginada de tutores</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponseDto<TutorResponseDto>))]
    public async Task<IActionResult> GetTutores(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Busca um tutor pelo seu ID
    /// </summary>
    /// <param name="id">ID único do tutor</param>
    /// <returns>Retorna os detalhes completos do tutor e a lista de seus pets</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TutorDetailsResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetTutor(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Busca um tutor pelo seu e-mail
    /// </summary>
    /// <param name="email">E-mail cadastrado do tutor</param>
    /// <returns>Retorna os detalhes completos do tutor encontrado</returns>
    [HttpGet("email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TutorDetailsResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetTutorByEmail(string email)
    {
        var result = await _service.GetByEmailAsync(email);
        return Ok(result);
    }

    /// <summary>
    /// Lista os pets de um tutor específico
    /// </summary>
    /// <param name="id">ID do tutor</param>
    /// <returns>Retorna a lista de pets vinculados ao tutor</returns>
    [HttpGet("{id:int}/pets")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PetDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetPetsDoTutor(int id)
    {
        var result = await _service.GetPetsByTutorIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Cadastra um novo tutor
    /// </summary>
    /// <param name="dto">Dados para cadastro do tutor</param>
    /// <returns>Retorna o tutor recém-criado</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TutorDetailsResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> PostTutor([FromBody] TutorRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetTutor), new { id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza as informações de um tutor existente
    /// </summary>
    /// <param name="id">ID do tutor a ser atualizado</param>
    /// <param name="dto">Novos dados do tutor</param>
    /// <returns>Retorna o tutor com os dados atualizados</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TutorDetailsResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> PutTutor(int id, [FromBody] TutorRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    /// <summary>
    /// Remove um tutor pelo seu ID
    /// </summary>
    /// <param name="id">ID do tutor a ser excluído</param>
    /// <returns>Retorna status 204 NoContent indicando sucesso</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteTutor(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}