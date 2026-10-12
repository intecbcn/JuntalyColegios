using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Areas.ColegioSupervisor.Models;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Models;
using Microsoft.AspNetCore.Mvc;

namespace JuntalyColegios.Areas.ColegioSupervisor.Controllers;

/// <summary>Padres y representantes del colegio: alta con invitación, hijos y vehículos con su tag RFID.</summary>
public class RepresentantesController : SupervisionController
{
    public async Task<IActionResult> Index([FromServices] IListarRepresentantesUseCase listar, CancellationToken ct) =>
        View(await listar.EjecutarAsync(UsuarioId, ct));

    [HttpGet]
    public async Task<IActionResult> Crear([FromServices] IListarAlumnosParaVincularUseCase alumnos, CancellationToken ct) =>
        View(new CrearRepresentanteViewModel { Formulario = new RepresentanteFormulario(), Alumnos = await alumnos.EjecutarAsync(UsuarioId, ct) });

    [HttpPost]
    public async Task<IActionResult> Crear(
        [Bind(Prefix = "")] RepresentanteFormulario formulario,
        [FromServices] IAltaRepresentanteUseCase alta,
        [FromServices] IListarAlumnosParaVincularUseCase alumnos,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await alta.EjecutarAsync(UsuarioId, formulario.ADto(), ct);
                TempData["Mensaje"] = $"Alta completada. Le hemos enviado un email a {formulario.Email} para que establezca su contraseña.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
            catch (DominioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }
        return View(new CrearRepresentanteViewModel { Formulario = formulario, Alumnos = await alumnos.EjecutarAsync(UsuarioId, ct) });
    }

    public async Task<IActionResult> Detalle(Guid id, [FromServices] IObtenerRepresentanteUseCase obtener, CancellationToken ct) =>
        View(new DetalleRepresentanteViewModel { Detalle = await obtener.EjecutarAsync(UsuarioId, id, ct) });

    [HttpPost]
    public Task<IActionResult> Vincular(Guid id, Guid alumnoId, [FromServices] IVincularAlumnoUseCase vincular, CancellationToken ct) =>
        EjecutarYVolverAsync(id, () => vincular.EjecutarAsync(UsuarioId, id, alumnoId, ct), "Hijo vinculado.");

    [HttpPost]
    public Task<IActionResult> Desvincular(Guid id, Guid alumnoId, [FromServices] IDesvincularAlumnoUseCase desvincular, CancellationToken ct) =>
        EjecutarYVolverAsync(id, () => desvincular.EjecutarAsync(UsuarioId, id, alumnoId, ct), "Hijo desvinculado.");

    [HttpPost]
    public Task<IActionResult> AgregarVehiculo(
        Guid id,
        [Bind(Prefix = nameof(DetalleRepresentanteViewModel.NuevoVehiculo))] VehiculoFormulario formulario,
        [FromServices] IAgregarVehiculoUseCase agregar,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ErroresDeValidacion();
            return Task.FromResult<IActionResult>(RedirectToAction(nameof(Detalle), new { id }));
        }
        return EjecutarYVolverAsync(id, () => agregar.EjecutarAsync(UsuarioId, id, formulario.ADatos(), ct), "Vehículo añadido.");
    }

    /// <param name="id">Representante cuyo detalle se vuelve a mostrar.</param>
    [HttpPost]
    public Task<IActionResult> AsignarTag(Guid id, Guid vehiculoId, string? tag, [FromServices] IAsignarTagUseCase asignar, CancellationToken ct) =>
        EjecutarYVolverAsync(id, () => asignar.EjecutarAsync(UsuarioId, vehiculoId, tag, ct),
            string.IsNullOrWhiteSpace(tag) ? "Tag retirado." : "Tag asignado.");

    /// <summary>Ejecuta la acción y vuelve al detalle con el mensaje de éxito o el error de negocio.</summary>
    private async Task<IActionResult> EjecutarYVolverAsync(Guid id, Func<Task> accion, string mensaje)
    {
        try
        {
            await accion();
            TempData["Mensaje"] = mensaje;
        }
        catch (DominioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalle), new { id });
    }
}
