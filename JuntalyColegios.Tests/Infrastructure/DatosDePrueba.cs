using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Application.UseCases.Salida;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

    public static async Task CrearFranjaAsync(
        JuntalyWebApplicationFactory factory, Guid colegioId, DayOfWeek dia, string inicio, string fin, Guid? claseId = null)
    {
        var franja = FranjaRetiro.Crear(colegioId, claseId, dia, TimeOnly.Parse(inicio), TimeOnly.Parse(fin));
        await EnBaseDeDatosAsync(factory, db =>
        {
            db.FranjasRetiro.Add(franja);
            return db.SaveChangesAsync();
        });
    }

    /// <summary>Genera la clave de antena del colegio (guardando solo su hash) y la devuelve.</summary>
    public static async Task<string> EstablecerClaveAntenaAsync(JuntalyWebApplicationFactory factory, Guid colegioId)
    {
        var clave = ClaveDeAntena.Generar();
        await EnBaseDeDatosAsync(factory, async db =>
        {
            var colegio = await db.Colegios.SingleAsync(c => c.Id == colegioId);
            colegio.EstablecerHashClaveAntena(ClaveDeAntena.Hash(clave));
            await db.SaveChangesAsync();
        });
        return clave;
    }

    /// <summary>Crea una pantalla de llamadas (portería si no hay clase) y devuelve su token secreto.</summary>
    public static async Task<string> CrearPantallaAsync(JuntalyWebApplicationFactory factory, Guid colegioId, Guid? claseId = null)
    {
        var token = TokenSeguro.Generar();
        await EnBaseDeDatosAsync(factory, db =>
        {
            db.PantallasLlamadas.Add(PantallaLlamadas.Crear(colegioId, claseId, TokenSeguro.Hash(token)));
            return db.SaveChangesAsync();
        });
        return token;
    }

    public static async Task EnBaseDeDatosAsync(JuntalyWebApplicationFactory factory, Func<ApplicationDbContext, Task> accion)
    {
        using var scope = factory.Services.CreateScope();
        await accion(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
}
