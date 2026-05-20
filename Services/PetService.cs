using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.DTOs.Response;
using ClyvoVetApi.Exceptions;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories.Interfaces;
using ClyvoVetApi.Services.Interfaces;

namespace ClyvoVetApi.Services;

public class PetService(IPetRepository repository, ITutorRepository tutorRepository) : IPetService
{
    private readonly IPetRepository _repository = repository;
    private readonly ITutorRepository _tutorRepository = tutorRepository;

    public async Task<PagedResponseDto<PetResponseDto>> GetAllAsync(int page, int pageSize)
    {
        var (items, total) = await _repository.GetAllAsync(page, pageSize);
        return new PagedResponseDto<PetResponseDto>
        {
            Data = items.Select(ToResponseDto),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<PetDetailsResponseDto> GetByIdAsync(int id)
    {
        var pet = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Pet com ID {id} não encontrado.");
        return ToDetailsResponseDto(pet);
    }

    public async Task<IEnumerable<PetResponseDto>> GetByEspecieAsync(string especie)
    {
        var items = await _repository.GetByEspecieAsync(especie);
        return items.Select(ToResponseDto);
    }

    public async Task<IEnumerable<PetResponseDto>> GetByRacaAsync(string raca)
    {
        var items = await _repository.GetByRacaAsync(raca);
        return items.Select(ToResponseDto);
    }

    public async Task<PetDetailsResponseDto> CreateAsync(PetRequestDto dto)
    {
        var tutor = await _tutorRepository.GetByIdAsync(dto.TutorId)
            ?? throw new BusinessException($"Tutor associado com ID {dto.TutorId} não encontrado.");

        var pet = new Pet
        {
            Nome = dto.Nome,
            Especie = dto.Especie,
            Raca = dto.Raca,
            DataNascimento = dto.DataNascimento,
            TutorId = dto.TutorId
        };

        var created = await _repository.CreateAsync(pet);
        
        // Atribuir o tutor para o mapeamento correto do DTO
        created.Tutor = tutor;

        return ToDetailsResponseDto(created);
    }

    public async Task<PetDetailsResponseDto> UpdateAsync(int id, PetRequestDto dto)
    {
        var pet = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Pet com ID {id} não encontrado.");

        var tutor = await _tutorRepository.GetByIdAsync(dto.TutorId)
            ?? throw new BusinessException($"Tutor associado com ID {dto.TutorId} não encontrado.");

        pet.Nome = dto.Nome;
        pet.Especie = dto.Especie;
        pet.Raca = dto.Raca;
        pet.DataNascimento = dto.DataNascimento;
        pet.TutorId = dto.TutorId;

        var updated = await _repository.UpdateAsync(pet);
        updated.Tutor = tutor;

        return ToDetailsResponseDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var pet = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Pet com ID {id} não encontrado.");
        await _repository.DeleteAsync(pet);
    }

    private static PetResponseDto ToResponseDto(Pet p) => new()
    {
        Id = p.Id,
        Nome = p.Nome,
        Especie = p.Especie,
        Raca = p.Raca,
        DataNascimento = p.DataNascimento,
        TutorId = p.TutorId,
        TutorNome = p.Tutor?.Nome ?? string.Empty
    };

    private static PetDetailsResponseDto ToDetailsResponseDto(Pet p) => new()
    {
        Id = p.Id,
        Nome = p.Nome,
        Especie = p.Especie,
        Raca = p.Raca,
        DataNascimento = p.DataNascimento,
        TutorId = p.TutorId,
        TutorNome = p.Tutor?.Nome ?? string.Empty,
        Tutor = p.Tutor != null ? new TutorResponseDto
        {
            Id = p.Tutor.Id,
            Nome = p.Tutor.Nome,
            Email = p.Tutor.Email,
            Telefone = p.Tutor.Telefone,
            TotalPets = p.Tutor.Pets?.Count ?? 0
        } : null,
        Consultas = p.Consultas?.Select(c => new ConsultaResponseDto
        {
            Id = c.Id,
            Data = c.Data,
            Descricao = c.Descricao,
            Veterinario = c.Veterinario,
            Observacoes = c.Observacoes,
            PetId = c.PetId,
            PetNome = p.Nome
        }) ?? [],
        Vacinas = p.Vacinas?.Select(v => new VacinaResponseDto
        {
            Id = v.Id,
            Nome = v.Nome,
            DataAplicacao = v.DataAplicacao,
            ProximaDose = v.ProximaDose,
            Aplicada = v.Aplicada,
            PetId = v.PetId,
            PetNome = p.Nome
        }) ?? []
    };
}
