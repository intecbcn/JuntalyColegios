using JuntalyColegios.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;

namespace JuntalyColegios.Areas.ColegioSupervisor.Controllers;

/// <summary>
/// Pantallas de llamada del colegio (portería y aulas): el supervisor genera sus enlaces secretos, que se
/// muestran una sola vez, y los revoca.
/// </summary>
public class PantallasController : SupervisionController
{
    public async Task<IActionResult> Index([FromServices] IListarPantallasUseCase listar, CancellationToken ct) =>
        View(await listar.EjecutarAsync(UsuarioId, ct));

    /// <summary>Crea la pantalla (portería si no hay clase) o regenera su enlace.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear(
        Guid? claseId, [FromServices] ICrearPantallaUseCase crear, [FromServices] IConfiguration configuracion, CancellationToken ct)
    {
        var token = await crear.EjecutarAsync(UsuarioId, claseId, ct);
        var urlBase = configuracion["App:UrlBase"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
        TempData["EnlacePantalla"] = $"{urlBase}/Pantalla/{token}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Revocar(Guid id, [FromServices] IRevocarPantallaUseCase revocar, CancellationToken ct)
    {
        await revocar.EjecutarAsync(UsuarioId, id, ct);
        TempData["Mensaje"] = "Pantalla revocada: su enlace ya no funciona.";
        return RedirectToAction(nameof(Index));
    }
}
