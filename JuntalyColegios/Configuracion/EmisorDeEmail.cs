using JuntalyColegios.Infrastructure.Email;

namespace JuntalyColegios.Configuracion;

/// <summary>
/// Elige el adaptador de email: SMTP siempre, salvo en Development sin servidor configurado, donde los
/// emails se escriben en la consola para poder probar la app en local. Fuera de Development la falta de
/// SMTP debe notarse (no se pierden invitaciones en silencio).
/// </summary>
public static class EmisorDeEmail
{
    public static Type Elegir(IConfiguration configuracion, IHostEnvironment entorno) =>
        entorno.IsDevelopment() && string.IsNullOrWhiteSpace(configuracion["Smtp:Host"])
            ? typeof(EmailEnRegistro)
            : typeof(SmtpEmailSender);
}
