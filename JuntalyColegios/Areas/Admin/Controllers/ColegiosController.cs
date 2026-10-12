using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Areas.Admin.Models;
using JuntalyColegios.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JuntalyColegios.Areas.Admin.Controllers;

/// <summary>
/// Gestión de colegios y alta de sus supervisores (solo AdminApp). Controlador delgado: cada acción
/// recibe por [FromServices] únicamente el caso de uso que necesita.
/// </summary>
[Area("Admin")]
[Authorize(Roles = Roles.AdminApp)]
[AutoValidateAntiforgeryToken]
public class ColegiosController : Controller
{
    public async Task<IActionResult> Index([FromServices] IListarColegiosUseCase listar, CancellationToken ct) =>
        View(await listar.EjecutarAsync(ct));

    [HttpGet]
    public IActionResult Crear() => View(new ColegioFormulario());

    [HttpPost]
    public async Task<IActionResult> Crear(ColegioFormulario formulario, [FromServices] ICrearColegioUseCase crear, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(formulario);
        try
        {
            var id = await crear.EjecutarAsync(formulario.ADatos(), ct);
            TempData["Mensaje"] = "Colegio creado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (DominioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }
    }

    public async Task<IActionResult> Detalle(Guid id, [FromServices] IObtenerColegioUseCase obtener, CancellationToken ct)
    {
        try
        {
            return View(new DetalleColegioViewModel { Detalle = await obtener.EjecutarAsync(id, ct) });
        }
        catch (NoEncontradoException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, [FromServices] IObtenerColegioUseCase obtener, CancellationToken ct)
    {
        try
        {
            var detalle = await obtener.EjecutarAsync(id, ct);
            ViewData["ColegioId"] = id;
            return View(ColegioFormulario.Desde(detalle.Colegio));
        }
        catch (NoEncontradoException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Editar(Guid id, ColegioFormulario formulario, [FromServices] IEditarColegioUseCase editar, CancellationToken ct)
    {
        ViewData["ColegioId"] = id;
        if (!ModelState.IsValid) return View(formulario);
        try
        {
            await editar.EjecutarAsync(id, formulario.ADatos(), ct);
            TempData["Mensaje"] = "Cambios guardados.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (NoEncontradoException)
        {
            return NotFound();
        }
        catch (DominioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }
    }

    /// <summary>Genera una clave de antena nueva y la muestra una única vez (solo se guarda su hash).</summary>
    [HttpPost]
    public async Task<IActionResult> GenerarClaveAntena(Guid id, [FromServices] IGenerarClaveAntenaUseCase generar, CancellationToken ct)
    {
        try
        {
            TempData["ClaveAntena"] = await generar.EjecutarAsync(id, ct);
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (NoEncontradoException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(Guid id, bool activo, [FromServices] ICambiarEstadoColegioUseCase cambiar, CancellationToken ct)
    {
        try
        {
            await cambiar.EjecutarAsync(id, activo, ct);
            TempData["Mensaje"] = activo ? "Colegio activado." : "Colegio desactivado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (NoEncontradoException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> AltaSupervisor(
        Guid id,
        [Bind(Prefix = nameof(DetalleColegioViewModel.NuevoSupervisor))] SupervisorFormulario nuevoSupervisor,
        [FromServices] IAltaSupervisorUseCase alta,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Detalle), new { id });
        }
        try
        {
            await alta.EjecutarAsync(new AltaSupervisorDto(id, nuevoSupervisor.Nombre, nuevoSupervisor.Email), ct);
            TempData["Mensaje"] = $"Supervisor dado de alta. Le hemos enviado un email a {nuevoSupervisor.Email} para que establezca su contraseña.";
        }
        catch (NoEncontradoException)
        {
            return NotFound();
        }
        catch (DominioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalle), new { id });
    }
}
