using JuntalyColegios.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JuntalyColegios.Infrastructure.Persistence.Configuraciones;

public sealed class ClaseConfiguracion : IEntityTypeConfiguration<Clase>
{
    public void Configure(EntityTypeBuilder<Clase> builder)
    {
        builder.ToTable("Clases");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Nombre).HasMaxLength(Clase.LongitudMaximaNombre).IsRequired();
        builder.Property(c => c.Nivel).HasMaxLength(Clase.LongitudMaximaNivel);

        builder.HasOne<Colegio>().WithMany().HasForeignKey(c => c.ColegioId).OnDelete(DeleteBehavior.Restrict);
        // Red de seguridad del nombre único por colegio (la regla, sin distinguir mayúsculas, la aplica Application).
        builder.HasIndex(c => new { c.ColegioId, c.Nombre }).IsUnique();
    }
}

public sealed class AlumnoConfiguracion : IEntityTypeConfiguration<Alumno>
{
    public void Configure(EntityTypeBuilder<Alumno> builder)
    {
        builder.ToTable("Alumnos");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Nombres).HasMaxLength(Alumno.LongitudMaximaNombres).IsRequired();
        builder.Property(a => a.Apellidos).HasMaxLength(Alumno.LongitudMaximaApellidos).IsRequired();
        builder.Ignore(a => a.NombreCompleto);

        builder.HasOne<Colegio>().WithMany().HasForeignKey(a => a.ColegioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Clase>().WithMany().HasForeignKey(a => a.ClaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.ColegioId, a.ClaseId });
    }
}
