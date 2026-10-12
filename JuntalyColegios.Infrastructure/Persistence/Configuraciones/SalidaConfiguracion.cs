using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JuntalyColegios.Infrastructure.Persistence.Configuraciones;

public sealed class DeteccionConfiguracion : IEntityTypeConfiguration<Deteccion>
{
    public void Configure(EntityTypeBuilder<Deteccion> builder)
    {
        builder.ToTable("Detecciones");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Tag).HasMaxLength(TagRfid.LongitudMaxima).IsRequired();
        builder.Property(d => d.Resultado).HasConversion<int>();

        builder.HasOne<Colegio>().WithMany().HasForeignKey(d => d.ColegioId).OnDelete(DeleteBehavior.Restrict);
        // El historial se conserva aunque el vehículo se elimine.
        builder.HasOne<Vehiculo>().WithMany().HasForeignKey(d => d.VehiculoId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(d => new { d.ColegioId, d.DetectadoEn });
        builder.HasIndex(d => new { d.VehiculoId, d.DetectadoEn });
    }
}

public sealed class RecogidaConfiguracion : IEntityTypeConfiguration<Recogida>
{
    public void Configure(EntityTypeBuilder<Recogida> builder)
    {
        builder.ToTable("Recogidas");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Ignore(r => r.Pendiente);

        builder.HasOne<Colegio>().WithMany().HasForeignKey(r => r.ColegioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Deteccion>().WithMany().HasForeignKey(r => r.DeteccionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Alumno>().WithMany().HasForeignKey(r => r.AlumnoId).OnDelete(DeleteBehavior.Cascade);
        // Sin clave foránea al vehículo a propósito: es un dato histórico y eliminar el vehículo no debe
        // borrar el historial de recogidas de la familia.
        // La pantalla de salida consulta las pendientes del colegio.
        builder.HasIndex(r => new { r.ColegioId, r.EntregadoEn });
    }
}
