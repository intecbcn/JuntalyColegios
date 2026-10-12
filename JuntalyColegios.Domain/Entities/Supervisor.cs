using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;

namespace JuntalyColegios.Domain.Entities;

/// <summary>
/// Supervisor de un colegio: gestiona sus clases, alumnos, padres y la salida. Se vincula a una cuenta
/// de usuario por su id (el dominio no conoce Identity).
/// </summary>
public sealed class Supervisor
{
    public const int LongitudMaximaNombre = 100;

    private Supervisor()
    {
    }

    public Guid Id { get; private set; }

    public Guid ColegioId { get; private set; }

    public string UsuarioId { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public static Supervisor Crear(Guid colegioId, string usuarioId, string nombre, Email email)
    {
        if (colegioId == Guid.Empty)
            throw new DominioException("El supervisor debe pertenecer a un colegio.");
        if (string.IsNullOrWhiteSpace(usuarioId))
            throw new DominioException("El supervisor debe tener una cuenta de usuario.");

        return new Supervisor
        {
            Id = Guid.NewGuid(),
            ColegioId = colegioId,
            UsuarioId = usuarioId,
            Nombre = ValidarNombre(nombre),
            Email = email.Valor
        };
    }

    /// <summary>Valida todo antes de asignar: si algo falla, el supervisor no cambia.</summary>
    public void Actualizar(string nombre, Email email)
    {
        var nombreValido = ValidarNombre(nombre);
        Nombre = nombreValido;
        Email = email.Valor;
    }

    /// <summary>
    /// Valida el nombre por adelantado: el alta crea primero la cuenta de usuario y no debe crearla si
    /// el supervisor luego no es válido.
    /// </summary>
    public static string ValidarNombre(string nombre) =>
        Validacion.TextoObligatorio(nombre, "nombre", LongitudMaximaNombre);
}
