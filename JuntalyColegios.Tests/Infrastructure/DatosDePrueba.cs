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

    public static async Task<Alumno> CrearAlumnoAsync(JuntalyWebApplicationFactory factory, Clase clase, string nombres, string apellidos)
    {
        var alumno = Alumno.Crear(clase, nombres, apellidos);
        await EnBaseDeDatosAsync(factory, db =>
        {
            db.Alumnos.Add(alumno);
            return db.SaveChangesAsync();
        });
        return alumno;
    }

    /// <summary>Crea un padre (cuenta con rol Padre) del colegio, vinculado a los alumnos indicados.</summary>
    public static async Task<Representante> CrearRepresentanteAsync(
        JuntalyWebApplicationFactory factory, Guid colegioId, string email, params Alumno[] hijos)
    {
        var usuario = await AutenticacionTests.CrearUsuarioAsync(factory, email, Roles.Padre);
        var padre = Representante.Crear(colegioId, usuario.Id, "Padre de prueba", Email.Crear(email), null);
        foreach (var hijo in hijos) padre.VincularAlumno(hijo);
        await EnBaseDeDatosAsync(factory, db =>
        {
            db.Representantes.Add(padre);
            return db.SaveChangesAsync();
        });
        return padre;
    }

    public static async Task<Vehiculo> CrearVehiculoAsync(
        JuntalyWebApplicationFactory factory, Representante padre, string placa, string? tag = null)
    {
        var vehiculo = Vehiculo.Crear(padre, Placa.Crear(placa), null);
        if (tag is not null) vehiculo.AsignarTag(TagRfid.Crear(tag));
        await EnBaseDeDatosAsync(factory, db =>
        {
            db.Vehiculos.Add(vehiculo);
            return db.SaveChangesAsync();
        });
        return vehiculo;
    }

    public static async Task EnBaseDeDatosAsync(JuntalyWebApplicationFactory factory, Func<ApplicationDbContext, Task> accion)
    {
        using var scope = factory.Services.CreateScope();
        await accion(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
}
