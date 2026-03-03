using es.vargontoc.nuzlocke.ai.Models;
using Microsoft.EntityFrameworkCore;

namespace es.vargontoc.nuzlocke.ai.Data;

public class PokeDbContext : DbContext
{
    public PokeDbContext(DbContextOptions<PokeDbContext> options) : base(options)
    {
    }

    // ── Nuzlocke metadata ─────────────────────────────────────────────────
    public DbSet<NuzlockeMetadata> NuzlockeMetadatas { get; set; }

    // ── PokeAPI cache ─────────────────────────────────────────────────────
    public DbSet<CachedPokemon> CachedPokemons { get; set; }
    public DbSet<CachedMove> CachedMoves { get; set; }
    public DbSet<CachedType> CachedTypes { get; set; }
    public DbSet<CachedAbility> CachedAbilities { get; set; }
    public DbSet<CachedItem> CachedItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NuzlockeMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(100);
            entity.Property(e => e.LockeType).HasConversion<string>().IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().IsRequired();
            entity.Property(e => e.IsInitialized).IsRequired();
            entity.Property(e => e.Generation).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastUpdated).IsRequired();
        });

        modelBuilder.Entity<CachedPokemon>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NameOrId).IsUnique();
            entity.Property(e => e.JsonData).IsRequired();
            entity.Property(e => e.CachedAt).IsRequired();
        });

        modelBuilder.Entity<CachedMove>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NameOrId).IsUnique();
            entity.Property(e => e.JsonData).IsRequired();
            entity.Property(e => e.CachedAt).IsRequired();
        });

        modelBuilder.Entity<CachedType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NameOrId).IsUnique();
            entity.Property(e => e.JsonData).IsRequired();
            entity.Property(e => e.CachedAt).IsRequired();
        });

        modelBuilder.Entity<CachedAbility>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NameOrId).IsUnique();
            entity.Property(e => e.JsonData).IsRequired();
            entity.Property(e => e.CachedAt).IsRequired();
        });

        modelBuilder.Entity<CachedItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NameOrId).IsUnique();
            entity.Property(e => e.JsonData).IsRequired();
            entity.Property(e => e.CachedAt).IsRequired();
        });

    }
}

public class CachedPokemon
{
    public int Id { get; set; }
    public string NameOrId { get; set; } = string.Empty;
    public string JsonData { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
}

public class CachedMove
{
    public int Id { get; set; }
    public string NameOrId { get; set; } = string.Empty;
    public string JsonData { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
}

public class CachedType
{
    public int Id { get; set; }
    public string NameOrId { get; set; } = string.Empty;
    public string JsonData { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
}

public class CachedAbility
{
    public int Id { get; set; }
    public string NameOrId { get; set; } = string.Empty;
    public string JsonData { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
}

public class CachedItem
{
    public int Id { get; set; }
    public string NameOrId { get; set; } = string.Empty;
    public string JsonData { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
}

