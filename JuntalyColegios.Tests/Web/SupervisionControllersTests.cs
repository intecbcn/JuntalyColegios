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

/// <summary>Controladores del área del supervisor: pasan el usuario autenticado a los casos de uso.</summary>
public class SupervisionControllersTests
{
    private const string UsuarioId = "supervisor-123";

    private static T ConUsuario<T>(T controlador) where T : Controller
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UsuarioId)], "Test"))
        };
        controlador.ControllerContext = new ControllerContext { HttpContext = http };
        controlador.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return controlador;
    }

    [Fact]
    public async Task Inicio_ListaLasClasesDelSupervisorAutenticado()
    {
        var listar = Substitute.For<IListarClasesUseCase>();
        var clases = new ClasesDelColegioDto("Colegio A", []);
        listar.EjecutarAsync(UsuarioId, Arg.Any<CancellationToken>()).Returns(clases);
        var sut = ConUsuario(new InicioController());

        var resultado = await sut.Index(listar, CancellationToken.None);

        Assert.Same(clases, Assert.IsType<ViewResult>(resultado).Model);
    }

    [Fact]
    public async Task CrearClase_DatosValidos_CreaParaElSupervisorYRedirigeAlDetalle()
    {
        var crear = Substitute.For<ICrearClaseUseCase>();
        var id = Guid.NewGuid();
        crear.EjecutarAsync(UsuarioId, new DatosClaseDto("3.º A", "Primaria"), Arg.Any<CancellationToken>()).Returns(id);
        var sut = ConUsuario(new ClasesController());

        var resultado = await sut.Crear(new ClaseFormulario { Nombre = "3.º A", Nivel = "Primaria" }, crear, CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(ClasesController.Detalle), redireccion.ActionName);
        Assert.Equal(id, redireccion.RouteValues!["id"]);
    }

    [Fact]
    public async Task CrearClase_ModelStateInvalido_DevuelveLaVistaSinLlamarAlCasoDeUso()
    {
        var crear = Substitute.For<ICrearClaseUseCase>();
        var sut = ConUsuario(new ClasesController());
        sut.ModelState.AddModelError("Nombre", "Obligatorio");

        var resultado = await sut.Crear(new ClaseFormulario(), crear, CancellationToken.None);

        Assert.IsType<ViewResult>(resultado);
        await crear.DidNotReceiveWithAnyArgs().EjecutarAsync(default!, default!, default);
    }

    [Fact]
    public async Task CrearClase_NombreRepetido_MuestraElErrorEnElFormulario()
    {
        var crear = Substitute.For<ICrearClaseUseCase>();
        crear.EjecutarAsync(default!, default!, default).ThrowsAsyncForAnyArgs(new DominioException("Ya existe una clase con ese nombre."));
        var sut = ConUsuario(new ClasesController());

        var resultado = await sut.Crear(new ClaseFormulario { Nombre = "3.º A" }, crear, CancellationToken.None);

        Assert.IsType<ViewResult>(resultado);
        Assert.Contains("Ya existe una clase con ese nombre.", sut.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public async Task EliminarClase_ConAlumnos_VuelveAlDetalleConElError()
    {
        var id = Guid.NewGuid();
        var eliminar = Substitute.For<IEliminarClaseUseCase>();
        eliminar.EjecutarAsync(UsuarioId, id, Arg.Any<CancellationToken>()).ThrowsAsync(new DominioException("La clase tiene alumnos."));
        var sut = ConUsuario(new ClasesController());

        var resultado = await sut.Eliminar(id, eliminar, CancellationToken.None);

        Assert.Equal(nameof(ClasesController.Detalle), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        Assert.Equal("La clase tiene alumnos.", sut.TempData["Error"]);
    }

    [Fact]
    public async Task CrearAlumno_DatosValidos_LoCreaYVuelveALaClase()
    {
        var claseId = Guid.NewGuid();
        var crear = Substitute.For<ICrearAlumnoUseCase>();
        var sut = ConUsuario(new AlumnosController());

        var resultado = await sut.Crear(new AlumnoFormulario { ClaseId = claseId, Nombres = "Sofía", Apellidos = "Pérez" }, crear, CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Clases", redireccion.ControllerName);
        Assert.Equal(claseId, redireccion.RouteValues!["id"]);
        await crear.Received(1).EjecutarAsync(UsuarioId, new DatosAlumnoDto(claseId, "Sofía", "Pérez"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CrearAlumno_ModelStateInvalido_NoLlamaAlCasoDeUsoYAvisaDelError()
    {
        var crear = Substitute.For<ICrearAlumnoUseCase>();
        var sut = ConUsuario(new AlumnosController());
        sut.ModelState.AddModelError("Nombres", "Los nombres son obligatorios.");

        var resultado = await sut.Crear(new AlumnoFormulario { ClaseId = Guid.NewGuid() }, crear, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Los nombres son obligatorios.", sut.TempData["Error"]);
        await crear.DidNotReceiveWithAnyArgs().EjecutarAsync(default!, default!, default);
    }
}
