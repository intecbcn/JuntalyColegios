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

public class FranjasControllerTests
{
    private const string UsuarioId = "supervisor-123";
    private readonly FranjasController _sut;

    public FranjasControllerTests()
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UsuarioId)], "Test"))
        };
        _sut = new FranjasController
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    private static FranjaFormulario Formulario() => new()
    {
        Dias = [DayOfWeek.Monday, DayOfWeek.Friday],
        HoraInicio = new TimeOnly(12, 0),
        HoraFin = new TimeOnly(13, 0)
    };

    [Fact]
    public async Task Crear_DatosValidos_CreaLasFranjasYVuelveAlListado()
    {
        var crear = Substitute.For<ICrearFranjasUseCase>();
        crear.EjecutarAsync(default!, default!, default).ReturnsForAnyArgs(2);

        var resultado = await _sut.Crear(Formulario(), crear, CancellationToken.None);

        Assert.Equal(nameof(FranjasController.Index), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        await crear.Received(1).EjecutarAsync(UsuarioId,
            Arg.Is<DatosFranjasDto>(d => d.ClaseId == null && d.Dias.SequenceEqual(new[] { DayOfWeek.Monday, DayOfWeek.Friday })
                && d.Inicio == new TimeOnly(12, 0) && d.Fin == new TimeOnly(13, 0)),
            Arg.Any<CancellationToken>());
        Assert.Contains("2", (string)_sut.TempData["Mensaje"]!);
    }

    [Fact]
    public async Task Crear_Solape_VuelveAlListadoConElError()
    {
        var crear = Substitute.For<ICrearFranjasUseCase>();
        crear.EjecutarAsync(default!, default!, default).ThrowsAsyncForAnyArgs(new DominioException("Se solapa el miércoles."));

        await _sut.Crear(Formulario(), crear, CancellationToken.None);

        Assert.Equal("Se solapa el miércoles.", _sut.TempData["Error"]);
    }

    [Fact]
    public async Task Crear_ModelStateInvalido_NoLlamaAlCasoDeUso()
    {
        var crear = Substitute.For<ICrearFranjasUseCase>();
        _sut.ModelState.AddModelError("HoraInicio", "Indica la hora de inicio.");

        await _sut.Crear(Formulario(), crear, CancellationToken.None);

        Assert.Equal("Indica la hora de inicio.", _sut.TempData["Error"]);
        await crear.DidNotReceiveWithAnyArgs().EjecutarAsync(default!, default!, default);
    }
}
