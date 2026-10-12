namespace JuntalyColegios.Application.Ports.Out;

/// <summary>Puerto de salida: cuentas de usuario (implementado con ASP.NET Core Identity).</summary>
public interface IServicioUsuarios
{
    /// <summary>
    /// Crea una cuenta con el rol indicado y le envía una invitación para que establezca su
    /// contraseña. Devuelve el id del usuario. Lanza DominioException si ya existe una cuenta con ese email.
    /// </summary>
    Task<string> CrearUsuarioInvitadoAsync(string email, string nombre, string rol, CancellationToken cancellationToken);

    /// <summary>Elimina una cuenta (compensación si el alta no se puede completar).</summary>
    Task EliminarUsuarioAsync(string usuarioId, CancellationToken cancellationToken);

    /// <summary>Cambia el email (y nombre de usuario) de la cuenta. Lanza DominioException si otra cuenta ya lo usa.</summary>
    Task CambiarEmailAsync(string usuarioId, string email, CancellationToken cancellationToken);
}
