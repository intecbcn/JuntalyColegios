using JuntalyColegios.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JuntalyColegios.Infrastructure.Persistence.Configuraciones;

public sealed class ColegioConfiguracion : IEntityTypeConfiguration<Colegio>
{
    public void Configure(EntityTypeBuilder<Colegio> builder)
    {
        builder.ToTable("Colegios");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Nombre).HasMaxLength(Colegio.LongitudMaximaNombre).IsRequired();
        builder.Property(c => c.Direccion).HasMaxLength(Colegio.LongitudMaximaDireccion);
        builder.Property(c => c.ZonaHoraria).HasMaxLength(Colegio.LongitudMaximaZonaHoraria).IsRequired();
    }
}

public sealed class SupervisorConfiguracion : IEntityTypeConfiguration<Supervisor>
{
    public void Configure(EntityTypeBuilder<Supervisor> builder)
    {
        builder.ToTable("Supervisores");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Nombre).HasMaxLength(Supervisor.LongitudMaximaNombre).IsRequired();
        builder.Property(s => s.Email).HasMaxLength(Domain.ValueObjects.Email.LongitudMaxima).IsRequired();
        builder.Property(s => s.UsuarioId).IsRequired();

        builder.HasOne<Colegio>().WithMany().HasForeignKey(s => s.ColegioId).OnDelete(DeleteBehavior.Restrict);
        // Integridad con la cuenta de Identity sin que el dominio conozca IdentityUser.
        builder.HasOne<IdentityUser>().WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => s.UsuarioId).IsUnique();
    }
}
