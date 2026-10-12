using System.Security.Claims;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Areas.PanelPadres.Models;
using JuntalyColegios.Configuracion;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JuntalyColegios.Areas.PanelPadres.Controllers;

/// <summary>
/// Panel de la familia (/Padres): hijos, vehículos y personas autorizadas del padre autenticado. Los
/// casos de uso limitan todo a su propia familia; lo ajeno responde 404.
/// </summary>
[Area("Padres")]
[Authorize(Roles = Roles.Padre)]
[AutoValidateAntiforgeryToken]
[TraducirExcepciones]
public class InicioController : Controller
{
    private string UsuarioId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuario no autenticado.");

    public async Task<IActionResult> Index(
        [FromServices] IObtenerPanelPadreUseCase obtener, [FromServices] IObtenerHistorialPadreUseCase historial, CancellationToken ct) =>
        View(new PanelPadreViewModel
        {
            Panel = await obtener.EjecutarAsync(UsuarioId, ct),
            Historial = await historial.EjecutarAsync(UsuarioId, ct)
        });

    [HttpPost]
    public Task<IActionResult> RegistrarVehiculo(
        [Bind(Prefix = nameof(PanelPadreViewModel.NuevoVehiculo))] VehiculoFormulario formulario,
        [FromServices] IRegistrarMiVehiculoUseCase registrar,
        CancellationToken ct) =>
        EjecutarSiValidoAsync(() => registrar.EjecutarAsync(UsuarioId, formulario.ADatos(), ct),
            "Vehículo registrado. El colegio le asignará el tag RFID.");

    [HttpPost]
    public Task<IActionResult> EliminarVehiculo(Guid vehiculoId, [FromServices] IEliminarMiVehiculoUseCase eliminar, CancellationToken ct) =>
        EjecutarSiValidoAsync(() => eliminar.EjecutarAsync(UsuarioId, vehiculoId, ct), "Vehículo eliminado.");

    [HttpPost]
    public Task<IActionResult> AgregarAutorizado(
        [Bind(Prefix = nameof(PanelPadreViewModel.NuevoAutorizado))] AutorizadoFormulario formulario,
        [FromServices] IAgregarAutorizadoUseCase agregar,
        CancellationToken ct) =>
        EjecutarSiValidoAsync(() => agregar.EjecutarAsync(UsuarioId, formulario.ADatos(), ct),
            $"{formulario.Nombre} ya puede recoger a tus hijos.");

    [HttpPost]
    public Task<IActionResult> EliminarAutorizado(Guid autorizadoId, [FromServices] IEliminarAutorizadoUseCase eliminar, CancellationToken ct) =>
        EjecutarSiValidoAsync(() => eliminar.EjecutarAsync(UsuarioId, autorizadoId, ct), "Autorización retirada.");

    /// <summary>Valida el formulario, ejecuta la acción y vuelve al panel con el mensaje o el error.</summary>
    private async Task<IActionResult> EjecutarSiValidoAsync(Func<Task> accion, string mensaje)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        }
        else
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
        }
        return RedirectToAction(nameof(Index));
    }
}
