using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.UseCases.Supervision;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Tests.Infrastructure.Fakes;

namespace JuntalyColegios.Tests.Application;

/// <summary>
/// Clases y alumnos gestionados por el supervisor. Toda operación queda limitada al colegio del
/// supervisor que la ejecuta: lo de otro colegio "no existe" para él.
/// </summary>
public class ClasesYAlumnosUseCasesTests
{
    private const string UsuarioA = "supervisor-a";
    private const string UsuarioB = "supervisor-b";

    private readonly RepositorioColegiosEnMemoria _colegios = new();
    private readonly RepositorioSupervisoresEnMemoria _supervisores = new();
    private readonly RepositorioClasesEnMemoria _clases = new();
    private readonly RepositorioAlumnosEnMemoria _alumnos = new();
    private readonly Colegio _colegioA;
    private readonly Colegio _colegioB;

    public ClasesYAlumnosUseCasesTests()
    {
        _colegioA = ColegioConSupervisor("Colegio A", UsuarioA);
        _colegioB = ColegioConSupervisor("Colegio B", UsuarioB);
    }

    private Colegio ColegioConSupervisor(string nombre, string usuarioId)
    {
        var colegio = Colegio.Crear(nombre, null, null);
        _colegios.Colegios.Add(colegio);
        _supervisores.Supervisores.Add(Supervisor.Crear(colegio.Id, usuarioId, "Supervisor", Email.Crear($"{usuarioId}@colegio.com")));
        return colegio;
    }

    private Clase ClaseEn(Colegio colegio, string nombre = "3.º A")
    {
        var clase = Clase.Crear(colegio.Id, nombre, null);
        _clases.Clases.Add(clase);
        return clase;
    }

    private Alumno AlumnoEn(Clase clase, string nombres = "Sofía")
    {
        var alumno = Alumno.Crear(clase, nombres, "Pérez");
        _alumnos.Alumnos.Add(alumno);
        return alumno;
    }

    private ContextoSupervisor Contexto() => new(_supervisores, _colegios);

    [Fact]
    public async Task CualquierOperacion_UsuarioQueNoEsSupervisor_LanzaAccesoDenegado()
    {
        var sut = new ListarClasesUseCase(Contexto(), _clases, _alumnos);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => sut.EjecutarAsync("usuario-sin-colegio", CancellationToken.None));
    }

    [Fact]
    public async Task CualquierOperacion_ColegioDesactivado_LanzaAccesoDenegado()
    {
        _colegioA.Desactivar();
        var sut = new ListarClasesUseCase(Contexto(), _clases, _alumnos);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => sut.EjecutarAsync(UsuarioA, CancellationToken.None));
    }

    [Fact]
    public async Task ListarClases_DevuelveSoloLasDeSuColegioConElNumeroDeAlumnosActivos()
    {
        var tercero = ClaseEn(_colegioA, "3.º A");
        ClaseEn(_colegioA, "1.º A");
        ClaseEn(_colegioB, "Clase ajena");
        AlumnoEn(tercero);
        AlumnoEn(tercero, "Luis").DarDeBaja();
        var sut = new ListarClasesUseCase(Contexto(), _clases, _alumnos);

        var resultado = await sut.EjecutarAsync(UsuarioA, CancellationToken.None);

        Assert.Equal("Colegio A", resultado.NombreColegio);
        Assert.Equal(["1.º A", "3.º A"], resultado.Clases.Select(c => c.Nombre));
        Assert.Equal(1, resultado.Clases.Single(c => c.Nombre == "3.º A").NumeroAlumnos);
    }

    [Fact]
    public async Task CrearClase_EnSuColegio_LaGuarda()
    {
        var sut = new CrearClaseUseCase(Contexto(), _clases);

        var id = await sut.EjecutarAsync(UsuarioA, new DatosClaseDto("5.º B", "Primaria"), CancellationToken.None);

        var clase = Assert.Single(_clases.Clases);
        Assert.Equal(id, clase.Id);
        Assert.Equal(_colegioA.Id, clase.ColegioId);
    }

    [Fact]
    public async Task CrearClase_NombreRepetidoEnSuColegio_LanzaDominioException()
    {
        ClaseEn(_colegioA, "5.º B");
        var sut = new CrearClaseUseCase(Contexto(), _clases);

        await Assert.ThrowsAsync<DominioException>(() =>
            sut.EjecutarAsync(UsuarioA, new DatosClaseDto(" 5.º b ", null), CancellationToken.None));
    }

    [Fact]
    public async Task CrearClase_MismoNombreQueEnOtroColegio_Permitido()
    {
        ClaseEn(_colegioB, "5.º B");
        var sut = new CrearClaseUseCase(Contexto(), _clases);

        await sut.EjecutarAsync(UsuarioA, new DatosClaseDto("5.º B", null), CancellationToken.None);

        Assert.Equal(2, _clases.Clases.Count);
    }

    [Fact]
    public async Task ObtenerClase_DeOtroColegio_LanzaNoEncontrado()
    {
        var ajena = ClaseEn(_colegioB);
        var sut = new ObtenerClaseUseCase(Contexto(), _clases, _alumnos);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(UsuarioA, ajena.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ObtenerClase_Propia_DevuelveSusAlumnosOrdenadosPorApellidoYNombre()
    {
        var clase = ClaseEn(_colegioA);
        _alumnos.Alumnos.Add(Alumno.Crear(clase, "Zoe", "Arias"));
        _alumnos.Alumnos.Add(Alumno.Crear(clase, "Ana", "Bello"));
        _alumnos.Alumnos.Add(Alumno.Crear(clase, "Ana", "Arias"));
        var sut = new ObtenerClaseUseCase(Contexto(), _clases, _alumnos);

        var detalle = await sut.EjecutarAsync(UsuarioA, clase.Id, CancellationToken.None);

        Assert.Equal(["Ana Arias", "Zoe Arias", "Ana Bello"], detalle.Alumnos.Select(a => $"{a.Nombres} {a.Apellidos}"));
    }

    [Fact]
    public async Task EditarClase_DeOtroColegio_LanzaNoEncontradoYNoCambia()
    {
        var ajena = ClaseEn(_colegioB, "Original");
        var sut = new EditarClaseUseCase(Contexto(), _clases);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            sut.EjecutarAsync(UsuarioA, ajena.Id, new DatosClaseDto("Hackeada", null), CancellationToken.None));
        Assert.Equal("Original", ajena.Nombre);
    }

    [Fact]
    public async Task EditarClase_NombreDeOtraClaseDelColegio_LanzaDominioException()
    {
        ClaseEn(_colegioA, "3.º A");
        var clase = ClaseEn(_colegioA, "3.º B");
        var sut = new EditarClaseUseCase(Contexto(), _clases);

        await Assert.ThrowsAsync<DominioException>(() =>
            sut.EjecutarAsync(UsuarioA, clase.Id, new DatosClaseDto("3.º A", null), CancellationToken.None));
    }

    [Fact]
    public async Task EditarClase_MantieneSuPropioNombre_Permitido()
    {
        var clase = ClaseEn(_colegioA, "3.º B");
        var sut = new EditarClaseUseCase(Contexto(), _clases);

        await sut.EjecutarAsync(UsuarioA, clase.Id, new DatosClaseDto("3.º B", "Primaria"), CancellationToken.None);

        Assert.Equal("Primaria", clase.Nivel);
    }

    [Fact]
    public async Task EliminarClase_ConAlumnos_LanzaDominioExceptionYNoLaElimina()
    {
        var clase = ClaseEn(_colegioA);
        AlumnoEn(clase).DarDeBaja();
        var sut = new EliminarClaseUseCase(Contexto(), _clases, _alumnos);

        await Assert.ThrowsAsync<DominioException>(() => sut.EjecutarAsync(UsuarioA, clase.Id, CancellationToken.None));
        Assert.Contains(clase, _clases.Clases);
    }

    [Fact]
    public async Task EliminarClase_Vacia_LaElimina()
    {
        var clase = ClaseEn(_colegioA);
        var sut = new EliminarClaseUseCase(Contexto(), _clases, _alumnos);

        await sut.EjecutarAsync(UsuarioA, clase.Id, CancellationToken.None);

        Assert.Empty(_clases.Clases);
    }

    [Fact]
    public async Task EliminarClase_DeOtroColegio_LanzaNoEncontrado()
    {
        var ajena = ClaseEn(_colegioB);
        var sut = new EliminarClaseUseCase(Contexto(), _clases, _alumnos);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(UsuarioA, ajena.Id, CancellationToken.None));
        Assert.Contains(ajena, _clases.Clases);
    }

    [Fact]
    public async Task CrearAlumno_EnClaseDeSuColegio_LoGuarda()
    {
        var clase = ClaseEn(_colegioA);
        var sut = new CrearAlumnoUseCase(Contexto(), _clases, _alumnos);

        var id = await sut.EjecutarAsync(UsuarioA, new DatosAlumnoDto(clase.Id, "Sofía", "Pérez"), CancellationToken.None);

        var alumno = Assert.Single(_alumnos.Alumnos);
        Assert.Equal(id, alumno.Id);
        Assert.Equal(_colegioA.Id, alumno.ColegioId);
    }

    [Fact]
    public async Task CrearAlumno_EnClaseDeOtroColegio_LanzaNoEncontradoYNoLoGuarda()
    {
        var ajena = ClaseEn(_colegioB);
        var sut = new CrearAlumnoUseCase(Contexto(), _clases, _alumnos);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            sut.EjecutarAsync(UsuarioA, new DatosAlumnoDto(ajena.Id, "Sofía", "Pérez"), CancellationToken.None));
        Assert.Empty(_alumnos.Alumnos);
    }

    [Fact]
    public async Task EditarAlumno_CambiaDatosYClaseDentroDeSuColegio()
    {
        var alumno = AlumnoEn(ClaseEn(_colegioA, "3.º A"));
        var nueva = ClaseEn(_colegioA, "4.º A");
        var sut = new EditarAlumnoUseCase(Contexto(), _clases, _alumnos);

        await sut.EjecutarAsync(UsuarioA, alumno.Id, new DatosAlumnoDto(nueva.Id, "Sofía María", "Pérez"), CancellationToken.None);

        Assert.Equal(nueva.Id, alumno.ClaseId);
        Assert.Equal("Sofía María", alumno.Nombres);
    }

    [Fact]
    public async Task EditarAlumno_MoverAClaseDeOtroColegio_LanzaNoEncontradoYNoCambia()
    {
        var original = ClaseEn(_colegioA);
        var alumno = AlumnoEn(original);
        var ajena = ClaseEn(_colegioB);
        var sut = new EditarAlumnoUseCase(Contexto(), _clases, _alumnos);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            sut.EjecutarAsync(UsuarioA, alumno.Id, new DatosAlumnoDto(ajena.Id, "Sofía", "Pérez"), CancellationToken.None));
        Assert.Equal(original.Id, alumno.ClaseId);
    }

    [Fact]
    public async Task EditarAlumno_DeOtroColegio_LanzaNoEncontrado()
    {
        var ajeno = AlumnoEn(ClaseEn(_colegioB));
        var claseA = ClaseEn(_colegioA);
        var sut = new EditarAlumnoUseCase(Contexto(), _clases, _alumnos);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            sut.EjecutarAsync(UsuarioA, ajeno.Id, new DatosAlumnoDto(claseA.Id, "X", "Y"), CancellationToken.None));
    }

    [Fact]
    public async Task ObtenerAlumno_DeOtroColegio_LanzaNoEncontrado()
    {
        var ajeno = AlumnoEn(ClaseEn(_colegioB));
        var sut = new ObtenerAlumnoUseCase(Contexto(), _alumnos);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(UsuarioA, ajeno.Id, CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CambiarEstadoAlumno_Propio_CambiaActivo(bool activo)
    {
        var alumno = AlumnoEn(ClaseEn(_colegioA));
        if (activo) alumno.DarDeBaja();
        var sut = new CambiarEstadoAlumnoUseCase(Contexto(), _alumnos);

        await sut.EjecutarAsync(UsuarioA, alumno.Id, activo, CancellationToken.None);

        Assert.Equal(activo, alumno.Activo);
    }

    [Fact]
    public async Task CambiarEstadoAlumno_DeOtroColegio_LanzaNoEncontrado()
    {
        var ajeno = AlumnoEn(ClaseEn(_colegioB));
        var sut = new CambiarEstadoAlumnoUseCase(Contexto(), _alumnos);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(UsuarioA, ajeno.Id, false, CancellationToken.None));
        Assert.True(ajeno.Activo);
    }
}
