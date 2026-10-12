using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;

namespace JuntalyColegios.Domain.Entities;

/// <summary>
/// Padre, madre o tutor de uno o varios alumnos de un colegio. Tiene cuenta de usuario (rol Padre),
/// vehículos con tag RFID y, más adelante, personas autorizadas a recoger a sus hijos.
/// </summary>
public sealed class Representante
{
    public const int LongitudMaximaNombre = 100;
    public const int LongitudMaximaTelefono = 30;

    private readonly List<VinculoAlumno> _alumnos = [];

    private Representante()
    {
    }

    public Guid Id { get; private set; }

    public Guid ColegioId { get; private set; }

    public string UsuarioId { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string? Telefono { get; private set; }

    /// <summary>Hijos a cargo (alumnos del mismo colegio).</summary>
    public IReadOnlyCollection<VinculoAlumno> Alumnos => _alumnos;

    public static Representante Crear(Guid colegioId, string usuarioId, string nombre, Email email, string? telefono)
    {
        if (colegioId == Guid.Empty)
            throw new DominioException("El representante debe pertenecer a un colegio.");
        if (string.IsNullOrWhiteSpace(usuarioId))
            throw new DominioException("El representante debe tener una cuenta de usuario.");

        return new Representante
        {
            Id = Guid.NewGuid(),
            ColegioId = colegioId,
            UsuarioId = usuarioId,
            Nombre = ValidarNombre(nombre),
            Email = email.Valor,
            Telefono = ValidarTelefono(telefono)
        };
    }

    /// <summary>Validaciones expuestas para comprobar los datos antes de crear la cuenta de usuario.</summary>
    public static string ValidarNombre(string nombre) =>
        Validacion.TextoObligatorio(nombre, "nombre", LongitudMaximaNombre);

    public static string? ValidarTelefono(string? telefono) =>
        Validacion.TextoOpcional(telefono, "teléfono", LongitudMaximaTelefono);

    public bool EsRepresentanteDe(Guid alumnoId) => _alumnos.Any(v => v.AlumnoId == alumnoId);

    /// <summary>Vincula un hijo del mismo colegio. Vincularlo dos veces no tiene efecto.</summary>
    public void VincularAlumno(Alumno alumno)
    {
        if (alumno.ColegioId != ColegioId)
            throw new DominioException("Solo se pueden vincular alumnos del mismo colegio.");
        if (!EsRepresentanteDe(alumno.Id))
            _alumnos.Add(new VinculoAlumno(alumno.Id));
    }

    public void DesvincularAlumno(Guid alumnoId) => _alumnos.RemoveAll(v => v.AlumnoId == alumnoId);
}

/// <summary>Vínculo padre ↔ hijo (alumno del mismo colegio).</summary>
public sealed class VinculoAlumno
{
    private VinculoAlumno()
    {
    }

    internal VinculoAlumno(Guid alumnoId) => AlumnoId = alumnoId;

    public Guid AlumnoId { get; private set; }
}
