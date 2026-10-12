using System.Security.Claims;
using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Areas.PanelPadres.Models;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PanelPadresController = JuntalyColegios.Areas.PanelPadres.Controllers.InicioController;

namespace JuntalyColegios.Tests.Web;

public class PanelPadresControllerTests
{
    private const string UsuarioId = "padre-123";
    private readonly PanelPadresController _sut;

    public PanelPadresControllerTests()
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UsuarioId)], "Test"))
        };
        _sut = new PanelPadresController
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task Index_MuestraElPanelDelPadreAutenticado()
    {
        var obtener = Substitute.For<IObtenerPanelPadreUseCase>();
        var panel = new PanelPadreDto("Carlos", "Colegio A", [], [], []);
        obtener.EjecutarAsync(UsuarioId, Arg.Any<CancellationToken>()).Returns(panel);

        var resultado = await _sut.Index(obtener, CancellationToken.None);

        var modelo = Assert.IsType<PanelPadreViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Same(panel, modelo.Panel);
    }

    [Fact]
    public async Task RegistrarVehiculo_DatosValidos_LoRegistraParaElPadreYVuelveAlPanel()
    {
        var registrar = Substitute.For<IRegistrarMiVehiculoUseCase>();

        var resultado = await _sut.RegistrarVehiculo(new VehiculoFormulario { Placa = "AB123CD", Descripcion = "Corolla" }, registrar, CancellationToken.None);

        Assert.Equal(nameof(PanelPadresController.Index), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        await registrar.Received(1).EjecutarAsync(UsuarioId, new DatosVehiculoDto("AB123CD", "Corolla"), Arg.Any<CancellationToken>());
        Assert.NotNull(_sut.TempData["Mensaje"]);
    }

    [Fact]
    public async Task RegistrarVehiculo_PlacaRepetida_VuelveAlPanelConElError()
    {
        var registrar = Substitute.For<IRegistrarMiVehiculoUseCase>();
        registrar.EjecutarAsync(default!, default!, default).ThrowsAsyncForAnyArgs(new DominioException("El vehículo AB123CD ya está registrado."));

        await _sut.RegistrarVehiculo(new VehiculoFormulario { Placa = "AB123CD" }, registrar, CancellationToken.None);

        Assert.Equal("El vehículo AB123CD ya está registrado.", _sut.TempData["Error"]);
    }

    [Fact]
    public async Task AgregarAutorizado_ModelStateInvalido_NoLlamaAlCasoDeUso()
    {
        var agregar = Substitute.For<IAgregarAutorizadoUseCase>();
        _sut.ModelState.AddModelError("Documento", "El documento es obligatorio.");

        await _sut.AgregarAutorizado(new AutorizadoFormulario { Nombre = "María" }, agregar, CancellationToken.None);

        Assert.Equal("El documento es obligatorio.", _sut.TempData["Error"]);
        await agregar.DidNotReceiveWithAnyArgs().EjecutarAsync(default!, default!, default);
    }

    [Fact]
    public async Task EliminarAutorizado_LlamaAlCasoDeUsoConElPadreAutenticado()
    {
        var eliminar = Substitute.For<IEliminarAutorizadoUseCase>();
        var id = Guid.NewGuid();

        await _sut.EliminarAutorizado(id, eliminar, CancellationToken.None);

        await eliminar.Received(1).EjecutarAsync(UsuarioId, id, Arg.Any<CancellationToken>());
    }
}
