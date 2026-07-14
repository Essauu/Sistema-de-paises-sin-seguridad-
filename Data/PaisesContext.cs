using Microsoft.EntityFrameworkCore;
using PaisApp.Models;

namespace PaisApp.Data;

public partial class PaisesContext : DbContext
{
    public PaisesContext(DbContextOptions<PaisesContext> options) : base(options) { }

    public DbSet<Country> Country { get; set; } = null!;
    public DbSet<City> City { get; set; } = null!;
    public DbSet<CountryLanguage> CountryLanguage { get; set; } = null!;
    public DbSet<Cgusuarios> Cgusuarios { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UseCollation("utf8mb4_0900_ai_ci").HasCharSet("utf8mb4");

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PRIMARY");
            entity.ToTable("country");
            entity.Property(e => e.Code).HasMaxLength(3).IsFixedLength();
            entity.Property(e => e.Name).HasMaxLength(52).IsFixedLength();
            entity.Property(e => e.Continent).HasColumnType("enum('Asia','Europe','North America','Africa','Oceania','Antarctica','South America')").HasDefaultValueSql("'Asia'");
            entity.Property(e => e.Region).HasMaxLength(26).IsFixedLength();
            entity.Property(e => e.SurfaceArea).HasPrecision(10, 2).HasDefaultValueSql("'0.00'");
            entity.Property(e => e.Population).HasDefaultValueSql("'0'");
            entity.Property(e => e.LifeExpectancy).HasPrecision(3, 1);
            entity.Property(e => e.GNP).HasPrecision(10, 2);
            entity.Property(e => e.GNPOld).HasColumnName("GNPOld").HasPrecision(10, 2);
            entity.Property(e => e.LocalName).HasMaxLength(45).IsFixedLength();
            entity.Property(e => e.GovernmentForm).HasMaxLength(45).IsFixedLength();
            entity.Property(e => e.HeadOfState).HasMaxLength(60);
            entity.Property(e => e.Code2).HasMaxLength(2).IsFixedLength();
            entity.Property(e => e.FlagUrl).HasMaxLength(255);

            entity.HasOne(d => d.CapitalCity)
                .WithMany()
                .HasForeignKey(d => d.Capital)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("country_ibfk_1");
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("city");
entity.Property(e => e.Name).HasMaxLength(35).IsFixedLength();
            entity.Property(e => e.CountryCode).HasMaxLength(3).IsFixedLength();
            entity.Property(e => e.District).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.Population).HasDefaultValueSql("'0'");
            entity.HasIndex(e => e.CountryCode, "CountryCode");
            entity.HasOne(d => d.Country)
                .WithMany(p => p.Cities)
                .HasForeignKey(d => d.CountryCode)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("city_ibfk_1");
        });

        modelBuilder.Entity<CountryLanguage>(entity =>
        {
            entity.HasKey(e => new { e.CountryCode, e.Language }).HasName("PRIMARY");
            entity.ToTable("countrylanguage");
            entity.Property(e => e.CountryCode).HasMaxLength(3).IsFixedLength();
            entity.Property(e => e.Language).HasMaxLength(30).IsFixedLength();
            entity.Property(e => e.IsOfficial).HasColumnType("enum('T','F')").HasDefaultValueSql("'F'");
            entity.Property(e => e.Percentage).HasPrecision(4, 1).HasDefaultValueSql("'0.0'");
            entity.HasIndex(e => e.CountryCode, "CountryCode");
            entity.HasOne(d => d.Country)
                .WithMany(p => p.CountryLanguages)
                .HasForeignKey(d => d.CountryCode)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("countryLanguage_ibfk_1");
        });

        modelBuilder.Entity<Cgusuarios>(entity =>
        {
            entity.ToTable("cgusuarios");
            entity.Property(e => e.Id).HasColumnName("IdUsuario");
            entity.Property(e => e.Username).HasColumnName("Username").HasMaxLength(8);
            entity.Property(e => e.Nombre).HasMaxLength(30);
            entity.Property(e => e.Apellido1).HasMaxLength(30);
            entity.Property(e => e.Apellido2).HasMaxLength(30);
            entity.Property(e => e.Password).HasColumnName("Password");
            entity.Property(e => e.Nivel).HasDefaultValueSql("'1'");
            entity.Property(e => e.Status).HasDefaultValueSql("'0'");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
