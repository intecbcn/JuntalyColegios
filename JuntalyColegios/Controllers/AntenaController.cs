using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Configuracion;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JuntalyColegios.Controllers;

/// <summary>
/// API para la antena RFID del colegio. No usa cookies ni antiforgery: se autentica con la clave del
/// colegio en la cabecera X-Antena-Clave.
/// </summary>
[ApiController]
[Route("api/antena")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[EnableRateLimiting(LimiteAntena.Politica)]
public class AntenaController : ControllerBase
{
    public const string CabeceraClave = "X-Antena-Clave";

    /// <summary>Registra una lectura: <c>POST /api/antena/detecciones</c> con <c>{ "tag": "E200..." }</c>.</summary>
    [HttpPost("detecciones")]
    public async Task<IActionResult> Registrar(
        [FromHeader(Name = CabeceraClave)] string? clave,
        [FromBody] LecturaAntenaRequest lectura,
        [FromServices] IRegistrarDeteccionAntenaUseCase registrar,
        CancellationToken ct)
    {
        try
        {
            var resultado = await registrar.EjecutarAsync(clave, lectura.Tag, ct);
            return Ok(new
            {
                resultado = resultado.Resultado.ToString(),
                tag = resultado.Tag,
                placa = resultado.Placa,
                alumnos = resultado.AlumnosLlamados
            });
        }
        catch (AccesoDenegadoException)
        {
            return Unauthorized();
        }
        catch (DominioException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
