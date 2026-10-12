using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;

namespace JuntalyColegios.Tests.Domain;

public class ClaseYAlumnoTests
{
    private static readonly Guid ColegioA = Guid.NewGuid();
    private static readonly Guid ColegioB = Guid.NewGuid();

    [Fact]
    public void Clase_Crear_DatosValidos()
    {
        var clase = Clase.Crear(ColegioA, " 3.º A ", " Primaria ");

        Assert.NotEqual(Guid.Empty, clase.Id);
        Assert.Equal(ColegioA, clase.ColegioId);
        Assert.Equal("3.º A", clase.Nombre);
        Assert.Equal("Primaria", clase.Nivel);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Clase_Crear_NombreVacio_LanzaDominioException(string nombre)
    {
        Assert.Throws<DominioException>(() => Clase.Crear(ColegioA, nombre, null));
    }

    [Fact]
    public void Clase_Crear_NombreDemasiadoLargo_LanzaDominioException()
    {
        Assert.Throws<DominioException>(() => Clase.Crear(ColegioA, new string('x', Clase.LongitudMaximaNombre + 1), null));
    }

    [Fact]
    public void Clase_Crear_SinColegio_LanzaDominioException()
    {
        Assert.Throws<DominioException>(() => Clase.Crear(Guid.Empty, "3.º A", null));
    }

    [Fact]
    public void Clase_Actualizar_CambiaNombreYNivel()
    {
        var clase = Clase.Crear(ColegioA, "3.º A", null);

        clase.Actualizar("3.º B", "Primaria");

        Assert.Equal("3.º B", clase.Nombre);
        Assert.Equal("Primaria", clase.Nivel);
    }

    [Fact]
    public void Alumno_Crear_TomaElColegioDeSuClaseYEstaActivo()
    {
        var clase = Clase.Crear(ColegioA, "3.º A", null);

        var alumno = Alumno.Crear(clase, " Sofía ", " Pérez Gil ");

        Assert.NotEqual(Guid.Empty, alumno.Id);
        Assert.Equal(ColegioA, alumno.ColegioId);
        Assert.Equal(clase.Id, alumno.ClaseId);
        Assert.Equal("Sofía", alumno.Nombres);
        Assert.Equal("Pérez Gil", alumno.Apellidos);
        Assert.Equal("Sofía Pérez Gil", alumno.NombreCompleto);
        Assert.True(alumno.Activo);
    }

    [Theory]
    [InlineData("", "Pérez")]
    [InlineData("Sofía", " ")]
    public void Alumno_Crear_NombresOApellidosVacios_LanzaDominioException(string nombres, string apellidos)
    {
        Assert.Throws<DominioException>(() => Alumno.Crear(Clase.Crear(ColegioA, "3.º A", null), nombres, apellidos));
    }

    [Fact]
    public void Alumno_CambiarDeClase_MismoColegio_CambiaLaClase()
    {
        var alumno = Alumno.Crear(Clase.Crear(ColegioA, "3.º A", null), "Sofía", "Pérez");
        var nueva = Clase.Crear(ColegioA, "4.º A", null);

        alumno.CambiarDeClase(nueva);

        Assert.Equal(nueva.Id, alumno.ClaseId);
    }

    [Fact]
    public void Alumno_CambiarDeClase_ClaseDeOtroColegio_LanzaDominioExceptionYNoCambia()
    {
        var original = Clase.Crear(ColegioA, "3.º A", null);
        var alumno = Alumno.Crear(original, "Sofía", "Pérez");

        Assert.Throws<DominioException>(() => alumno.CambiarDeClase(Clase.Crear(ColegioB, "3.º A", null)));

        Assert.Equal(original.Id, alumno.ClaseId);
        Assert.Equal(ColegioA, alumno.ColegioId);
    }

    [Fact]
    public void Alumno_DarDeBajaYReactivar()
    {
        var alumno = Alumno.Crear(Clase.Crear(ColegioA, "3.º A", null), "Sofía", "Pérez");

        alumno.DarDeBaja();
        Assert.False(alumno.Activo);

        alumno.Reactivar();
        Assert.True(alumno.Activo);
    }
}
