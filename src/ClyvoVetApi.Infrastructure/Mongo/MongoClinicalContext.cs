using MongoDB.Driver;

namespace ClyvoVetApi.Mongo;

public sealed class MongoClinicalContext
{
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private bool _indexesCreated;

    public IMongoDatabase Database { get; }
    internal IMongoCollection<EvolucaoClinicaDocument> Evolucoes { get; }

    public MongoClinicalContext(MongoOptions options)
    {
        options.Validate();
        var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
        settings.ConnectTimeout = TimeSpan.FromSeconds(3);
        settings.SocketTimeout = TimeSpan.FromSeconds(5);
        Database = new MongoClient(settings).GetDatabase(options.DatabaseName);
        Evolucoes = Database.GetCollection<EvolucaoClinicaDocument>(options.CollectionName);
    }

    internal async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (_indexesCreated)
                return;

            var index = new CreateIndexModel<EvolucaoClinicaDocument>(
                Builders<EvolucaoClinicaDocument>.IndexKeys.Ascending(x => x.PetId)
                    .Descending(x => x.RegistradoEmUtc).Ascending(x => x.Id),
                new CreateIndexOptions { Name = "pet_data_id" });
            await Evolucoes.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
            _indexesCreated = true;
        }
        finally
        {
            _indexLock.Release();
        }
    }
}
