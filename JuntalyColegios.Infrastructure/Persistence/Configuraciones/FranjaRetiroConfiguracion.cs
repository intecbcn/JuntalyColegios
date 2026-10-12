using JuntalyColegios.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JuntalyColegios.Infrastructure.Persistence.Configuraciones;

public sealed class FranjaRetiroConfiguracion : IEntityTypeConfiguration<FranjaRetiro>
{
    public void Configure(EntityTypeBuilder<FranjaRetiro> builder)
    {
        builder.ToTable("FranjasRetiro");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.DiaSemana).HasConversion<int>();

        builder.HasOne<Colegio>().WithMany().HasForeignKey(f => f.ColegioId).OnDelete(DeleteBehavior.Restrict);
        // Al eliminar una clase desaparecen sus franjas propias (sus alumnos vuelven a las del colegio).
        builder.HasOne<Clase>().WithMany().HasForeignKey(f => f.ClaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(f => new { f.ColegioId, f.DiaSemana });
    }
}
