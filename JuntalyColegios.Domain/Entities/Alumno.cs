using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Domain.Entities;

/// <summary>
/// Alumno de un colegio. Pertenece siempre a una clase de su mismo colegio: se crea a partir de la
/// clase y no puede pasar a una clase de otro colegio.
/// </summary>
public sealed class Alumno
{
    public const int LongitudMaximaNombres = 100;
    public const int LongitudMaximaApellidos = 100;

    private Alumno()
    {
    }

    public Guid Id { get; private set; }

    public Guid ColegioId { get; private set; }

    public Guid ClaseId { get; private set; }

    public string Nombres { get; private set; } = string.Empty;

    public string Apellidos { get; private set; } = string.Empty;

    /// <summary>Un alumno dado de baja deja de contar en su clase y no se llama a la salida.</summary>
    public bool Activo { get; private set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public static Alumno Crear(Clase clase, string nombres, string apellidos)
    {
        var alumno = new Alumno { Id = Guid.NewGuid(), ColegioId = clase.ColegioId, ClaseId = clase.Id, Activo = true };
        alumno.Actualizar(nombres, apellidos);
        return alumno;
    }

    public void Actualizar(string nombres, string apellidos)
    {
        var nombresValidos = Validacion.TextoObligatorio(nombres, "nombres", LongitudMaximaNombres);
        var apellidosValidos = Validacion.TextoObligatorio(apellidos, "apellidos", LongitudMaximaApellidos);
        Nombres = nombresValidos;
        Apellidos = apellidosValidos;
    }

    public void CambiarDeClase(Clase clase)
    {
        if (clase.ColegioId != ColegioId)
            throw new DominioException("El alumno solo puede cambiar a una clase de su mismo colegio.");
        ClaseId = clase.Id;
    }

    public void DarDeBaja() => Activo = false;

    public void Reactivar() => Activo = true;
}
