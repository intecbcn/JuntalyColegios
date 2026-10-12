using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace JuntalyColegios.Tests.Infrastructure;

/// <summary>Datos de dominio sembrados directamente en la base de test (sin pasar por la UI).</summary>
public static class DatosDePrueba
{
    /// <summary>Crea un colegio y un supervisor vinculado a una cuenta con rol SupervisorColegio.</summary>
    public static async Task<Colegio> CrearColegioConSupervisorAsync(
        JuntalyWebApplicationFactory factory, string emailSupervisor, string nombreColegio = "Colegio de prueba", bool activo = true)
    {
        var usuario = await AutenticacionTests.CrearUsuarioAsync(factory, emailSupervisor, Roles.SupervisorColegio);
        var colegio = Colegio.Crear(nombreColegio, null, null);
        if (!activo) colegio.Desactivar();

        await EnBaseDeDatosAsync(factory, async db =>
        {
            db.Colegios.Add(colegio);
            db.Supervisores.Add(Supervisor.Crear(colegio.Id, usuario.Id, "Supervisor de prueba", Email.Crear(emailSupervisor)));
            await db.SaveChangesAsync();
        });
        return colegio;
    }

    public static async Task<Clase> CrearClaseAsync(JuntalyWebApplicationFactory factory, Guid colegioId, string nombre)
    {
        var clase = Clase.Crear(colegioId, nombre, null);
        await EnBaseDeDatosAsync(factory, db =>
        {
            db.Clases.Add(clase);
            return db.SaveChangesAsync();
        });
        return clase;
    }

    public static async Task EnBaseDeDatosAsync(JuntalyWebApplicationFactory factory, Func<ApplicationDbContext, Task> accion)
    {
        using var scope = factory.Services.CreateScope();
        await accion(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
}
