using JuntalyColegios.Application.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JuntalyColegios.Controllers;

/// <summary>Punto de entrada tras iniciar sesión: lleva a cada usuario a la zona de su rol.</summary>
[Authorize]
public class PanelController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole(Roles.AdminApp)) return LocalRedirect("/Admin");
        if (User.IsInRole(Roles.SupervisorColegio)) return LocalRedirect("/Colegio");
        if (User.IsInRole(Roles.Padre)) return LocalRedirect("/Padres");
        return LocalRedirect("/Identity/Account/Manage");
    }
}
