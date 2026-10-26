using ClyvoVetApi.Exceptions;

namespace ClyvoVetApi.Models;

public sealed class EvolucaoClinica
{
    public string Id { get; }
    public int PetId { get; }
    public string Texto { get; }
    public string Autor { get; }
    public DateTime RegistradoEmUtc { get; }
    public IReadOnlyList<string> Tags { get; }

    public EvolucaoClinica(string id, int petId, string texto, string autor,
        DateTime registradoEmUtc, IEnumerable<string>? tags = null)
    {
        if (!Guid.TryParseExact(id, "N", out _))
            throw new BusinessException("O identificador da evolução clínica é inválido.");
        if (petId <= 0)
            throw new BusinessException("O pet deve possuir um identificador positivo.");
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length > 4000)
            throw new BusinessException("O texto da evolução deve conter entre 1 e 4000 caracteres.");
        if (string.IsNullOrWhiteSpace(autor) || autor.Trim().Length > 100)
            throw new BusinessException("O autor deve conter entre 1 e 100 caracteres.");
        if (registradoEmUtc.Kind != DateTimeKind.Utc)
            throw new BusinessException("A data de registro deve estar em UTC.");

        var normalizedTags = (tags ?? []).Select(tag => tag?.Trim() ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (normalizedTags.Length > 10 || normalizedTags.Any(tag => tag.Length is < 1 or > 40))
            throw new BusinessException("Informe até 10 tags, cada uma com 1 a 40 caracteres.");

        Id = id;
        PetId = petId;
        Texto = texto.Trim();
        Autor = autor.Trim();
        RegistradoEmUtc = registradoEmUtc;
        Tags = Array.AsReadOnly(normalizedTags);
    }
}
