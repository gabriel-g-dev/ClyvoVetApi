using ClyvoVetApi.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace ClyvoVetApi.Mongo;

internal sealed class EvolucaoClinicaDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public int PetId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime RegistradoEmUtc { get; set; }

    public string[] Tags { get; set; } = [];

    public static EvolucaoClinicaDocument FromDomain(EvolucaoClinica evolucao) => new()
    {
        Id = evolucao.Id,
        PetId = evolucao.PetId,
        Texto = evolucao.Texto,
        Autor = evolucao.Autor,
        RegistradoEmUtc = evolucao.RegistradoEmUtc,
        Tags = evolucao.Tags.ToArray()
    };

    public EvolucaoClinica ToDomain() => new(Id, PetId, Texto, Autor, RegistradoEmUtc, Tags);
}
