using System.Security.Claims;
using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Areas.ColegioSupervisor.Controllers;
using JuntalyColegios.Areas.ColegioSupervisor.Models;
using JuntalyColegios.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace JuntalyColegios.Tests.Web;

public class RepresentantesControllerTests
{
    private const string UsuarioId = "supervisor-123";
    private readonly RepresentantesController _sut;

    public RepresentantesControllerTests()
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UsuarioId)], "Test"))
        };
        _sut = new RepresentantesController
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    private static RepresentanteFormulario Formulario() => new()
    {
        Nombre = "Carlos Pérez",
        Email = "carlos@familia.com",
        Telefono = "+58 412 1234567",
        AlumnoIds = [Guid.NewGuid()]
    };

    [Fact]
    public async Task Crear_DatosValidos_DaDeAltaYRedirigeAlDetalleAvisandoDelEmail()
    {
        var alta = Substitute.For<IAltaRepresentanteUseCase>();
        var id = Guid.NewGuid();
        alta.EjecutarAsync(UsuarioId, Arg.Any<AltaRepresentanteDto>(), Arg.Any<CancellationToken>()).Returns(id);
        var formulario = Formulario();

        var resultado = await _sut.Crear(formulario, alta, Substitute.For<IListarAlumnosParaVincularUseCase>(), CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(RepresentantesController.Detalle), redireccion.ActionName);
        Assert.Equal(id, redireccion.RouteValues!["id"]);
        Assert.Contains("email", (string)_sut.TempData["Mensaje"]!, StringComparison.OrdinalIgnoreCase);
        await alta.Received(1).EjecutarAsync(UsuarioId,
            Arg.Is<AltaRepresentanteDto>(d => d.Email == "carlos@familia.com" && d.AlumnoIds.SequenceEqual(formulario.AlumnoIds)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Crear_EmailYaRegistrado_VuelveAlFormularioConElError()
    {
        var alta = Substitute.For<IAltaRepresentanteUseCase>();
        alta.EjecutarAsync(default!, default!, default).ThrowsAsyncForAnyArgs(new DominioException("Ya existe una cuenta con ese email."));
        var alumnos = Substitute.For<IListarAlumnosParaVincularUseCase>();
        alumnos.EjecutarAsync(UsuarioId, Arg.Any<CancellationToken>()).Returns([]);

        var resultado = await _sut.Crear(Formulario(), alta, alumnos, CancellationToken.None);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.IsType<CrearRepresentanteViewModel>(vista.Model);
        Assert.Contains("Ya existe una cuenta con ese email.", _sut.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public async Task Crear_ModelStateInvalido_NoLlamaAlCasoDeUso()
    {
        var alta = Substitute.For<IAltaRepresentanteUseCase>();
        var alumnos = Substitute.For<IListarAlumnosParaVincularUseCase>();
        alumnos.EjecutarAsync(UsuarioId, Arg.Any<CancellationToken>()).Returns([]);
        _sut.ModelState.AddModelError("Email", "Obligatorio");

        var resultado = await _sut.Crear(Formulario(), alta, alumnos, CancellationToken.None);

        Assert.IsType<ViewResult>(resultado);
        await alta.DidNotReceiveWithAnyArgs().EjecutarAsync(default!, default!, default);
    }

    [Fact]
    public async Task AsignarTag_TagEnUso_VuelveAlDetalleConElError()
    {
        var representanteId = Guid.NewGuid();
        var vehiculoId = Guid.NewGuid();
        var asignar = Substitute.For<IAsignarTagUseCase>();
        asignar.EjecutarAsync(UsuarioId, vehiculoId, "E200AABB", Arg.Any<CancellationToken>())
            .ThrowsAsync(new DominioException("El tag ya está asignado al vehículo AAA111."));

        var resultado = await _sut.AsignarTag(representanteId, vehiculoId, "E200AABB", asignar, CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(RepresentantesController.Detalle), redireccion.ActionName);
        Assert.Equal(representanteId, redireccion.RouteValues!["id"]);
        Assert.Equal("El tag ya está asignado al vehículo AAA111.", _sut.TempData["Error"]);
    }

    [Fact]
    public async Task AgregarVehiculo_DatosValidos_LoAgregaYVuelveAlDetalle()
    {
        var representanteId = Guid.NewGuid();
        var agregar = Substitute.For<IAgregarVehiculoUseCase>();

        var resultado = await _sut.AgregarVehiculo(representanteId, new VehiculoFormulario { Placa = "AB123CD", Descripcion = "Corolla" }, agregar, CancellationToken.None);

        Assert.Equal(nameof(RepresentantesController.Detalle), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        await agregar.Received(1).EjecutarAsync(UsuarioId, representanteId, new DatosVehiculoDto("AB123CD", "Corolla"), Arg.Any<CancellationToken>());
    }
}
