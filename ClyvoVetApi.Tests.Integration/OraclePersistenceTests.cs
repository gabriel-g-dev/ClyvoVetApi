using ClyvoVetApi.Data;
using ClyvoVetApi.DTOs.Request;
using ClyvoVetApi.Models;
using ClyvoVetApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace ClyvoVetApi.Tests.Integration;

public sealed class OracleRealFactAttribute : FactAttribute
{
    public OracleRealFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLYVOVET_ORACLE_TEST_CONNECTION"))
            || Environment.GetEnvironmentVariable("CLYVOVET_ORACLE_ALLOW_MIGRATIONS") != "true")
            Skip = "Exige Oracle descartável, CLYVOVET_ORACLE_TEST_CONNECTION e CLYVOVET_ORACLE_ALLOW_MIGRATIONS=true. SQLite não valida Oracle.";
    }
}

public class OraclePersistenceTests(ITestOutputHelper output)
{
    [OracleRealFact]
    [Trait("Category", "OracleReal")]
    public async Task Migrations_ApplyToOracleAndPersistTheClinicalGraphWithConstraints()
    {
        // Arrange: exclusivamente um schema descartável disponibilizado pelo operador.
        var connectionString = Environment.GetEnvironmentVariable("CLYVOVET_ORACLE_TEST_CONNECTION")!;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseOracle(connectionString, oracle =>
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion21)).Options;
        await using var context = new AppDbContext(options);

        // Act: não utiliza EnsureCreated, EnsureDeleted nem DROP. A baseline recusa tabelas existentes.
        await context.Database.MigrateAsync();
        var unique = Guid.NewGuid().ToString("N")[..12];
        var dono = new Dono { Nome = "Tutor de teste", Email = $"{unique}@example.test", Telefone = "11999999999" };
        var funcionario = new Funcionario { Nome = "Veterinário", Email = $"vet{unique}@example.test", Telefone = "11999999999", Cargo = "Veterinário", Setor = "Clínica" };
        var pet = new Pet { Nome = $"Pet Oracle {unique}", Especie = "Canina", Raca = "SRD", DataNascimento = new DateTime(2020, 1, 1), Peso = 12.34m, Dono = dono };
        var lighter = new Pet { Nome = $"Pet leve {unique}", Especie = "Canina", Raca = "SRD", DataNascimento = new DateTime(2020, 1, 1), Peso = 2.50m, Dono = dono };
        var tied = new Pet { Nome = $"Pet empate {unique}", Especie = "Canina", Raca = "SRD", DataNascimento = new DateTime(2020, 1, 1), Peso = 12.34m, Dono = dono };
        var consulta = new Consulta { Tipo = "Rotina", Descricao = $"Teste controlado {unique}", Data = DateTime.UtcNow, Valor = 100.25m, Status = "A", Pet = pet, Funcionario = funcionario };
        var cheaper = new Consulta { Tipo = "Rotina", Descricao = $"Teste barato {unique}", Data = DateTime.UtcNow, Valor = 12.01m, Status = "A", Pet = lighter, Funcionario = funcionario };
        var tiedAmount = new Consulta { Tipo = "Rotina", Descricao = $"Teste empate {unique}", Data = DateTime.UtcNow, Valor = 100.25m, Status = "A", Pet = tied, Funcionario = funcionario };
        var medicamento = new Medicamento { Nome = $"Medicamento de teste {unique}" };
        context.Consultas.Add(consulta);
        context.Consultas.AddRange(cheaper, tiedAmount);
        context.ConsultasMedicamentos.Add(new ConsultaMedicamento { Consulta = consulta, Medicamento = medicamento, Dosagem = "Conforme prescrição" });
        context.Vacinas.Add(new Vacina { Nome = "Vacina de teste", Data = DateTime.UtcNow, Status = "A", Pet = pet });
        await context.SaveChangesAsync();

        try
        {
            // Act: uma nova conexão reaplica migrations com dados já confirmados.
            await using var restarted = new AppDbContext(options);
            await restarted.Database.MigrateAsync();
            var applied = (await restarted.Database.GetAppliedMigrationsAsync()).ToList();
            var pending = (await restarted.Database.GetPendingMigrationsAsync()).ToList();
            var persisted = await restarted.Consultas.AsNoTracking().Include(c => c.Pet).ThenInclude(p => p!.Dono)
                .Include(c => c.ConsultasMedicamentos).SingleAsync(c => c.Id == consulta.Id);
            var petRepository = new PetRepository(restarted);
            var consultaRepository = new ConsultaRepository(restarted);
            var search = unique.ToUpperInvariant();
            var firstPets = await petRepository.GetAllAsync(1, 2, new ListQueryOptions { Search = search, SortBy = "peso" });
            var nextPets = await petRepository.GetAllAsync(2, 2, new ListQueryOptions { Search = search, SortBy = "peso" });
            var consultations = await consultaRepository.GetAllAsync(1, 10,
                new ListQueryOptions { Search = search, SortBy = "valor", Descending = true });

            // Assert: segundo Migrate não recria tabelas nem perde o grafo clínico.
            Assert.Contains("20261025155040_OracleBaseline", applied);
            Assert.Empty(pending);
            Assert.Equal(12.34m, persisted.Pet!.Peso);
            Assert.Equal(100.25m, persisted.Valor);
            Assert.Equal(dono.Email, persisted.Pet.Dono!.Email);
            Assert.Single(persisted.ConsultasMedicamentos);
            Assert.Equal(3, firstPets.Total);
            Assert.Equal(3, nextPets.Total);
            var expectedPetIds = new[] { pet, lighter, tied }.OrderBy(p => p.Peso).ThenBy(p => p.Id).Select(p => p.Id);
            Assert.Equal(expectedPetIds, firstPets.Items.Concat(nextPets.Items).Select(p => p.Id));
            Assert.Equal(3, consultations.Total);
            var expectedConsultationIds = new[] { consulta, cheaper, tiedAmount }
                .OrderByDescending(c => c.Valor).ThenBy(c => c.Id).Select(c => c.Id);
            Assert.Equal(expectedConsultationIds, consultations.Items.Select(c => c.Id));

            await using var transaction = await restarted.Database.BeginTransactionAsync();
            restarted.Vacinas.Add(new Vacina { Nome = "Status inválido", Data = DateTime.UtcNow, Status = "X", PetId = pet.Id });
            var checkError = await Assert.ThrowsAsync<DbUpdateException>(() => restarted.SaveChangesAsync());
            Assert.Contains("CK_VACINA_STATUS", checkError.ToString());
            await transaction.RollbackAsync();
            output.WriteLine($"Oracle real: migrations aplicadas={string.Join(",", applied)}; pendentes={pending.Count}; segunda aplicação e nova conexão preservaram dados; busca/paginação/ordem decimal e check exercitados.");
        }
        finally
        {
            // Somente os três registros raiz deste teste, confirmados por ID e marcador.
            // Os seis cascades removem apenas o grafo criado acima; não há DROP/reset.
            await using var cleanup = new AppDbContext(options);
            var ownDono = await cleanup.Donos.SingleOrDefaultAsync(d => d.Id == dono.Id && d.Email == dono.Email);
            var ownFuncionario = await cleanup.Funcionarios.SingleOrDefaultAsync(f => f.Id == funcionario.Id && f.Email == funcionario.Email);
            var ownMedicamento = await cleanup.Medicamentos.SingleOrDefaultAsync(m => m.Id == medicamento.Id && m.Nome == medicamento.Nome);
            if (ownDono is not null) cleanup.Donos.Remove(ownDono);
            if (ownFuncionario is not null) cleanup.Funcionarios.Remove(ownFuncionario);
            if (ownMedicamento is not null) cleanup.Medicamentos.Remove(ownMedicamento);
            await cleanup.SaveChangesAsync();
            output.WriteLine("Limpeza limitada aos IDs e marcadores aleatórios criados pelo próprio teste.");
        }
    }
}
