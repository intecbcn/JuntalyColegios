using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Application.UseCases.Salida;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EmailVO = JuntalyColegios.Domain.ValueObjects.Email;

namespace JuntalyColegios.Infrastructure.Demo;

/// <param name="Contrasena">Contraseña de las cuentas de prueba (supervisor y padre).</param>
/// <param name="ClaveAntena">Clave de antena del colegio de demo (opcional): permite probar la API.</param>
public sealed record OpcionesDemo(string Contrasena, string? ClaveAntena);

/// <summary>
/// Datos de demostración para presentar y probar la aplicación: un colegio completo con un supervisor y
/// un padre de prueba. Se activa con Demo__Sembrar=true y es idempotente (si el colegio de demo ya
/// existe no hace nada).
/// </summary>
public static class SembradorDemo
{
    public const string NombreColegio = "U. E. Colegio Demo Juntaly";
    public const string EmailSupervisor = "supervisor@demo.juntaly.com";
    public const string EmailPadre = "padre@demo.juntaly.com";
    public const string TagFamiliaDemo = "E20000172211014418900001";

    private const string ZonaHoraria = "America/Caracas";

    public static async Task SembrarAsync(IServiceProvider servicios, OpcionesDemo opciones)
    {
        using var scope = servicios.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var ahora = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();

        if (await db.Colegios.AnyAsync(c => c.Nombre == NombreColegio))
            return;

        var colegio = Colegio.Crear(NombreColegio, "Av. Francisco de Miranda, Caracas", ZonaHoraria);
        if (!string.IsNullOrWhiteSpace(opciones.ClaveAntena))
            colegio.EstablecerHashClaveAntena(ClaveDeAntena.Hash(opciones.ClaveAntena));
        db.Colegios.Add(colegio);

        var supervisorId = await CuentaAsync(usuarios, EmailSupervisor, Roles.SupervisorColegio, opciones.Contrasena);
        db.Supervisores.Add(Supervisor.Crear(colegio.Id, supervisorId, "Laura Supervisora", EmailVO.Crear(EmailSupervisor)));

        var primero = Clase.Crear(colegio.Id, "1.º A", "Primaria");
        var tercero = Clase.Crear(colegio.Id, "3.º B", "Primaria");
        var quinto = Clase.Crear(colegio.Id, "5.º Año", "Bachillerato");
        db.Clases.AddRange(primero, tercero, quinto);

        // Salida general todo el día (el revisor puede probar a cualquier hora, también desde otra zona
        // horaria) y una clase con horario propio,
        // para ver también una llegada fuera de franja.
        foreach (var dia in Enum.GetValues<DayOfWeek>())
            db.FranjasRetiro.Add(FranjaRetiro.Crear(colegio.Id, null, dia, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        foreach (var dia in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
            db.FranjasRetiro.Add(FranjaRetiro.Crear(colegio.Id, quinto.Id, dia, new TimeOnly(13, 0), new TimeOnly(14, 0)));

        // Familia del padre de prueba: una hija en primaria y un hijo en bachillerato.
        var sofia = Alumno.Crear(primero, "Sofía", "Martínez");
        var diego = Alumno.Crear(quinto, "Diego", "Martínez");
        var padreId = await CuentaAsync(usuarios, EmailPadre, Roles.Padre, opciones.Contrasena);
        var familiaDemo = Representante.Crear(colegio.Id, padreId, "Andrea Martínez", EmailVO.Crear(EmailPadre), "+58 412 5550101");
        familiaDemo.VincularAlumno(sofia);
        familiaDemo.VincularAlumno(diego);
        var carroDemo = Vehiculo.Crear(familiaDemo, Placa.Crear("AB123CD"), "Toyota Corolla gris");
        carroDemo.AsignarTag(TagRfid.Crear(TagFamiliaDemo));
        db.Alumnos.AddRange(sofia, diego);
        db.Representantes.Add(familiaDemo);
        db.Vehiculos.Add(carroDemo);
        db.PersonasAutorizadas.Add(PersonaAutorizada.Crear(familiaDemo, "Carmen Rivas", "V-8123456", "Abuela"));

        // Otras familias (sin contraseña: solo sirven para poblar la pantalla y el simulador).
        var otras = new[]
        {
            (Email: "familia.gomez@demo.juntaly.com", Nombre: "Luis Gómez", Hijo: ("Valentina", "Gómez", tercero), Placa: "XY987ZZ", Vehiculo: "Chevrolet Spark rojo", Tag: "E20000172211014418900002"),
            (Email: "familia.fernandez@demo.juntaly.com", Nombre: "María Fernández", Hijo: ("Pablo", "Fernández", primero), Placa: "KL456MN", Vehiculo: "Ford Explorer negra", Tag: "E20000172211014418900003"),
            (Email: "familia.rojas@demo.juntaly.com", Nombre: "Andrés Rojas", Hijo: ("Daniela", "Rojas", quinto), Placa: "GH321JK", Vehiculo: "Hyundai Tucson blanca", Tag: "E20000172211014418900004")
        };
        var vehiculosOtras = new Dictionary<string, Vehiculo>();
        foreach (var f in otras)
        {
            var hijo = Alumno.Crear(f.Hijo.Item3, f.Hijo.Item1, f.Hijo.Item2);
            var familia = Representante.Crear(colegio.Id, await CuentaAsync(usuarios, f.Email, Roles.Padre, null), f.Nombre, EmailVO.Crear(f.Email), null);
            familia.VincularAlumno(hijo);
            var vehiculo = Vehiculo.Crear(familia, Placa.Crear(f.Placa), f.Vehiculo);
            vehiculo.AsignarTag(TagRfid.Crear(f.Tag));
            db.Alumnos.Add(hijo);
            db.Representantes.Add(familia);
            db.Vehiculos.Add(vehiculo);
            vehiculosOtras[f.Placa] = vehiculo;
        }

        // Historial de los últimos días: recogidas entregadas de Sofía y lecturas que no llamaron a nadie.
        var hoy = DateOnly.FromDateTime(colegio.HoraLocal(ahora));
        for (var diasAtras = 1; diasAtras <= 3; diasAtras++)
        {
            var llegada = InstanteLocal(hoy.AddDays(-diasAtras), new TimeOnly(12, 10));
            var deteccion = Deteccion.Crear(colegio.Id, TagFamiliaDemo, carroDemo.Id, llegada, ResultadoDeteccion.GeneraRecogida, simulada: false);
            var recogida = Recogida.Crear(deteccion, sofia.Id);
            recogida.MarcarEntregada(llegada.AddMinutes(4));
            db.Detecciones.Add(deteccion);
            db.Recogidas.Add(recogida);
        }
        // La familia Rojas solo tiene una hija en 5.º Año (sale de 13:00 a 14:00 entre semana): a las 10:00 de
        // un día laborable su coche se detecta fuera de franja.
        var laborable = hoy.AddDays(-1);
        while (laborable.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) laborable = laborable.AddDays(-1);
        var carroRojas = vehiculosOtras["GH321JK"];
        db.Detecciones.Add(Deteccion.Crear(colegio.Id, carroRojas.Tag!, carroRojas.Id,
            InstanteLocal(laborable, new TimeOnly(10, 0)), ResultadoDeteccion.FueraDeFranja, simulada: false));
        db.Detecciones.Add(Deteccion.Crear(colegio.Id, "E200FFFF00000000DEADBEEF", null,
            InstanteLocal(hoy.AddDays(-2), new TimeOnly(12, 30)), ResultadoDeteccion.TagDesconocido, simulada: false));

        await db.SaveChangesAsync();
    }

    /// <summary>Cuenta confirmada con el rol indicado; si ya existe se reutiliza.</summary>
    private static async Task<string> CuentaAsync(UserManager<IdentityUser> usuarios, string email, string rol, string? contrasena)
    {
        var usuario = await usuarios.FindByEmailAsync(email);
        if (usuario is null)
        {
            usuario = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var creado = contrasena is null ? await usuarios.CreateAsync(usuario) : await usuarios.CreateAsync(usuario, contrasena);
            if (!creado.Succeeded)
                throw new InvalidOperationException($"No se pudo crear la cuenta de demo {email}: {string.Join(" ", creado.Errors.Select(e => e.Description))}");
        }
        if (!await usuarios.IsInRoleAsync(usuario, rol))
            await usuarios.AddToRoleAsync(usuario, rol);
        return usuario.Id;
    }

    private static DateTimeOffset InstanteLocal(DateOnly dia, TimeOnly hora)
    {
        var local = dia.ToDateTime(hora);
        var zona = TimeZoneInfo.FindSystemTimeZoneById(ZonaHoraria);
        return new DateTimeOffset(local, zona.GetUtcOffset(local)).ToUniversalTime();
    }
}
