using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services.Interfaces;

namespace ClyvoVetApi.Services;

public class ConsultaService(IConsultaRepository repository, IPetRepository petRepository) : IConsultaService
{
    private readonly IConsultaRepository _repository = repository;
    private readonly IPetRepository _petRepository = petRepository;

    public async Task<PagedResponseDto<ConsultaResponseDto>> GetAllAsync(int page, int pageSize)
    {
        var (items, total) = await _repository.GetAllAsync(page, pageSize);
        return new PagedResponseDto<ConsultaResponseDto>
        {
            Data = items.Select(ToResponseDto),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<ConsultaResponseDto> GetByIdAsync(int id)
    {
        var consulta = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Consulta com ID {id} não encontrada.");
        return ToResponseDto(consulta);
    }

    public async Task<IEnumerable<ConsultaResponseDto>> GetByVeterinarioAsync(string nome)
    {
        var items = await _repository.GetByVeterinarioAsync(nome);
        return items.Select(ToResponseDto);
    }

    public async Task<IEnumerable<ConsultaResponseDto>> GetByPeriodoAsync(DateTime inicio, DateTime fim)
    {
        if (inicio > fim)
            throw new BusinessException("A data de início do período não pode ser posterior à data de fim.");

        var items = await _repository.GetByPeriodoAsync(inicio, fim);
        return items.Select(ToResponseDto);
    }

    public async Task<ConsultaResponseDto> CreateAsync(ConsultaRequestDto dto)
    {
        var pet = await _petRepository.GetByIdAsync(dto.PetId)
            ?? throw new BusinessException($"Pet associado com ID {dto.PetId} não encontrado.");

        var consulta = new Consulta
        {
            Data = dto.Data,
            Descricao = dto.Descricao,
            Veterinario = dto.Veterinario,
            Observacoes = dto.Observacoes,
            PetId = dto.PetId
        };

        var created = await _repository.CreateAsync(consulta);
        created.Pet = pet;

        return ToResponseDto(created);
    }

    public async Task<ConsultaResponseDto> UpdateAsync(int id, ConsultaRequestDto dto)
    {
        var consulta = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Consulta com ID {id} não encontrada.");

        var pet = await _petRepository.GetByIdAsync(dto.PetId)
            ?? throw new BusinessException($"Pet associado com ID {dto.PetId} não encontrado.");

        consulta.Data = dto.Data;
        consulta.Descricao = dto.Descricao;
        consulta.Veterinario = dto.Veterinario;
        consulta.Observacoes = dto.Observacoes;
        consulta.PetId = dto.PetId;

        var updated = await _repository.UpdateAsync(consulta);
        updated.Pet = pet;

        return ToResponseDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var consulta = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Consulta com ID {id} não encontrada.");
        await _repository.DeleteAsync(consulta);
    }

    private static ConsultaResponseDto ToResponseDto(Consulta c) => new()
    {
        Id = c.Id,
        Data = c.Data,
        Descricao = c.Descricao,
        Veterinario = c.Veterinario,
        Observacoes = c.Observacoes,
        PetId = c.PetId,
        PetNome = c.Pet?.Nome ?? string.Empty
    };
}
