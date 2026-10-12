using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;

namespace JuntalyColegios.Tests.Domain;

public class SupervisorYEmailTests
{
    [Theory]
    [InlineData("ana@colegio.com", "ana@colegio.com")]
    [InlineData("  Ana.Perez@Colegio.COM ", "ana.perez@colegio.com")]
    public void Email_Valido_SeNormalizaEnMinusculasYSinEspacios(string valor, string esperado)
    {
        Assert.Equal(esperado, Email.Crear(valor).Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-un-email")]
    [InlineData("ana@")]
    [InlineData("Ana <ana@colegio.com>")]
    public void Email_Invalido_LanzaDominioException(string valor)
    {
        Assert.Throws<DominioException>(() => Email.Crear(valor));
    }

    [Fact]
    public void Supervisor_Crear_DatosValidos()
    {
        var colegioId = Guid.NewGuid();

        var supervisor = Supervisor.Crear(colegioId, "usuario-1", "  Luis Gómez ", Email.Crear("luis@colegio.com"));

        Assert.NotEqual(Guid.Empty, supervisor.Id);
        Assert.Equal(colegioId, supervisor.ColegioId);
        Assert.Equal("usuario-1", supervisor.UsuarioId);
        Assert.Equal("Luis Gómez", supervisor.Nombre);
        Assert.Equal("luis@colegio.com", supervisor.Email);
    }

    [Fact]
    public void Supervisor_Crear_NombreVacio_LanzaDominioException()
    {
        Assert.Throws<DominioException>(() =>
            Supervisor.Crear(Guid.NewGuid(), "usuario-1", " ", Email.Crear("luis@colegio.com")));
    }

    [Fact]
    public void Supervisor_Crear_SinUsuario_LanzaDominioException()
    {
        Assert.Throws<DominioException>(() =>
            Supervisor.Crear(Guid.NewGuid(), "", "Luis", Email.Crear("luis@colegio.com")));
    }
}
