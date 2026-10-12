using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace JuntalyColegios.Tests.Infrastructure;

/// <summary>
/// Utilidades para los tests de integración que necesitan un usuario con sesión iniciada: crea el
/// usuario (con email confirmado) en la base de datos de test e inicia sesión por el formulario real
/// de Identity, de modo que el cliente devuelto lleva la cookie de autenticación.
/// </summary>
public static partial class AutenticacionTests
{
    public const string ContrasenaPorDefecto = "Prueba123!";

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenAntiforgery();

    public static async Task<HttpClient> CrearClienteAutenticadoAsync(
        this JuntalyWebApplicationFactory factory, string email, string contrasena = ContrasenaPorDefecto)
    {
        await CrearUsuarioAsync(factory, email, contrasena);

        var cliente = factory.CreateClient();
        var html = await cliente.GetStringAsync("/Identity/Account/Login");
        var token = TokenAntiforgery().Match(html).Groups[1].Value;

        var respuesta = await cliente.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = contrasena,
            ["__RequestVerificationToken"] = token
        }));
        respuesta.EnsureSuccessStatusCode();
        return cliente;
    }

    /// <summary>Crea el usuario con email confirmado si aún no existe (idempotente entre tests).</summary>
    public static async Task<IdentityUser> CrearUsuarioAsync(
        JuntalyWebApplicationFactory factory, string email, string contrasena = ContrasenaPorDefecto)
    {
        using var scope = factory.Services.CreateScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var usuario = await usuarios.FindByEmailAsync(email);
        if (usuario is not null) return usuario;

        usuario = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var resultado = await usuarios.CreateAsync(usuario, contrasena);
        if (!resultado.Succeeded)
            throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Description)));
        return usuario;
    }
}
