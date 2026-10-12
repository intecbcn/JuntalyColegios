using JuntalyColegios.Application.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JuntalyColegios.Areas.Admin.Controllers;

/// <summary>Portada del área de administración de la plataforma (solo AdminApp).</summary>
[Area("Admin")]
[Authorize(Roles = Roles.AdminApp)]
public class InicioController : Controller
{
    public IActionResult Index() => View();
}
