using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;

namespace JuntalyColegios.Tests.Domain;

public class RepresentanteYVehiculoTests
{
    private static readonly Guid ColegioA = Guid.NewGuid();
    private static readonly Guid ColegioB = Guid.NewGuid();

    private static Representante Padre(Guid? colegioId = null) =>
        Representante.Crear(colegioId ?? ColegioA, "usuario-1", "Carlos Pérez", Email.Crear("carlos@familia.com"), "+58 412 1234567");

    private static Alumno AlumnoDe(Guid colegioId) => Alumno.Crear(Clase.Crear(colegioId, "3.º A", null), "Sofía", "Pérez");

    [Theory]
    [InlineData("e2004700a1b2c3d4e5f60708", "E2004700A1B2C3D4E5F60708")]
    [InlineData("  ABCD1234 ", "ABCD1234")]
    public void TagRfid_Valido_SeNormalizaEnMayusculas(string valor, string esperado)
    {
        Assert.Equal(esperado, TagRfid.Crear(valor).Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ABCD 1234")]
    [InlineData("ABCD-1234")]
    [InlineData("0123456789012345678901234567890123")]
    public void TagRfid_Invalido_LanzaDominioException(string valor)
    {
        Assert.Throws<DominioException>(() => TagRfid.Crear(valor));
    }

    [Theory]
    [InlineData("ab123cd", "AB123CD")]
    [InlineData(" AB-123-CD ", "AB-123-CD")]
    [InlineData("a b 1 2 3", "AB123")]
    public void Placa_Valida_SeNormalizaEnMayusculasSinEspacios(string valor, string esperado)
    {
        Assert.Equal(esperado, Placa.Crear(valor).Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("AB.123")]
    [InlineData("ABCDEFGHIJKL")]
    public void Placa_Invalida_LanzaDominioException(string valor)
    {
        Assert.Throws<DominioException>(() => Placa.Crear(valor));
    }

    [Fact]
    public void Representante_Crear_DatosValidos()
    {
        var padre = Representante.Crear(ColegioA, "usuario-1", " Carlos Pérez ", Email.Crear("carlos@familia.com"), "  ");

        Assert.NotEqual(Guid.Empty, padre.Id);
        Assert.Equal(ColegioA, padre.ColegioId);
        Assert.Equal("Carlos Pérez", padre.Nombre);
        Assert.Equal("carlos@familia.com", padre.Email);
        Assert.Null(padre.Telefono);
        Assert.Empty(padre.Alumnos);
    }

    [Theory]
    [InlineData("", "usuario-1")]
    [InlineData("Carlos", "")]
    public void Representante_Crear_SinNombreOSinUsuario_LanzaDominioException(string nombre, string usuarioId)
    {
        Assert.Throws<DominioException>(() =>
            Representante.Crear(ColegioA, usuarioId, nombre, Email.Crear("carlos@familia.com"), null));
    }

    [Fact]
    public void Representante_VincularAlumnoDeSuColegio_LoAnadeUnaSolaVez()
    {
        var padre = Padre();
        var hijo = AlumnoDe(ColegioA);

        padre.VincularAlumno(hijo);
        padre.VincularAlumno(hijo);

        Assert.Equal([hijo.Id], padre.Alumnos.Select(v => v.AlumnoId));
    }

    [Fact]
    public void Representante_VincularAlumnoDeOtroColegio_LanzaDominioException()
    {
        var padre = Padre();

        Assert.Throws<DominioException>(() => padre.VincularAlumno(AlumnoDe(ColegioB)));
        Assert.Empty(padre.Alumnos);
    }

    [Fact]
    public void Representante_DesvincularAlumno_LoQuita()
    {
        var padre = Padre();
        var hijo = AlumnoDe(ColegioA);
        padre.VincularAlumno(hijo);

        padre.DesvincularAlumno(hijo.Id);

        Assert.Empty(padre.Alumnos);
    }

    [Fact]
    public void Vehiculo_Crear_PerteneceAlRepresentanteYASuColegioSinTag()
    {
        var padre = Padre();

        var vehiculo = Vehiculo.Crear(padre, Placa.Crear("ab123cd"), " Toyota Corolla gris ");

        Assert.NotEqual(Guid.Empty, vehiculo.Id);
        Assert.Equal(padre.Id, vehiculo.RepresentanteId);
        Assert.Equal(ColegioA, vehiculo.ColegioId);
        Assert.Equal("AB123CD", vehiculo.Placa);
        Assert.Equal("Toyota Corolla gris", vehiculo.Descripcion);
        Assert.Null(vehiculo.Tag);
    }

    [Fact]
    public void Vehiculo_AsignarYQuitarTag()
    {
        var vehiculo = Vehiculo.Crear(Padre(), Placa.Crear("AB123CD"), null);

        vehiculo.AsignarTag(TagRfid.Crear("e2004700a1b2"));
        Assert.Equal("E2004700A1B2", vehiculo.Tag);

        vehiculo.QuitarTag();
        Assert.Null(vehiculo.Tag);
    }
}
