using JuntalyColegios.Application.Excepciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JuntalyColegios.Configuracion;

/// <summary>
/// Traduce las excepciones de Application a respuestas HTTP: acceso denegado → 403 (con cookie:
/// redirección a AccessDenied) y recurso inexistente o de otro colegio → 404. Así los controladores no
/// repiten try/catch para estos casos.
/// </summary>
public sealed class TraducirExcepcionesAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        context.Result = context.Exception switch
        {
            AccesoDenegadoException => new ForbidResult(),
            NoEncontradoException => new NotFoundResult(),
            _ => context.Result
        };
        if (context.Result is not null) context.ExceptionHandled = true;
    }
}
