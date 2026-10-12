using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Tests.Domain;

public class PantallaLlamadasTests
{
    [Fact]
    public void Crear_Porteria_SinClase()
    {
        var colegioId = Guid.NewGuid();

        var pantalla = PantallaLlamadas.Crear(colegioId, null, "HASH1");

        Assert.NotEqual(Guid.Empty, pantalla.Id);
        Assert.Equal(colegioId, pantalla.ColegioId);
        Assert.Null(pantalla.ClaseId);
        Assert.True(pantalla.EsPorteria);
        Assert.Equal("HASH1", pantalla.HashToken);
    }

    [Fact]
    public void Crear_DeAula_ConSuClase()
    {
        var claseId = Guid.NewGuid();

        var pantalla = PantallaLlamadas.Crear(Guid.NewGuid(), claseId, "HASH1");

        Assert.Equal(claseId, pantalla.ClaseId);
        Assert.False(pantalla.EsPorteria);
    }

    [Theory]
    [InlineData(true, "HASH")]
    [InlineData(false, "")]
    public void Crear_SinColegioOSinHash_LanzaDominioException(bool sinColegio, string hash)
    {
        Assert.Throws<DominioException>(() => PantallaLlamadas.Crear(sinColegio ? Guid.Empty : Guid.NewGuid(), null, hash));
    }

    [Fact]
    public void RegenerarToken_SustituyeElHash()
    {
        var pantalla = PantallaLlamadas.Crear(Guid.NewGuid(), null, "HASH1");

        pantalla.RegenerarToken("HASH2");

        Assert.Equal("HASH2", pantalla.HashToken);
    }
}
