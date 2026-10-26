using ClyvoVetApi.Data;
using ClyvoVetApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ClyvoVetApi.Tests.Unit.Persistence;

public class OracleModelTests
{
    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>().UseOracle("Data Source=design-time", oracle =>
            oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion21)).Options);

    [Fact]
    public void OracleModel_MapsTheSevenClinicalTablesAndCascadeRelationships()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entities = context.Model.GetEntityTypes().ToList();

        // Assert
        Assert.Equal(new[] { "CONSULTAS", "CONSULTAS_MEDICAMENTOS", "DONOS", "FUNCIONARIOS", "MEDICAMENTOS", "PETS", "VACINAS" },
            entities.Select(e => e.GetTableName()).OrderBy(name => name));
        var relationships = entities.SelectMany(e => e.GetForeignKeys()).ToList();
        Assert.Equal(6, relationships.Count);
        Assert.All(relationships, key => Assert.Equal(DeleteBehavior.Cascade, key.DeleteBehavior));
        Assert.Equal("DECIMAL(5,2)", context.Model.FindEntityType(typeof(Pet))!.FindProperty(nameof(Pet.Peso))!.GetColumnType());
    }

    [Fact]
    public void OracleChecks_QuoteTheCaseSensitiveColumnNames()
    {
        // Arrange
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        // Act
        var checks = model.GetEntityTypes().SelectMany(e => e.GetCheckConstraints()).ToDictionary(c => c.Name!);

        // Assert
        Assert.Equal("\"consulta_status\" IN ('A', 'C', 'R')", checks["CK_CONSULTA_STATUS"].Sql);
        Assert.Equal("\"vacina_status\" IN ('P', 'A')", checks["CK_VACINA_STATUS"].Sql);
    }

    [Fact]
    public void Oracle21Queries_UseNumericBooleanProjection()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var sql = context.Pets.Select(p => p.Id > 0).ToQueryString();

        // Assert: Oracle 21 não aceita o tipo BOOLEAN SQL introduzido na versão 23.
        Assert.Contains("THEN 1", sql);
        Assert.Contains("ELSE 0", sql);
    }

    [Fact]
    public void Baseline_IsTheOnlyActiveMigrationAndMatchesTheCurrentModel()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var migrations = context.Database.GetMigrations().ToList();
        var hasModelChanges = context.Database.HasPendingModelChanges();

        // Assert
        Assert.Equal("20261025155040_OracleBaseline", Assert.Single(migrations));
        Assert.False(hasModelChanges);
    }

    [Fact]
    public void Baseline_RejectsExistingSchemasAndUsesRawCheckDdlToAvoidProviderEscapingBug()
    {
        // Arrange
        using var context = CreateContext();
        var assembly = context.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(Assert.Single(assembly.Migrations).Value, context.Database.ProviderName!);

        // Act
        var operations = migration.UpOperations;
        var sql = context.GetService<IMigrator>().GenerateScript();

        // Assert
        Assert.Contains("RAISE_APPLICATION_ERROR(-20001", Assert.IsType<SqlOperation>(operations[0]).Sql);
        Assert.Equal(7, operations.OfType<CreateTableOperation>().Count());
        Assert.DoesNotContain(operations, operation => operation is DropTableOperation or DropColumnOperation);
        Assert.All(operations.OfType<CreateTableOperation>(), table => Assert.Empty(table.CheckConstraints));
        Assert.Contains("ALTER TABLE \"CONSULTAS\" ADD CONSTRAINT \"CK_CONSULTA_STATUS\"", sql);
        Assert.Contains("ALTER TABLE \"VACINAS\" ADD CONSTRAINT \"CK_VACINA_STATUS\"", sql);
        Assert.Contains("CHECK (\"consulta_status\" IN ('A', 'C', 'R'))", sql);
    }
}
