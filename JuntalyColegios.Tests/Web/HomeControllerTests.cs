using System.Text.Json;
using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Controllers;
using JuntalyColegios.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace JuntalyColegios.Tests.Web;

/// <summary>Tests unitarios del controlador: el caso de uso (puerto de entrada) se mockea.</summary>
public class HomeControllerTests
{
    private readonly ISolicitarDemoUseCase _useCase = Substitute.For<ISolicitarDemoUseCase>();
    private readonly HomeController _sut;

    public HomeControllerTests()
    {
        _sut = new HomeController(_useCase, NullLogger<HomeController>.Instance);
    }

    private static SolicitudDemoRequest RequestValido() => new()
    {
        Nombre = "Ana Pérez",
        Colegio = "Colegio San José",
        Contacto = "ana@colegio.com",
        NumeroAlumnos = 350
    };

    private static JsonElement LeerJson(IActionResult resultado)
    {
        var json = Assert.IsType<JsonResult>(resultado);
        return JsonSerializer.SerializeToElement(json.Value);
    }

    [Fact]
    public async Task SolicitarDemo_DatosValidos_LlamaAlCasoDeUsoYRespondeOk()
    {
        var resultado = await _sut.SolicitarDemo(RequestValido(), CancellationToken.None);

        Assert.True(LeerJson(resultado).GetProperty("ok").GetBoolean());
        await _useCase.Received(1).EjecutarAsync(
            new SolicitudDemoDto("Ana Pérez", "Colegio San José", "ana@colegio.com", 350),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SolicitarDemo_HoneypotRelleno_RespondeOkSinLlamarAlCasoDeUso()
    {
        var request = RequestValido();
        request.Empresa2 = "Bot S.A.";

        var resultado = await _sut.SolicitarDemo(request, CancellationToken.None);

        Assert.True(LeerJson(resultado).GetProperty("ok").GetBoolean());
        await _useCase.DidNotReceiveWithAnyArgs().EjecutarAsync(default!, default);
    }

    [Theory]
    [InlineData("", "Colegio", "ana@colegio.com")]
    [InlineData("Ana", " ", "ana@colegio.com")]
    [InlineData("Ana", "Colegio", "")]
    public async Task SolicitarDemo_FaltaUnCampoObligatorio_RespondeErrorSinLlamarAlCasoDeUso(string nombre, string colegio, string contacto)
    {
        var request = new SolicitudDemoRequest { Nombre = nombre, Colegio = colegio, Contacto = contacto };

        var resultado = await _sut.SolicitarDemo(request, CancellationToken.None);

        var json = LeerJson(resultado);
        Assert.False(json.GetProperty("ok").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("error").GetString()));
        await _useCase.DidNotReceiveWithAnyArgs().EjecutarAsync(default!, default);
    }

    [Fact]
    public async Task SolicitarDemo_ElEnvioFalla_RespondeOkSinPropagarLaExcepcion()
    {
        _useCase.EjecutarAsync(Arg.Any<SolicitudDemoDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP caído"));

        var resultado = await _sut.SolicitarDemo(RequestValido(), CancellationToken.None);

        Assert.True(LeerJson(resultado).GetProperty("ok").GetBoolean());
    }
}
