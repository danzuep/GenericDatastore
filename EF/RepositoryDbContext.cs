using Microsoft.EntityFrameworkCore;
using Data.EF.Entities;

namespace Data.EF;

/// <summary>
/// The Npgsql EF Core provider also supports reverse-engineering from an existing PostgreSQL database ("database-first") using the CLI (dotnet ef dbcontext) or:
/// Scaffold-DbContext Name=ConnectionStrings:<database-host> Npgsql.EntityFrameworkCore.PostgreSQL -OutputDir "Entities" -DataAnnotations -Force -Tables <csv-tables>
/// https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing?tabs=dotnet-core-cli#add-a-migration
/// Add-Migration InitialCreate -Context <context-name> -Project <project-name.data>
/// https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=vs#sql-scripts
/// Script-Migration -Context <context-name>
/// </summary>
internal class RepositoryDbContext : DbContext
{
    public virtual DbSet<EntityItem> Jobs => Set<EntityItem>();

    public RepositoryDbContext() { }

    public RepositoryDbContext(DbContextOptions options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<EntityItem>();
    }
}