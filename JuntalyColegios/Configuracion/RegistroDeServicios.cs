using Microsoft.Extensions.DependencyInjection.Extensions;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.UseCases;
using JuntalyColegios.Application.UseCases.Colegios;
using JuntalyColegios.Application.UseCases.Padres;
using JuntalyColegios.Application.UseCases.Salida;
using JuntalyColegios.Application.UseCases.Supervision;
using JuntalyColegios.Infrastructure.Email;
using JuntalyColegios.Infrastructure.Identity;
using JuntalyColegios.Infrastructure.Persistence.Repositorios;

namespace JuntalyColegios.Configuracion;

/// <summary>
/// Composition root de la arquitectura hexagonal: une cada puerto de Application con su adaptador de
/// Infrastructure y cada puerto de entrada con su caso de uso.
/// </summary>
public static class RegistroDeServicios
{
    public static IServiceCollection AddJuntalyColegios(this IServiceCollection services)
    {
        // Adaptadores (puertos de salida).
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EmailSenderIdentityUi>();
        services.AddScoped<IServicioUsuarios, ServicioUsuariosIdentity>();
        services.AddScoped<IRepositorioColegios, RepositorioColegiosEf>();
        services.AddScoped<IRepositorioSupervisores, RepositorioSupervisoresEf>();
        services.AddScoped<IRepositorioClases, RepositorioClasesEf>();
        services.AddScoped<IRepositorioAlumnos, RepositorioAlumnosEf>();
        services.AddScoped<IRepositorioRepresentantes, RepositorioRepresentantesEf>();
        services.AddScoped<IRepositorioVehiculos, RepositorioVehiculosEf>();
        services.AddScoped<IRepositorioAutorizados, RepositorioAutorizadosEf>();
        services.AddScoped<IRepositorioFranjas, RepositorioFranjasEf>();
        services.AddScoped<IRepositorioDetecciones, RepositorioDeteccionesEf>();
        services.AddScoped<IRepositorioRecogidas, RepositorioRecogidasEf>();

        // Reloj del sistema (en los tests se sustituye por uno controlado).
        services.TryAddSingleton(TimeProvider.System);

        // Casos de uso (puertos de entrada).
        services.AddScoped<ISolicitarDemoUseCase, SolicitarDemoUseCase>();
        services.AddScoped<ICrearColegioUseCase, CrearColegioUseCase>();
        services.AddScoped<IListarColegiosUseCase, ListarColegiosUseCase>();
        services.AddScoped<IObtenerColegioUseCase, ObtenerColegioUseCase>();
        services.AddScoped<IEditarColegioUseCase, EditarColegioUseCase>();
        services.AddScoped<ICambiarEstadoColegioUseCase, CambiarEstadoColegioUseCase>();
        services.AddScoped<IAltaSupervisorUseCase, AltaSupervisorUseCase>();

        // Área del supervisor: todos los casos de uso resuelven su colegio con ContextoSupervisor.
        services.AddScoped<ContextoSupervisor>();
        services.AddScoped<IListarClasesUseCase, ListarClasesUseCase>();
        services.AddScoped<ICrearClaseUseCase, CrearClaseUseCase>();
        services.AddScoped<IObtenerClaseUseCase, ObtenerClaseUseCase>();
        services.AddScoped<IEditarClaseUseCase, EditarClaseUseCase>();
        services.AddScoped<IEliminarClaseUseCase, EliminarClaseUseCase>();
        services.AddScoped<ICrearAlumnoUseCase, CrearAlumnoUseCase>();
        services.AddScoped<IObtenerAlumnoUseCase, ObtenerAlumnoUseCase>();
        services.AddScoped<IEditarAlumnoUseCase, EditarAlumnoUseCase>();
        services.AddScoped<ICambiarEstadoAlumnoUseCase, CambiarEstadoAlumnoUseCase>();
        services.AddScoped<IListarRepresentantesUseCase, ListarRepresentantesUseCase>();
        services.AddScoped<IListarAlumnosParaVincularUseCase, ListarAlumnosParaVincularUseCase>();
        services.AddScoped<IAltaRepresentanteUseCase, AltaRepresentanteUseCase>();
        services.AddScoped<IObtenerRepresentanteUseCase, ObtenerRepresentanteUseCase>();
        services.AddScoped<IVincularAlumnoUseCase, VincularAlumnoUseCase>();
        services.AddScoped<IDesvincularAlumnoUseCase, DesvincularAlumnoUseCase>();
        services.AddScoped<IAgregarVehiculoUseCase, AgregarVehiculoUseCase>();
        services.AddScoped<IAsignarTagUseCase, AsignarTagUseCase>();

        // Flujo RFID: lectura de la antena (o simulada) → recogidas → pantalla de salida.
        services.AddScoped<RegistroDeDetecciones>();
        services.AddScoped<IRegistrarDeteccionAntenaUseCase, RegistrarDeteccionAntenaUseCase>();
        services.AddScoped<ISimularLlegadaUseCase, SimularLlegadaUseCase>();
        services.AddScoped<IObtenerPantallaSalidaUseCase, ObtenerPantallaSalidaUseCase>();
        services.AddScoped<IMarcarEntregadaUseCase, MarcarEntregadaUseCase>();
        services.AddScoped<IGenerarClaveAntenaUseCase, GenerarClaveAntenaUseCase>();

        services.AddScoped<IListarFranjasUseCase, ListarFranjasUseCase>();
        services.AddScoped<ICrearFranjasUseCase, CrearFranjasUseCase>();
        services.AddScoped<IEliminarFranjaUseCase, EliminarFranjaUseCase>();

        // Panel de padres: todos los casos de uso resuelven la familia con ContextoPadre.
        services.AddScoped<ContextoPadre>();
        services.AddScoped<IObtenerPanelPadreUseCase, ObtenerPanelPadreUseCase>();
        services.AddScoped<IRegistrarMiVehiculoUseCase, RegistrarMiVehiculoUseCase>();
        services.AddScoped<IEliminarMiVehiculoUseCase, EliminarMiVehiculoUseCase>();
        services.AddScoped<IAgregarAutorizadoUseCase, AgregarAutorizadoUseCase>();
        services.AddScoped<IEliminarAutorizadoUseCase, EliminarAutorizadoUseCase>();

        return services;
    }
}
