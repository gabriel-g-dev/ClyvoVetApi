using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services.Interfaces;

namespace ClyvoVetApi.Services;

public class TutorService(ITutorRepository repository) : ITutorService
{
    private readonly ITutorRepository _repository = repository;

    public async Task<PagedResponseDto<TutorResponseDto>> GetAllAsync(int page, int pageSize)
    {
        var (items, total) = await _repository.GetAllAsync(page, pageSize);
        return new PagedResponseDto<TutorResponseDto>
        {
            Data       = items.Select(ToResponseDto),
            Page       = page,
            PageSize   = pageSize,
            TotalItems = total
        };
    }

    public async Task<TutorDetailsResponseDto> GetByIdAsync(int id)
    {
        var tutor = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tutor com ID {id} não encontrado.");
        return ToDetailsResponseDto(tutor);
    }

    public async Task<TutorDetailsResponseDto> GetByEmailAsync(string email)
    {
        var tutor = await _repository.GetByEmailAsync(email)
            ?? throw new NotFoundException($"Tutor com e-mail '{email}' não encontrado.");
        return ToDetailsResponseDto(tutor);
    }

    public async Task<TutorDetailsResponseDto> CreateAsync(TutorRequestDto dto)
    {
        var existing = await _repository.GetByEmailAsync(dto.Email);
        if (existing != null)
            throw new BusinessException("Já existe um tutor cadastrado com este e-mail.");

        var tutor = new Tutor
        {
            Nome     = dto.Nome,
            Email    = dto.Email,
            Telefone = dto.Telefone
        };

        var created = await _repository.CreateAsync(tutor);
        return ToDetailsResponseDto(created);
    }

    public async Task<TutorDetailsResponseDto> UpdateAsync(int id, TutorRequestDto dto)
    {
        var tutor = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tutor com ID {id} não encontrado.");

        if (tutor.Email != dto.Email)
        {
            var existing = await _repository.GetByEmailAsync(dto.Email);
            if (existing != null)
                throw new BusinessException("Já existe outro tutor cadastrado com este e-mail.");
        }

        tutor.Nome     = dto.Nome;
        tutor.Email    = dto.Email;
        tutor.Telefone = dto.Telefone;

        var updated = await _repository.UpdateAsync(tutor);
        return ToDetailsResponseDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var tutor = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tutor com ID {id} não encontrado.");
        await _repository.DeleteAsync(tutor);
    }

    public async Task<IEnumerable<PetDto>> GetPetsByTutorIdAsync(int tutorId)
    {
        var tutor = await _repository.GetByIdAsync(tutorId)
            ?? throw new NotFoundException($"Tutor com ID {tutorId} não encontrado.");
        
        return tutor.Pets?.Select(p => new PetDto
        {
            Id             = p.Id,
            Nome           = p.Nome,
            Especie        = p.Especie,
            Raca           = p.Raca,
            DataNascimento = p.DataNascimento
        }) ?? [];
    }

    private static TutorResponseDto ToResponseDto(Tutor t) => new()
    {
        Id        = t.Id,
        Nome      = t.Nome,
        Email     = t.Email,
        Telefone  = t.Telefone,
        TotalPets = t.Pets?.Count ?? 0
    };

    private static TutorDetailsResponseDto ToDetailsResponseDto(Tutor t) => new()
    {
        Id        = t.Id,
        Nome      = t.Nome,
        Email     = t.Email,
        Telefone  = t.Telefone,
        TotalPets = t.Pets?.Count ?? 0,
        Pets      = t.Pets?.Select(p => new PetDto
        {
            Id             = p.Id,
            Nome           = p.Nome,
            Especie        = p.Especie,
            Raca           = p.Raca,
            DataNascimento = p.DataNascimento
        }) ?? []
    };
}
