namespace JuntalyColegios.Application.Seguridad;

/// <summary>Roles de la aplicación. El alta es en cascada: AdminApp → SupervisorColegio → Padre.</summary>
public static class Roles
{
    /// <summary>Administrador de la plataforma: gestiona colegios y sus supervisores.</summary>
    public const string AdminApp = "AdminApp";

    /// <summary>Supervisor de un colegio: gestiona clases, alumnos, padres, franjas y la salida.</summary>
    public const string SupervisorColegio = "SupervisorColegio";

    /// <summary>Padre o representante: consulta a sus hijos y gestiona vehículos y autorizados.</summary>
    public const string Padre = "Padre";

    public static readonly IReadOnlyList<string> Todos = [AdminApp, SupervisorColegio, Padre];
}
