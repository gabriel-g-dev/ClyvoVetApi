using Microsoft.EntityFrameworkCore;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tutor> Tutores { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<Consulta> Consultas { get; set; }
    public DbSet<Vacina> Vacinas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tutor>().ToTable("TUTORES");
        modelBuilder.Entity<Pet>().ToTable("PETS");
        modelBuilder.Entity<Consulta>().ToTable("CONSULTAS");
        modelBuilder.Entity<Vacina>().ToTable("VACINAS");
    }
}