using System.Text.RegularExpressions;
using JuntalyColegios.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JuntalyColegios.Tests.Infrastructure;

/// <summary>
/// Utilidades para los tests de integración que necesitan un usuario con sesión iniciada: crea el
/// usuario (con email confirmado y los roles pedidos) en la base de datos de test e inicia sesión por
/// el formulario real de Identity, de modo que el cliente devuelto lleva la cookie de autenticación.
/// </summary>
public static partial class AutenticacionTests
{
    public const string ContrasenaPorDefecto = "Prueba123!";

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenAntiforgery();

    /// <summary>Extrae el token antiforgery de un formulario HTML.</summary>
    public static string ExtraerTokenAntiforgery(string html) => TokenAntiforgery().Match(html).Groups[1].Value;

    public static async Task<HttpClient> CrearClienteAutenticadoAsync(
        this JuntalyWebApplicationFactory factory, string email, params string[] roles)
    {
        await CrearUsuarioAsync(factory, email, roles);
        return await IniciarSesionAsync(factory, email, ContrasenaPorDefecto);
    }

    /// <summary>
    /// Inicia sesión con un usuario existente. El cliente no sigue redirecciones, para que los tests
    /// puedan comprobar 302 a Login/AccessDenied.
    /// </summary>
    public static async Task<HttpClient> IniciarSesionAsync(
        this JuntalyWebApplicationFactory factory, string email, string contrasena)
    {
        var cliente = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = ExtraerTokenAntiforgery(await cliente.GetStringAsync("/Identity/Account/Login"));

        var respuesta = await cliente.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = contrasena,
            ["__RequestVerificationToken"] = token
        }));
        if (respuesta.StatusCode != System.Net.HttpStatusCode.Redirect)
            throw new InvalidOperationException($"No se pudo iniciar sesión como {email} (HTTP {(int)respuesta.StatusCode}).");
        return cliente;
    }

    /// <summary>
    /// Crea el usuario con email confirmado si aún no existe y le asigna los roles (creándolos en la
    /// base de test si faltan). Idempotente entre tests.
    /// </summary>
    public static async Task<IdentityUser> CrearUsuarioAsync(
        JuntalyWebApplicationFactory factory, string email, params string[] roles)
    {
        using var scope = factory.Services.CreateScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var usuario = await usuarios.FindByEmailAsync(email);
        if (usuario is null)
        {
            usuario = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var resultado = await usuarios.CreateAsync(usuario, ContrasenaPorDefecto);
            if (!resultado.Succeeded)
                throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Description)));
        }

        foreach (var rol in roles)
        {
            var normalizado = rol.ToUpperInvariant();
            if (!db.Roles.Any(r => r.NormalizedName == normalizado))
            {
                db.Roles.Add(new IdentityRole(rol) { NormalizedName = normalizado });
                await db.SaveChangesAsync();
            }
            if (!await usuarios.IsInRoleAsync(usuario, rol))
                await usuarios.AddToRoleAsync(usuario, rol);
        }
        return usuario;
    }
}
