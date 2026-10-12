using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Areas.Admin.Controllers;
using JuntalyColegios.Areas.Admin.Models;
using JuntalyColegios.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace JuntalyColegios.Tests.Web;

/// <summary>Tests unitarios del controlador de colegios (área Admin) con los casos de uso mockeados.</summary>
public class ColegiosControllerTests
{
    private readonly ColegiosController _sut = new()
    {
        TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
    };

    private static ColegioFormulario FormularioValido() => new()
    {
        Nombre = "Colegio San José",
        Direccion = "Av. Principal 1",
        ZonaHoraria = "America/Caracas"
    };

    private static ColegioDetalleDto Detalle(Guid id) =>
        new(new ColegioDto(id, "Colegio San José", null, "America/Caracas", true), []);

    [Fact]
    public async Task Index_DevuelveLaListaDeColegios()
    {
        var listar = Substitute.For<IListarColegiosUseCase>();
        IReadOnlyList<ColegioDto> colegios = [new ColegioDto(Guid.NewGuid(), "Colegio A", null, "America/Caracas", true)];
        listar.EjecutarAsync(Arg.Any<CancellationToken>()).Returns(colegios);

        var resultado = await _sut.Index(listar, CancellationToken.None);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(colegios, vista.Model);
    }

    [Fact]
    public async Task CrearPost_DatosValidos_CreaYRedirigeAlDetalle()
    {
        var crear = Substitute.For<ICrearColegioUseCase>();
        var id = Guid.NewGuid();
        crear.EjecutarAsync(Arg.Any<DatosColegioDto>(), Arg.Any<CancellationToken>()).Returns(id);

        var resultado = await _sut.Crear(FormularioValido(), crear, CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(ColegiosController.Detalle), redireccion.ActionName);
        Assert.Equal(id, redireccion.RouteValues!["id"]);
        await crear.Received(1).EjecutarAsync(
            new DatosColegioDto("Colegio San José", "Av. Principal 1", "America/Caracas"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CrearPost_ModelStateInvalido_DevuelveLaVistaSinLlamarAlCasoDeUso()
    {
        var crear = Substitute.For<ICrearColegioUseCase>();
        _sut.ModelState.AddModelError("Nombre", "Obligatorio");

        var resultado = await _sut.Crear(FormularioValido(), crear, CancellationToken.None);

        Assert.IsType<ViewResult>(resultado);
        await crear.DidNotReceiveWithAnyArgs().EjecutarAsync(default!, default);
    }

    [Fact]
    public async Task CrearPost_ReglaDeDominioIncumplida_MuestraElErrorEnElFormulario()
    {
        var crear = Substitute.For<ICrearColegioUseCase>();
        crear.EjecutarAsync(Arg.Any<DatosColegioDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DominioException("La zona horaria no existe."));

        var resultado = await _sut.Crear(FormularioValido(), crear, CancellationToken.None);

        Assert.IsType<ViewResult>(resultado);
        Assert.False(_sut.ModelState.IsValid);
        Assert.Contains("La zona horaria no existe.", _sut.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public async Task Detalle_ColegioInexistente_Devuelve404()
    {
        var obtener = Substitute.For<IObtenerColegioUseCase>();
        obtener.EjecutarAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ThrowsAsync(new NoEncontradoException("Colegio no encontrado."));

        var resultado = await _sut.Detalle(Guid.NewGuid(), obtener, CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Detalle_ColegioExistente_DevuelveVistaConDetalleYFormularioDeSupervisor()
    {
        var id = Guid.NewGuid();
        var obtener = Substitute.For<IObtenerColegioUseCase>();
        obtener.EjecutarAsync(id, Arg.Any<CancellationToken>()).Returns(Detalle(id));

        var resultado = await _sut.Detalle(id, obtener, CancellationToken.None);

        var modelo = Assert.IsType<DetalleColegioViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(id, modelo.Detalle.Colegio.Id);
        Assert.NotNull(modelo.NuevoSupervisor);
    }

    [Fact]
    public async Task EditarPost_DatosValidos_ActualizaYRedirigeAlDetalle()
    {
        var id = Guid.NewGuid();
        var editar = Substitute.For<IEditarColegioUseCase>();

        var resultado = await _sut.Editar(id, FormularioValido(), editar, CancellationToken.None);

        Assert.Equal(nameof(ColegiosController.Detalle), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        await editar.Received(1).EjecutarAsync(id, Arg.Any<DatosColegioDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditarPost_ColegioInexistente_Devuelve404()
    {
        var editar = Substitute.For<IEditarColegioUseCase>();
        editar.EjecutarAsync(Arg.Any<Guid>(), Arg.Any<DatosColegioDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new NoEncontradoException("Colegio no encontrado."));

        var resultado = await _sut.Editar(Guid.NewGuid(), FormularioValido(), editar, CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task CambiarEstado_LlamaAlCasoDeUsoYRedirigeAlDetalle()
    {
        var id = Guid.NewGuid();
        var cambiar = Substitute.For<ICambiarEstadoColegioUseCase>();

        var resultado = await _sut.CambiarEstado(id, false, cambiar, CancellationToken.None);

        Assert.Equal(nameof(ColegiosController.Detalle), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        await cambiar.Received(1).EjecutarAsync(id, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AltaSupervisor_DatosValidos_DaDeAltaYAvisaDelEmailDeInvitacion()
    {
        var id = Guid.NewGuid();
        var alta = Substitute.For<IAltaSupervisorUseCase>();

        var resultado = await _sut.AltaSupervisor(id, new SupervisorFormulario { Nombre = "Luis", Email = "luis@colegio.com" }, alta, CancellationToken.None);

        Assert.Equal(nameof(ColegiosController.Detalle), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        await alta.Received(1).EjecutarAsync(new AltaSupervisorDto(id, "Luis", "luis@colegio.com"), Arg.Any<CancellationToken>());
        Assert.Contains("email", (string)_sut.TempData["Mensaje"]!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AltaSupervisor_EmailYaRegistrado_MuestraElErrorSinPerderElDetalle()
    {
        var alta = Substitute.For<IAltaSupervisorUseCase>();
        alta.EjecutarAsync(Arg.Any<AltaSupervisorDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DominioException("Ya existe una cuenta con ese email."));

        var resultado = await _sut.AltaSupervisor(Guid.NewGuid(), new SupervisorFormulario { Nombre = "Luis", Email = "luis@colegio.com" }, alta, CancellationToken.None);

        Assert.Equal(nameof(ColegiosController.Detalle), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        Assert.Equal("Ya existe una cuenta con ese email.", _sut.TempData["Error"]);
    }

    [Fact]
    public async Task AltaSupervisor_ModelStateInvalido_NoLlamaAlCasoDeUso()
    {
        var alta = Substitute.For<IAltaSupervisorUseCase>();
        _sut.ModelState.AddModelError("Email", "Email no válido");

        var resultado = await _sut.AltaSupervisor(Guid.NewGuid(), new SupervisorFormulario { Nombre = "Luis", Email = "x" }, alta, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(resultado);
        Assert.NotNull(_sut.TempData["Error"]);
        await alta.DidNotReceiveWithAnyArgs().EjecutarAsync(default!, default);
    }
}
