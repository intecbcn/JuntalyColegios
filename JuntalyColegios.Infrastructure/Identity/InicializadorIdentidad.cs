using JuntalyColegios.Application.Seguridad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JuntalyColegios.Infrastructure.Identity;

/// <summary>
/// Crea los roles de la aplicación y el administrador inicial al arrancar. Idempotente: si ya existen
/// no hace nada. El administrador se toma de la configuración "AdminInicial" (Email/Password), que en
/// producción llega por variables de entorno (AdminInicial__Email / AdminInicial__Password).
/// </summary>
public static class InicializadorIdentidad
{
    public static async Task InicializarAsync(IServiceProvider servicios)
    {
        using var scope = servicios.CreateScope();
        var proveedor = scope.ServiceProvider;
        var roles = proveedor.GetRequiredService<RoleManager<IdentityRole>>();
        var usuarios = proveedor.GetRequiredService<UserManager<IdentityUser>>();
        var configuracion = proveedor.GetRequiredService<IConfiguration>();
        var logger = proveedor.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(InicializadorIdentidad));

        foreach (var rol in Roles.Todos)
        {
            if (!await roles.RoleExistsAsync(rol))
                Validar(await roles.CreateAsync(new IdentityRole(rol)), $"crear el rol {rol}");
        }

        var email = configuracion["AdminInicial:Email"];
        var contrasena = configuracion["AdminInicial:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(contrasena))
        {
            logger.LogWarning("AdminInicial no configurado: no se crea el administrador inicial.");
            return;
        }

        var admin = await usuarios.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            Validar(await usuarios.CreateAsync(admin, contrasena), "crear el administrador inicial");
        }
        if (!await usuarios.IsInRoleAsync(admin, Roles.AdminApp))
            Validar(await usuarios.AddToRoleAsync(admin, Roles.AdminApp), "asignar el rol de administrador");
    }

    private static void Validar(IdentityResult resultado, string accion)
    {
        if (!resultado.Succeeded)
            throw new InvalidOperationException(
                $"No se pudo {accion}: {string.Join("; ", resultado.Errors.Select(e => e.Description))}");
    }
}
