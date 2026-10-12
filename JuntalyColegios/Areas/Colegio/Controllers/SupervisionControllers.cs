using System.Security.Claims;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Areas.ColegioSupervisor.Models;
using JuntalyColegios.Configuracion;
using JuntalyColegios.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// El namespace no es "Areas.Colegio" para no ocultar la entidad de dominio Colegio.
namespace JuntalyColegios.Areas.ColegioSupervisor.Controllers;

/// <summary>
/// Base del área del supervisor (/Colegio): rol obligatorio, antiforgery en los POST y traducción de
/// AccesoDenegado/NoEncontrado a 403/404. El colegio lo resuelven los casos de uso a partir del usuario.
/// </summary>
[Area("Colegio")]
[Authorize(Roles = Roles.SupervisorColegio)]
[AutoValidateAntiforgeryToken]
[TraducirExcepciones]
public abstract class SupervisionController : Controller
{
    protected string UsuarioId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuario no autenticado.");

    protected string ErroresDeValidacion() =>
        string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
}

public class InicioController : SupervisionController
{
    public async Task<IActionResult> Index([FromServices] IListarClasesUseCase listar, CancellationToken ct) =>
        View(await listar.EjecutarAsync(UsuarioId, ct));
}

public class ClasesController : SupervisionController
{
    [HttpGet]
    public IActionResult Crear() => View(new ClaseFormulario());

    [HttpPost]
    public async Task<IActionResult> Crear(ClaseFormulario formulario, [FromServices] ICrearClaseUseCase crear, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(formulario);
        try
        {
            var id = await crear.EjecutarAsync(UsuarioId, formulario.ADatos(), ct);
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (DominioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }
    }

    public async Task<IActionResult> Detalle(Guid id, [FromServices] IObtenerClaseUseCase obtener, CancellationToken ct) =>
        View(new DetalleClaseViewModel
        {
            Detalle = await obtener.EjecutarAsync(UsuarioId, id, ct),
            NuevoAlumno = new AlumnoFormulario { ClaseId = id }
        });

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, [FromServices] IObtenerClaseUseCase obtener, CancellationToken ct)
    {
        var clase = await obtener.EjecutarAsync(UsuarioId, id, ct);
        ViewData["ClaseId"] = id;
        return View(new ClaseFormulario { Nombre = clase.Nombre, Nivel = clase.Nivel });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(Guid id, ClaseFormulario formulario, [FromServices] IEditarClaseUseCase editar, CancellationToken ct)
    {
        ViewData["ClaseId"] = id;
        if (!ModelState.IsValid) return View(formulario);
        try
        {
            await editar.EjecutarAsync(UsuarioId, id, formulario.ADatos(), ct);
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (DominioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(Guid id, [FromServices] IEliminarClaseUseCase eliminar, CancellationToken ct)
    {
        try
        {
            await eliminar.EjecutarAsync(UsuarioId, id, ct);
            TempData["Mensaje"] = "Clase eliminada.";
            return RedirectToAction(nameof(InicioController.Index), "Inicio");
        }
        catch (DominioException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Detalle), new { id });
        }
    }
}

public class AlumnosController : SupervisionController
{
    [HttpPost]
    public async Task<IActionResult> Crear(
        [Bind(Prefix = nameof(DetalleClaseViewModel.NuevoAlumno))] AlumnoFormulario formulario,
        [FromServices] ICrearAlumnoUseCase crear,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ErroresDeValidacion();
        }
        else
        {
            try
            {
                await crear.EjecutarAsync(UsuarioId, formulario.ADatos(), ct);
                TempData["Mensaje"] = $"{formulario.Nombres} {formulario.Apellidos} añadido a la clase.";
            }
            catch (DominioException ex)
            {
                TempData["Error"] = ex.Message;
            }
        }
        return RedirectToAction(nameof(ClasesController.Detalle), "Clases", new { id = formulario.ClaseId });
    }

    [HttpGet]
    public async Task<IActionResult> Editar(
        Guid id, [FromServices] IObtenerAlumnoUseCase obtener, [FromServices] IListarClasesUseCase listarClases, CancellationToken ct)
    {
        var alumno = await obtener.EjecutarAsync(UsuarioId, id, ct);
        return View(await ModeloEdicionAsync(
            id, alumno.Activo, new AlumnoFormulario { ClaseId = alumno.ClaseId, Nombres = alumno.Nombres, Apellidos = alumno.Apellidos },
            listarClases, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Editar(
        Guid id,
        [Bind(Prefix = nameof(EditarAlumnoViewModel.Alumno))] AlumnoFormulario formulario,
        [FromServices] IEditarAlumnoUseCase editar,
        [FromServices] IObtenerAlumnoUseCase obtener,
        [FromServices] IListarClasesUseCase listarClases,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await editar.EjecutarAsync(UsuarioId, id, formulario.ADatos(), ct);
                TempData["Mensaje"] = "Alumno actualizado.";
                return RedirectToAction(nameof(ClasesController.Detalle), "Clases", new { id = formulario.ClaseId });
            }
            catch (DominioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }
        var alumno = await obtener.EjecutarAsync(UsuarioId, id, ct);
        return View(await ModeloEdicionAsync(id, alumno.Activo, formulario, listarClases, ct));
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(
        Guid id, bool activo, Guid claseId, [FromServices] ICambiarEstadoAlumnoUseCase cambiar, CancellationToken ct)
    {
        await cambiar.EjecutarAsync(UsuarioId, id, activo, ct);
        TempData["Mensaje"] = activo ? "Alumno reactivado." : "Alumno dado de baja.";
        return RedirectToAction(nameof(ClasesController.Detalle), "Clases", new { id = claseId });
    }

    private async Task<EditarAlumnoViewModel> ModeloEdicionAsync(
        Guid id, bool activo, AlumnoFormulario formulario, IListarClasesUseCase listarClases, CancellationToken ct) => new()
    {
        AlumnoId = id,
        Activo = activo,
        Alumno = formulario,
        Clases = (await listarClases.EjecutarAsync(UsuarioId, ct)).Clases
    };
}
