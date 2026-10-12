using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Domain.Entities;

/// <summary>
/// Pantalla de llamadas de un colegio (TV de portería o tablet de un aula). Se abre con un enlace
/// secreto, sin iniciar sesión, y solo muestra a los alumnos llamados. Del enlace solo se guarda el hash.
/// </summary>
public sealed class PantallaLlamadas
{
    private PantallaLlamadas()
    {
    }

    public Guid Id { get; private set; }

    public Guid ColegioId { get; private set; }

    /// <summary>Null: portería (todos los alumnos del colegio). Con clase: solo los de esa aula.</summary>
    public Guid? ClaseId { get; private set; }

    public string HashToken { get; private set; } = string.Empty;

    public bool EsPorteria => ClaseId is null;

    public static PantallaLlamadas Crear(Guid colegioId, Guid? claseId, string hashToken)
    {
        if (colegioId == Guid.Empty)
            throw new DominioException("La pantalla debe pertenecer a un colegio.");

        var pantalla = new PantallaLlamadas { Id = Guid.NewGuid(), ColegioId = colegioId, ClaseId = claseId };
        pantalla.RegenerarToken(hashToken);
        return pantalla;
    }

    /// <summary>El enlace anterior deja de funcionar.</summary>
    public void RegenerarToken(string hashToken)
    {
        if (string.IsNullOrWhiteSpace(hashToken))
            throw new DominioException("El enlace de la pantalla no puede estar vacío.");
        HashToken = hashToken;
    }
}
