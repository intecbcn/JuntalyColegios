using System.Buffers.Text;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace JuntalyColegios.Infrastructure.Identity;

/// <summary>
/// Adaptador de <see cref="IServicioUsuarios"/> sobre ASP.NET Core Identity. La cuenta se crea sin
/// contraseña y con el email confirmado (la da de alta alguien de confianza); el usuario recibe un
/// enlace para establecer su contraseña, así nadie más llega a conocerla.
/// </summary>
public sealed class ServicioUsuariosIdentity(
    UserManager<IdentityUser> usuarios, IEmailSender emailSender, IConfiguration configuracion) : IServicioUsuarios
{
    private static readonly HtmlEncoder Html = HtmlEncoder.Create(UnicodeRanges.All);

    public async Task<string> CrearUsuarioInvitadoAsync(string email, string nombre, string rol, CancellationToken cancellationToken)
    {
        var urlBase = UrlBase();
        if (await usuarios.FindByEmailAsync(email) is not null)
            throw new DominioException("Ya existe una cuenta con ese email.");

        var usuario = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        Validar(await usuarios.CreateAsync(usuario));
        try
        {
            Validar(await usuarios.AddToRoleAsync(usuario, rol));

            // Mismo formato que espera la página ResetPassword de Identity (Base64Url del token).
            var token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
            var codigo = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(token));
            var cuerpo = $"""
                Hola, {Html.Encode(nombre)}:<br><br>
                Te hemos dado de alta en Juntaly Colegios. Para empezar, establece tu contraseña aquí:<br>
                <a href="{urlBase}/Identity/Account/ResetPassword?code={codigo}">Establecer mi contraseña</a><br><br>
                Si el enlace caduca, usa «¿Olvidaste tu contraseña?» en la pantalla de inicio de sesión.
                """;
            await emailSender.EnviarAsync(email, "Bienvenido a Juntaly Colegios", cuerpo, cancellationToken);
        }
        catch
        {
            // Sin rol o sin invitación la cuenta no sirve y bloquearía reintentar el alta: se deshace.
            await usuarios.DeleteAsync(usuario);
            throw;
        }

        return usuario.Id;
    }

    public async Task EliminarUsuarioAsync(string usuarioId, CancellationToken cancellationToken)
    {
        if (await usuarios.FindByIdAsync(usuarioId) is { } usuario)
            await usuarios.DeleteAsync(usuario);
    }

    private string UrlBase() =>
        configuracion["App:UrlBase"]?.TrimEnd('/') is { Length: > 0 } url
            ? url
            : throw new InvalidOperationException("Falta la configuración App:UrlBase (URL pública de la aplicación).");

    private static void Validar(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
            throw new DominioException(string.Join(" ", resultado.Errors.Select(e => e.Description)));
    }
}
