using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace JuntalyColegios.Areas.ColegioSupervisor.Controllers;

/// <summary>
/// Pantalla de salida del colegio: alumnos llamados por la antena pendientes de entregar. La lista se
/// refresca sola (salida.js pide <see cref="Pendientes"/> cada pocos segundos).
/// </summary>
public class SalidaController : SupervisionController
{
    public async Task<IActionResult> Index([FromServices] IObtenerPantallaSalidaUseCase obtener, CancellationToken ct) =>
        View(await obtener.EjecutarAsync(UsuarioId, ct));

    /// <summary>Fragmento HTML con las recogidas pendientes, para la actualización automática.</summary>
    public async Task<IActionResult> Pendientes([FromServices] IObtenerPantallaSalidaUseCase obtener, CancellationToken ct) =>
        PartialView("_Pendientes", await obtener.EjecutarAsync(UsuarioId, ct));

    [HttpPost]
    public async Task<IActionResult> Simular(Guid vehiculoId, [FromServices] ISimularLlegadaUseCase simular, CancellationToken ct)
    {
        try
        {
            TempData["Simulacion"] = Explicar(await simular.EjecutarAsync(UsuarioId, vehiculoId, ct));
        }
        catch (DominioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Entregar(Guid recogidaId, [FromServices] IMarcarEntregadaUseCase marcar, CancellationToken ct)
    {
        try
        {
            await marcar.EjecutarAsync(UsuarioId, recogidaId, ct);
        }
        catch (DominioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private static string Explicar(ResultadoDeteccionDto r) => r.Resultado switch
    {
        ResultadoDeteccion.GeneraRecogida => $"Llegó {r.Placa}: se llama a {string.Join(", ", r.AlumnosLlamados)}.",
        ResultadoDeteccion.FueraDeFranja => $"Llegó {r.Placa} fuera de la franja de retiro: no se llama a nadie.",
        ResultadoDeteccion.Duplicada => $"{r.Placa} ya se había detectado o sus hijos ya están llamados.",
        ResultadoDeteccion.SinAlumnos => $"{r.Placa} es de una familia sin alumnos activos vinculados.",
        _ => $"Tag {r.Tag} desconocido."
    };
}
