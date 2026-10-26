using System.ComponentModel.DataAnnotations;
using ClyvoVetApi.Exceptions;

namespace ClyvoVetApi.DTOs.Request;

public class ListQueryOptions
{
    [StringLength(100)]
    public string? Search { get; set; }

    [StringLength(30)]
    public string SortBy { get; set; } = "id";

    public bool Descending { get; set; }

    public void Validate(int page, int pageSize, params string[] allowedSortFields)
    {
        ValidatePage(page, pageSize);
        SortBy = SortBy?.Trim().ToLowerInvariant() ?? "id";
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        if (Search?.Length > 100)
            throw new BusinessException("A busca deve ter no máximo 100 caracteres.");
        if (!allowedSortFields.Contains(SortBy, StringComparer.Ordinal))
            throw new BusinessException($"Ordenação inválida. Campos permitidos: {string.Join(", ", allowedSortFields)}.");
    }

    public static void ValidatePage(int page, int pageSize)
    {
        if (page is < 1 or > 1_000_000 || pageSize is < 1 or > 100)
            throw new BusinessException("Page deve estar entre 1 e 1000000 e pageSize entre 1 e 100.");
    }
}
