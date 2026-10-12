#nullable enable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JuntalyColegios.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Sin registro público no hay confirmación que mostrar. La plantilla original generaba un enlace
    /// de confirmación válido para cualquier email y revelaba qué emails tienen cuenta, así que la
    /// página solo redirige al login.
    /// </summary>
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        public IActionResult OnGet() => RedirectToPage("./Login");
    }
}
