using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.UseCases;
using JuntalyColegios.Application.UseCases.Colegios;
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
        services.AddScoped<IServicioUsuarios, ServicioUsuariosIdentity>();
        services.AddScoped<IRepositorioColegios, RepositorioColegiosEf>();
        services.AddScoped<IRepositorioSupervisores, RepositorioSupervisoresEf>();

        // Casos de uso (puertos de entrada).
        services.AddScoped<ISolicitarDemoUseCase, SolicitarDemoUseCase>();
        services.AddScoped<ICrearColegioUseCase, CrearColegioUseCase>();
        services.AddScoped<IListarColegiosUseCase, ListarColegiosUseCase>();
        services.AddScoped<IObtenerColegioUseCase, ObtenerColegioUseCase>();
        services.AddScoped<IEditarColegioUseCase, EditarColegioUseCase>();
        services.AddScoped<ICambiarEstadoColegioUseCase, CambiarEstadoColegioUseCase>();
        services.AddScoped<IAltaSupervisorUseCase, AltaSupervisorUseCase>();

        return services;
    }
}
