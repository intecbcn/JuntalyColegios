using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace JuntalyColegios.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISolicitarDemoUseCase _solicitarDemoUseCase;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ISolicitarDemoUseCase solicitarDemoUseCase, ILogger<HomeController> logger)
        {
            _solicitarDemoUseCase = solicitarDemoUseCase;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Recibe el formulario "Solicita una demo" de la landing y lo envía a ventas.
        /// <see cref="SolicitudDemoRequest.Empresa2"/> es un honeypot: si llega relleno, se
        /// responde éxito igualmente para no revelar al bot que fue detectado, pero no se llama
        /// al caso de uso ni se envía nada.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarDemo(SolicitudDemoRequest request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request?.Empresa2))
                return Json(new { ok = true });

            if (request == null || string.IsNullOrWhiteSpace(request.Nombre)
                || string.IsNullOrWhiteSpace(request.Colegio) || string.IsNullOrWhiteSpace(request.Contacto))
            {
                return Json(new { ok = false, error = "Faltan datos del formulario." });
            }

            try
            {
                var dto = new SolicitudDemoDto(request.Nombre, request.Colegio, request.Contacto, request.NumeroAlumnos);
                await _solicitarDemoUseCase.EjecutarAsync(dto, cancellationToken);
            }
            catch (Exception ex)
            {
                // El visitante siempre ve "¡Gracias!" (no se le expone un fallo de infraestructura
                // de correo); queda registrado para que se revise el envío.
                _logger.LogError(ex, "Error al enviar la solicitud de demo de {Colegio}", request.Colegio);
            }

            return Json(new { ok = true });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
