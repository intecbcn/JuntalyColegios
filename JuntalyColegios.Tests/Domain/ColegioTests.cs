using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Tests.Domain;

public class ColegioTests
{
    [Fact]
    public void Crear_DatosValidos_CreaColegioActivoConDatosNormalizados()
    {
        var colegio = Colegio.Crear("  Colegio San José  ", "  Av. Principal 1 ", "America/Bogota");

        Assert.NotEqual(Guid.Empty, colegio.Id);
        Assert.Equal("Colegio San José", colegio.Nombre);
        Assert.Equal("Av. Principal 1", colegio.Direccion);
        Assert.Equal("America/Bogota", colegio.ZonaHoraria);
        Assert.True(colegio.Activo);
    }

    [Fact]
    public void Crear_SinZonaHoraria_UsaLaDeCaracas()
    {
        var colegio = Colegio.Crear("Colegio San José", null, null);

        Assert.Equal("America/Caracas", colegio.ZonaHoraria);
        Assert.Null(colegio.Direccion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_NombreVacio_LanzaDominioException(string nombre)
    {
        var ex = Assert.Throws<DominioException>(() => Colegio.Crear(nombre, null, null));
        Assert.Contains("nombre", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Crear_NombreDemasiadoLargo_LanzaDominioException()
    {
        Assert.Throws<DominioException>(() => Colegio.Crear(new string('a', Colegio.LongitudMaximaNombre + 1), null, null));
    }

    [Fact]
    public void Crear_DireccionDemasiadoLarga_LanzaDominioException()
    {
        Assert.Throws<DominioException>(() => Colegio.Crear("Colegio", new string('a', Colegio.LongitudMaximaDireccion + 1), null));
    }

    [Fact]
    public void Crear_ZonaHorariaInexistente_LanzaDominioException()
    {
        var ex = Assert.Throws<DominioException>(() => Colegio.Crear("Colegio", null, "Marte/Olympus"));
        Assert.Contains("zona horaria", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Actualizar_DatosValidos_CambiaLosDatos()
    {
        var colegio = Colegio.Crear("Colegio A", null, null);

        colegio.Actualizar("Colegio B", "Calle 2", "America/Lima");

        Assert.Equal("Colegio B", colegio.Nombre);
        Assert.Equal("Calle 2", colegio.Direccion);
        Assert.Equal("America/Lima", colegio.ZonaHoraria);
    }

    [Fact]
    public void Actualizar_NombreVacio_LanzaDominioExceptionYNoCambiaNada()
    {
        var colegio = Colegio.Crear("Colegio A", "Calle 1", null);

        Assert.Throws<DominioException>(() => colegio.Actualizar("", "Calle 2", null));

        Assert.Equal("Colegio A", colegio.Nombre);
        Assert.Equal("Calle 1", colegio.Direccion);
    }

    [Fact]
    public void DesactivarYActivar_CambianElEstado()
    {
        var colegio = Colegio.Crear("Colegio A", null, null);

        colegio.Desactivar();
        Assert.False(colegio.Activo);

        colegio.Activar();
        Assert.True(colegio.Activo);
    }
}
