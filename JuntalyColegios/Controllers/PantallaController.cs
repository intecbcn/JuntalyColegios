using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.In;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JuntalyColegios.Controllers;

/// <summary>
/// Pantalla de llamadas estilo turnero (TV de portería o tablet de aula). Se abre con un enlace secreto,
/// sin iniciar sesión, y es solo de lectura. Un enlace inválido o revocado responde 404, sin dar pistas.
/// </summary>
[AllowAnonymous]
[Route("Pantalla/{token}")]
public class PantallaController : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string token, [FromServices] IObtenerPantallaPublicaUseCase obtener, CancellationToken ct)
    {
        try
        {
            return View(await obtener.EjecutarAsync(token, ct));
        }
        catch (AccesoDenegadoException)
        {
            return NotFound();
        }
    }

    /// <summary>Fragmento con los llamados, para la actualización automática (pantalla.js).</summary>
    [HttpGet("Contenido")]
    public async Task<IActionResult> Contenido(string token, [FromServices] IObtenerPantallaPublicaUseCase obtener, CancellationToken ct)
    {
        try
        {
            return PartialView("_Turnos", await obtener.EjecutarAsync(token, ct));
        }
        catch (AccesoDenegadoException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// El enlace es la credencial: que no lo indexen buscadores, no viaje en el Referer a otras webs ni
    /// quede en cachés intermedias.
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var cabeceras = context.HttpContext.Response.Headers;
        cabeceras["X-Robots-Tag"] = "noindex, nofollow";
        cabeceras["Referrer-Policy"] = "no-referrer";
        cabeceras.CacheControl = "no-store";
        base.OnActionExecuting(context);
    }
}
