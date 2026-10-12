using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;

namespace JuntalyColegios.Tests.Domain;

public class PersonaAutorizadaTests
{
    private static readonly Representante Padre =
        Representante.Crear(Guid.NewGuid(), "usuario-1", "Carlos Pérez", Email.Crear("carlos@familia.com"), null);

    [Fact]
    public void Crear_DatosValidos_PerteneceAlRepresentanteYASuColegio()
    {
        var autorizada = PersonaAutorizada.Crear(Padre, " María Gómez ", " v-12.345.678 ", " Abuela ");

        Assert.NotEqual(Guid.Empty, autorizada.Id);
        Assert.Equal(Padre.Id, autorizada.RepresentanteId);
        Assert.Equal(Padre.ColegioId, autorizada.ColegioId);
        Assert.Equal("María Gómez", autorizada.Nombre);
        Assert.Equal("V-12.345.678", autorizada.Documento);
        Assert.Equal("Abuela", autorizada.Parentesco);
    }

    [Fact]
    public void Crear_SinParentesco_Permitido()
    {
        Assert.Null(PersonaAutorizada.Crear(Padre, "María Gómez", "12345678", "  ").Parentesco);
    }

    [Theory]
    [InlineData("", "12345678")]
    [InlineData("María", "")]
    [InlineData("María", "12")]
    [InlineData("María", "12 345 678")]
    [InlineData("María", "<script>")]
    public void Crear_NombreODocumentoInvalidos_LanzaDominioException(string nombre, string documento)
    {
        Assert.Throws<DominioException>(() => PersonaAutorizada.Crear(Padre, nombre, documento, null));
    }

    [Fact]
    public void TieneDocumento_IgnoraMayusculasYEspacios()
    {
        var autorizada = PersonaAutorizada.Crear(Padre, "María", "V-12345678", null);

        Assert.True(autorizada.TieneDocumento(" v-12345678 "));
        Assert.False(autorizada.TieneDocumento("V-87654321"));
    }
}
