namespace ClyvoVetApi.Mongo;

public class MongoOptions
{
    public const string SectionName = "Mongo";
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "clyvovet";
    public string CollectionName { get; set; } = "evolucoes_clinicas";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString) ||
            string.IsNullOrWhiteSpace(DatabaseName) ||
            DatabaseName.IndexOfAny(['/', '\\', '.', ' ', '"', '$', '\0']) >= 0 ||
            string.IsNullOrWhiteSpace(CollectionName) || CollectionName.Contains('$'))
            throw new InvalidOperationException("Configuração Mongo inválida: informe conexão, banco e coleção válidos.");
    }
}
