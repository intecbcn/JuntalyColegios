#nullable enable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JuntalyColegios.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Registro público cerrado: las cuentas se crean en cascada (el administrador da de alta a los
    /// supervisores y estos a los padres). Cualquier acceso a esta página vuelve al login.
    /// </summary>
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost() => RedirectToPage("./Login");
    }
}
