using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Areas.ColegioSupervisor.Models;
using JuntalyColegios.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace JuntalyColegios.Areas.ColegioSupervisor.Controllers;

/// <summary>
/// Franjas horarias de retiro: solo dentro de ellas se llama a los alumnos cuando la antena detecta un
/// vehículo.
/// </summary>
public class FranjasController : SupervisionController
{
    public async Task<IActionResult> Index([FromServices] IListarFranjasUseCase listar, CancellationToken ct) =>
        View(await listar.EjecutarAsync(UsuarioId, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(FranjaFormulario formulario, [FromServices] ICrearFranjasUseCase crear, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ErroresDeValidacion();
            return RedirectToAction(nameof(Index));
        }
        try
        {
            var creadas = await crear.EjecutarAsync(UsuarioId, formulario.ADatos(), ct);
            TempData["Mensaje"] = creadas == 1 ? "Franja añadida." : $"{creadas} franjas añadidas.";
        }
        catch (DominioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(Guid id, [FromServices] IEliminarFranjaUseCase eliminar, CancellationToken ct)
    {
        await eliminar.EjecutarAsync(UsuarioId, id, ct);
        TempData["Mensaje"] = "Franja eliminada.";
        return RedirectToAction(nameof(Index));
    }
}
