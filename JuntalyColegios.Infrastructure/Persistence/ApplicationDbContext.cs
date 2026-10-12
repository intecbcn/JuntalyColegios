using JuntalyColegios.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JuntalyColegios.Infrastructure.Persistence
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Colegio> Colegios => Set<Colegio>();

        public DbSet<Supervisor> Supervisores => Set<Supervisor>();

        public DbSet<Clase> Clases => Set<Clase>();

        public DbSet<Alumno> Alumnos => Set<Alumno>();

        public DbSet<Representante> Representantes => Set<Representante>();

        public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

        public DbSet<PersonaAutorizada> PersonasAutorizadas => Set<PersonaAutorizada>();

        public DbSet<FranjaRetiro> FranjasRetiro => Set<FranjaRetiro>();

        public DbSet<Deteccion> Detecciones => Set<Deteccion>();

        public DbSet<Recogida> Recogidas => Set<Recogida>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Mapeo de las entidades del dominio: Persistence/Configuraciones/*.
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
