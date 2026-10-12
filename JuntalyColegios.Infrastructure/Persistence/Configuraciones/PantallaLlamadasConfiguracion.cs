using JuntalyColegios.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JuntalyColegios.Infrastructure.Persistence.Configuraciones;

public sealed class PantallaLlamadasConfiguracion : IEntityTypeConfiguration<PantallaLlamadas>
{
    public void Configure(EntityTypeBuilder<PantallaLlamadas> builder)
    {
        builder.ToTable("PantallasLlamadas");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        // SHA-256 en hexadecimal; la pantalla se abre buscando por él.
        builder.Property(p => p.HashToken).HasMaxLength(64).IsRequired();
        builder.HasIndex(p => p.HashToken).IsUnique();
        builder.Ignore(p => p.EsPorteria);

        builder.HasOne<Colegio>().WithMany().HasForeignKey(p => p.ColegioId).OnDelete(DeleteBehavior.Restrict);
        // Al eliminar una clase desaparece la pantalla de su aula.
        builder.HasOne<Clase>().WithMany().HasForeignKey(p => p.ClaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => new { p.ColegioId, p.ClaseId });
    }
}
