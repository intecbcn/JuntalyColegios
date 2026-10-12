using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Tests.Domain;

public class DeteccionYRecogidaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 12, 16, 5, 0, TimeSpan.Zero);

    [Fact]
    public void Deteccion_Crear_GuardaLecturaYResultado()
    {
        var colegioId = Guid.NewGuid();
        var vehiculoId = Guid.NewGuid();

        var deteccion = Deteccion.Crear(colegioId, "E200AABB", vehiculoId, Ahora, ResultadoDeteccion.FueraDeFranja, simulada: true);

        Assert.NotEqual(Guid.Empty, deteccion.Id);
        Assert.Equal(colegioId, deteccion.ColegioId);
        Assert.Equal("E200AABB", deteccion.Tag);
        Assert.Equal(vehiculoId, deteccion.VehiculoId);
        Assert.Equal(Ahora, deteccion.DetectadoEn);
        Assert.Equal(ResultadoDeteccion.FueraDeFranja, deteccion.Resultado);
        Assert.True(deteccion.Simulada);
    }

    [Fact]
    public void Recogida_Crear_QuedaPendiente()
    {
        var deteccion = Deteccion.Crear(Guid.NewGuid(), "E200AABB", Guid.NewGuid(), Ahora, ResultadoDeteccion.GeneraRecogida, false);
        var alumnoId = Guid.NewGuid();

        var recogida = Recogida.Crear(deteccion, alumnoId);

        Assert.Equal(deteccion.ColegioId, recogida.ColegioId);
        Assert.Equal(deteccion.Id, recogida.DeteccionId);
        Assert.Equal(deteccion.VehiculoId, recogida.VehiculoId);
        Assert.Equal(alumnoId, recogida.AlumnoId);
        Assert.Equal(Ahora, recogida.DetectadoEn);
        Assert.True(recogida.Pendiente);
        Assert.Null(recogida.EntregadoEn);
    }

    [Fact]
    public void Recogida_Crear_DeteccionSinVehiculo_LanzaDominioException()
    {
        var deteccion = Deteccion.Crear(Guid.NewGuid(), "E200AABB", null, Ahora, ResultadoDeteccion.TagDesconocido, false);

        Assert.Throws<DominioException>(() => Recogida.Crear(deteccion, Guid.NewGuid()));
    }

    [Fact]
    public void Recogida_MarcarEntregada_RegistraLaHora()
    {
        var recogida = Recogida.Crear(
            Deteccion.Crear(Guid.NewGuid(), "E200AABB", Guid.NewGuid(), Ahora, ResultadoDeteccion.GeneraRecogida, false), Guid.NewGuid());

        recogida.MarcarEntregada(Ahora.AddMinutes(3));

        Assert.False(recogida.Pendiente);
        Assert.Equal(Ahora.AddMinutes(3), recogida.EntregadoEn);
    }

    [Fact]
    public void Recogida_MarcarEntregadaDosVeces_LanzaDominioException()
    {
        var recogida = Recogida.Crear(
            Deteccion.Crear(Guid.NewGuid(), "E200AABB", Guid.NewGuid(), Ahora, ResultadoDeteccion.GeneraRecogida, false), Guid.NewGuid());
        recogida.MarcarEntregada(Ahora.AddMinutes(3));

        Assert.Throws<DominioException>(() => recogida.MarcarEntregada(Ahora.AddMinutes(4)));
        Assert.Equal(Ahora.AddMinutes(3), recogida.EntregadoEn);
    }

    [Fact]
    public void Colegio_EstablecerClaveAntena_GuardaSoloElHash()
    {
        var colegio = Colegio.Crear("Colegio", null, null);
        Assert.Null(colegio.HashClaveAntena);

        colegio.EstablecerHashClaveAntena("ABCDEF0123");

        Assert.Equal("ABCDEF0123", colegio.HashClaveAntena);
    }
}
