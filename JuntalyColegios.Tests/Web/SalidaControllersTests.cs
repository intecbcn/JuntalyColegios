using System.Security.Claims;
using System.Text.Json;
using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Areas.ColegioSupervisor.Controllers;
using JuntalyColegios.Controllers;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace JuntalyColegios.Tests.Web;

public class SalidaControllersTests
{
    private readonly IRegistrarDeteccionAntenaUseCase _antena = Substitute.For<IRegistrarDeteccionAntenaUseCase>();

    private static ResultadoDeteccionDto Resultado(ResultadoDeteccion resultado, params string[] alumnos) =>
        new(resultado, "E200AABB", "AB123CD", alumnos);

    [Fact]
    public async Task Antena_LecturaValida_Responde200ConElResultado()
    {
        _antena.EjecutarAsync("clave", "E200AABB", Arg.Any<CancellationToken>())
            .Returns(Resultado(ResultadoDeteccion.GeneraRecogida, "Sofía Pérez"));

        var respuesta = await new AntenaController().Registrar("clave", new LecturaAntenaRequest { Tag = "E200AABB" }, _antena, CancellationToken.None);

        var json = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(respuesta).Value);
        Assert.Equal("GeneraRecogida", json.GetProperty("resultado").GetString());
        Assert.Equal("Sofía Pérez", json.GetProperty("alumnos")[0].GetString());
    }

    [Fact]
    public async Task Antena_ClaveRechazada_Responde401()
    {
        _antena.EjecutarAsync(default, default!, default).ThrowsAsyncForAnyArgs(new AccesoDenegadoException("Clave de antena no válida."));

        var respuesta = await new AntenaController().Registrar("mala", new LecturaAntenaRequest { Tag = "E200AABB" }, _antena, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(respuesta);
    }

    [Fact]
    public async Task Antena_TagInvalido_Responde400()
    {
        _antena.EjecutarAsync(default, default!, default).ThrowsAsyncForAnyArgs(new DominioException("El tag RFID no es válido."));

        var respuesta = await new AntenaController().Registrar("clave", new LecturaAntenaRequest { Tag = "<x>" }, _antena, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(respuesta);
    }

    private static SalidaController Salida()
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "supervisor-1")], "Test"))
        };
        return new SalidaController
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    [Theory]
    [InlineData(ResultadoDeteccion.GeneraRecogida, "Sofía Pérez")]
    [InlineData(ResultadoDeteccion.FueraDeFranja, "fuera de la franja")]
    [InlineData(ResultadoDeteccion.Duplicada, "ya")]
    [InlineData(ResultadoDeteccion.SinAlumnos, "sin alumnos")]
    public async Task Simular_ExplicaElResultadoAlSupervisor(ResultadoDeteccion resultado, string textoEsperado)
    {
        var simular = Substitute.For<ISimularLlegadaUseCase>();
        simular.EjecutarAsync(default!, default, default).ReturnsForAnyArgs(Resultado(resultado, "Sofía Pérez"));
        var sut = Salida();

        var respuesta = await sut.Simular(Guid.NewGuid(), simular, CancellationToken.None);

        Assert.Equal(nameof(SalidaController.Index), Assert.IsType<RedirectToActionResult>(respuesta).ActionName);
        Assert.Contains(textoEsperado, (string)sut.TempData["Simulacion"]!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Entregar_RecogidaYaEntregada_AvisaDelError()
    {
        var marcar = Substitute.For<IMarcarEntregadaUseCase>();
        marcar.EjecutarAsync(default!, default, default).ThrowsAsyncForAnyArgs(new DominioException("La recogida ya estaba entregada."));
        var sut = Salida();

        await sut.Entregar(Guid.NewGuid(), marcar, CancellationToken.None);

        Assert.Equal("La recogida ya estaba entregada.", sut.TempData["Error"]);
    }
}
