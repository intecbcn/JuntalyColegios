using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.UseCases;
using JuntalyColegios.Tests.Infrastructure;

namespace JuntalyColegios.Tests.Application;

/// <summary>
/// Tests unitarios del caso de uso: solo dependen del puerto <c>IEmailSender</c> (doble en memoria),
/// sin ASP.NET Core, EF Core ni SMTP — la ventaja del aislamiento hexagonal.
/// </summary>
public class SolicitarDemoUseCaseTests
{
    private readonly FakeEmailSender _emailSender = new();
    private readonly SolicitarDemoUseCase _sut;

    public SolicitarDemoUseCaseTests()
    {
        _sut = new SolicitarDemoUseCase(_emailSender);
    }

    [Fact]
    public async Task EjecutarAsync_SolicitudValida_EnviaUnCorreoAVentasConLosDatos()
    {
        var solicitud = new SolicitudDemoDto("Ana Pérez", "Colegio San José", "ana@colegio.com", 350);

        var resultado = await _sut.EjecutarAsync(solicitud, CancellationToken.None);

        Assert.True(resultado);
        var correo = Assert.Single(_emailSender.Enviados);
        Assert.Equal("info@serviciosintec.com", correo.Destinatario);
        Assert.Contains("solicitud de demo", correo.Asunto, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ana Pérez", correo.CuerpoHtml);
        Assert.Contains("Colegio San José", correo.CuerpoHtml);
        Assert.Contains("ana@colegio.com", correo.CuerpoHtml);
        Assert.Contains("350", correo.CuerpoHtml);
    }

    [Fact]
    public async Task EjecutarAsync_SinNumeroDeAlumnos_IndicaNoIndicado()
    {
        var solicitud = new SolicitudDemoDto("Ana", "Colegio", "0414-0000000", null);

        await _sut.EjecutarAsync(solicitud, CancellationToken.None);

        Assert.Contains("No indicado", Assert.Single(_emailSender.Enviados).CuerpoHtml);
    }

    [Fact]
    public async Task EjecutarAsync_DatosConHtml_SeEscapanEnElCuerpoDelCorreo()
    {
        // El cuerpo se envía como HTML: lo que escribe el visitante no debe poder inyectar marcado
        // (enlaces o scripts) en el correo que recibe ventas.
        var solicitud = new SolicitudDemoDto("<a href=\"http://malicioso\">Clic</a>", "<script>x</script>", "ana@colegio.com", null);

        await _sut.EjecutarAsync(solicitud, CancellationToken.None);

        var cuerpo = Assert.Single(_emailSender.Enviados).CuerpoHtml;
        Assert.DoesNotContain("<a href", cuerpo);
        Assert.DoesNotContain("<script>", cuerpo);
        Assert.Contains("&lt;script&gt;", cuerpo);
    }

    [Fact]
    public async Task EjecutarAsync_FallaElEnvio_PropagaLaExcepcion()
    {
        _emailSender.FallarEnvios = true;
        var solicitud = new SolicitudDemoDto("Ana", "Colegio", "ana@colegio.com", null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.EjecutarAsync(solicitud, CancellationToken.None));
    }
}
