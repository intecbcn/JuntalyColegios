using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Domain.Entities;

/// <summary>Colegio cliente de la plataforma. Lo gestiona el administrador (AdminApp).</summary>
public sealed class Colegio
{
    public const int LongitudMaximaNombre = 150;
    public const int LongitudMaximaDireccion = 250;
    public const int LongitudMaximaZonaHoraria = 64;

    /// <summary>Público de referencia: colegios de Venezuela.</summary>
    public const string ZonaHorariaPorDefecto = "America/Caracas";

    private Colegio()
    {
    }

    public Guid Id { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public string? Direccion { get; private set; }

    /// <summary>Zona horaria IANA: las franjas de retiro se evalúan en la hora local del colegio.</summary>
    public string ZonaHoraria { get; private set; } = ZonaHorariaPorDefecto;

    public bool Activo { get; private set; }

    /// <summary>
    /// Hash de la clave con la que se identifica la antena del colegio. La clave en claro no se guarda:
    /// se muestra una sola vez al generarla.
    /// </summary>
    public string? HashClaveAntena { get; private set; }

    public void EstablecerHashClaveAntena(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new DominioException("El hash de la clave de antena no puede estar vacío.");
        HashClaveAntena = hash;
    }

    public static Colegio Crear(string nombre, string? direccion, string? zonaHoraria)
    {
        var colegio = new Colegio { Id = Guid.NewGuid(), Activo = true };
        colegio.Actualizar(nombre, direccion, zonaHoraria);
        return colegio;
    }

    /// <summary>Valida todos los datos antes de asignar ninguno: si algo falla, el colegio no cambia.</summary>
    public void Actualizar(string nombre, string? direccion, string? zonaHoraria)
    {
        var nombreValido = Validacion.TextoObligatorio(nombre, "nombre", LongitudMaximaNombre);
        var direccionValida = Validacion.TextoOpcional(direccion, "dirección", LongitudMaximaDireccion);
        var zonaValida = ValidarZonaHoraria(zonaHoraria);

        Nombre = nombreValido;
        Direccion = direccionValida;
        ZonaHoraria = zonaValida;
    }

    /// <summary>Fecha y hora del instante en la zona horaria del colegio (las franjas se definen en hora local).</summary>
    public DateTime HoraLocal(DateTimeOffset instante) =>
        TimeZoneInfo.ConvertTime(instante, TimeZoneInfo.FindSystemTimeZoneById(ZonaHoraria)).DateTime;

    public void Desactivar() => Activo = false;

    public void Activar() => Activo = true;

    private static string ValidarZonaHoraria(string? zonaHoraria)
    {
        var zona = string.IsNullOrWhiteSpace(zonaHoraria) ? ZonaHorariaPorDefecto : zonaHoraria.Trim();
        if (zona.Length > LongitudMaximaZonaHoraria || !TimeZoneInfo.TryFindSystemTimeZoneById(zona, out _))
            throw new DominioException($"La zona horaria \"{zona}\" no existe.");
        return zona;
    }
}
