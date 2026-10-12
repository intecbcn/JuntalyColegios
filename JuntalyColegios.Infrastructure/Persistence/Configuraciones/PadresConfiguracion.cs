using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JuntalyColegios.Infrastructure.Persistence.Configuraciones;

public sealed class RepresentanteConfiguracion : IEntityTypeConfiguration<Representante>
{
    public void Configure(EntityTypeBuilder<Representante> builder)
    {
        builder.ToTable("Representantes");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Nombre).HasMaxLength(Representante.LongitudMaximaNombre).IsRequired();
        builder.Property(r => r.Email).HasMaxLength(Domain.ValueObjects.Email.LongitudMaxima).IsRequired();
        builder.Property(r => r.Telefono).HasMaxLength(Representante.LongitudMaximaTelefono);
        builder.Property(r => r.UsuarioId).IsRequired();

        builder.HasOne<Colegio>().WithMany().HasForeignKey(r => r.ColegioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IdentityUser>().WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => r.UsuarioId).IsUnique();

        // Vínculos padre ↔ hijo: colección propia del agregado Representante, en su propia tabla y con
        // clave foránea real al alumno.
        builder.OwnsMany(r => r.Alumnos, vinculo =>
        {
            vinculo.ToTable("RepresentantesAlumnos");
            vinculo.WithOwner().HasForeignKey("RepresentanteId");
            vinculo.Property(v => v.AlumnoId);
            vinculo.HasKey("RepresentanteId", nameof(VinculoAlumno.AlumnoId));
            vinculo.HasOne<Alumno>().WithMany().HasForeignKey(v => v.AlumnoId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Navigation(r => r.Alumnos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PersonaAutorizadaConfiguracion : IEntityTypeConfiguration<PersonaAutorizada>
{
    public void Configure(EntityTypeBuilder<PersonaAutorizada> builder)
    {
        builder.ToTable("PersonasAutorizadas");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Nombre).HasMaxLength(PersonaAutorizada.LongitudMaximaNombre).IsRequired();
        builder.Property(a => a.Documento).HasMaxLength(PersonaAutorizada.LongitudMaximaDocumento).IsRequired();
        builder.Property(a => a.Parentesco).HasMaxLength(PersonaAutorizada.LongitudMaximaParentesco);

        builder.HasOne<Representante>().WithMany().HasForeignKey(a => a.RepresentanteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Colegio>().WithMany().HasForeignKey(a => a.ColegioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.RepresentanteId, a.Documento }).IsUnique();
    }
}

public sealed class VehiculoConfiguracion : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> builder)
    {
        builder.ToTable("Vehiculos");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Placa).HasMaxLength(Placa.LongitudMaxima).IsRequired();
        builder.Property(v => v.Descripcion).HasMaxLength(Vehiculo.LongitudMaximaDescripcion);
        builder.Property(v => v.Tag).HasMaxLength(TagRfid.LongitudMaxima);

        builder.HasOne<Representante>().WithMany().HasForeignKey(v => v.RepresentanteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Colegio>().WithMany().HasForeignKey(v => v.ColegioId).OnDelete(DeleteBehavior.Restrict);
        // Un tag identifica un único vehículo en el colegio (los NULL no chocan entre sí). La antena
        // busca por (colegio, tag), así que el índice sirve también para esa consulta.
        builder.HasIndex(v => new { v.ColegioId, v.Tag }).IsUnique();
        builder.HasIndex(v => new { v.RepresentanteId, v.Placa }).IsUnique();
    }
}
