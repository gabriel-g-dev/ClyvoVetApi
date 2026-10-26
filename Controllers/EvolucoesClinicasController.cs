using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVetApi.Controllers;

[ApiController]
[Authorize(Policy = "ClinicalAccess")]
[Route("api/pets/{petId:int}/evolucoes")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status503ServiceUnavailable, Type = typeof(ProblemDetails))]
public class EvolucoesClinicasController(IEvolucaoClinicaService service) : ControllerBase
{
    /// <summary>Consulta as evoluções do pet por tag e período, ordenadas pela data de registro.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponseDto<EvolucaoClinicaResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetEvolucoes(int petId, [FromQuery] EvolucaoClinicaQueryDto query,
        CancellationToken cancellationToken) => Ok(await service.GetByPetAsync(petId, query, cancellationToken));

    /// <summary>Consulta uma evolução imutável pertencente ao pet informado.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EvolucaoClinicaResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetEvolucao(int petId, string id, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdAsync(petId, id, cancellationToken));

    /// <summary>Registra uma evolução clínica no MongoDB, com autor obtido do JWT.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EvolucaoClinicaResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> PostEvolucao(int petId, [FromBody] EvolucaoClinicaRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(petId, dto, User.Identity!.Name!, cancellationToken);
        return CreatedAtAction(nameof(GetEvolucao), new { petId, id = result.Id }, result);
    }
}
