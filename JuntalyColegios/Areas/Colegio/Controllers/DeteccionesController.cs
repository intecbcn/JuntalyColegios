using JuntalyColegios.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;

namespace JuntalyColegios.Areas.ColegioSupervisor.Controllers;

/// <summary>Registro de lecturas de la antena: incluye las que no llamaron a nadie (fuera de franja, repetidas...).</summary>
public class DeteccionesController : SupervisionController
{
    public async Task<IActionResult> Index([FromServices] IListarDeteccionesUseCase listar, CancellationToken ct) =>
        View(await listar.EjecutarAsync(UsuarioId, ct));
}
