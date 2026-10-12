using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Application.UseCases.Supervision;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Tests.Infrastructure.Fakes;
using NSubstitute;

namespace JuntalyColegios.Tests.Application;

/// <summary>Alta de padres, vínculo con sus hijos y tags RFID de sus vehículos, desde el supervisor.</summary>
public class PadresYVehiculosUseCasesTests
{
    private const string UsuarioA = "supervisor-a";

    private readonly RepositorioColegiosEnMemoria _colegios = new();
    private readonly RepositorioSupervisoresEnMemoria _supervisores = new();
    private readonly RepositorioClasesEnMemoria _clases = new();
    private readonly RepositorioAlumnosEnMemoria _alumnos = new();
    private readonly RepositorioRepresentantesEnMemoria _representantes = new();
    private readonly RepositorioVehiculosEnMemoria _vehiculos = new();
    private readonly IServicioUsuarios _usuarios = Substitute.For<IServicioUsuarios>();
    private readonly Colegio _colegioA;
    private readonly Colegio _colegioB;

    public PadresYVehiculosUseCasesTests()
    {
        _colegioA = ColegioConSupervisor("Colegio A", UsuarioA);
        _colegioB = ColegioConSupervisor("Colegio B", "supervisor-b");
        _usuarios.CrearUsuarioInvitadoAsync(default!, default!, default!, default).ReturnsForAnyArgs("usuario-padre");
    }

    private Colegio ColegioConSupervisor(string nombre, string usuarioId)
    {
        var colegio = Colegio.Crear(nombre, null, null);
        _colegios.Colegios.Add(colegio);
        _supervisores.Supervisores.Add(Supervisor.Crear(colegio.Id, usuarioId, "Supervisor", Email.Crear($"{usuarioId}@colegio.com")));
        return colegio;
    }

    private ContextoSupervisor Contexto() => new(_supervisores, _colegios);

    private Alumno AlumnoEn(Colegio colegio, string nombres = "Sofía", string clase = "3.º A")
    {
        var c = _clases.Clases.FirstOrDefault(x => x.ColegioId == colegio.Id && x.Nombre == clase)
            ?? Clase.Crear(colegio.Id, clase, null);
        if (!_clases.Clases.Contains(c)) _clases.Clases.Add(c);
        var alumno = Alumno.Crear(c, nombres, "Pérez");
        _alumnos.Alumnos.Add(alumno);
        return alumno;
    }

    private Representante PadreEn(Colegio colegio, string usuarioId = "padre-1")
    {
        var padre = Representante.Crear(colegio.Id, usuarioId, "Carlos Pérez", Email.Crear($"{usuarioId}@familia.com"), null);
        _representantes.Representantes.Add(padre);
        return padre;
    }

    private Vehiculo VehiculoDe(Representante padre, string placa = "AB123CD", string? tag = null)
    {
        var vehiculo = Vehiculo.Crear(padre, Placa.Crear(placa), null);
        if (tag is not null) vehiculo.AsignarTag(TagRfid.Crear(tag));
        _vehiculos.Vehiculos.Add(vehiculo);
        return vehiculo;
    }

    private AltaRepresentanteUseCase Alta() => new(Contexto(), _alumnos, _representantes, _usuarios);

    [Fact]
    public async Task AltaRepresentante_CreaCuentaConRolPadreYLoVinculaASusHijos()
    {
        var hijo1 = AlumnoEn(_colegioA, "Sofía");
        var hijo2 = AlumnoEn(_colegioA, "Mateo", "1.º B");

        var id = await Alta().EjecutarAsync(UsuarioA,
            new AltaRepresentanteDto("Carlos Pérez", "Carlos@Familia.com", "+58 412 1234567", [hijo1.Id, hijo2.Id]), CancellationToken.None);

        await _usuarios.Received(1).CrearUsuarioInvitadoAsync("carlos@familia.com", "Carlos Pérez", Roles.Padre, Arg.Any<CancellationToken>());
        var padre = Assert.Single(_representantes.Representantes);
        Assert.Equal(id, padre.Id);
        Assert.Equal(_colegioA.Id, padre.ColegioId);
        Assert.Equal("usuario-padre", padre.UsuarioId);
        Assert.Equal(new[] { hijo1.Id, hijo2.Id }.Order(), padre.Alumnos.Select(v => v.AlumnoId).Order());
    }

    [Fact]
    public async Task AltaRepresentante_HijoDeOtroColegio_LanzaNoEncontradoSinCrearCuenta()
    {
        var ajeno = AlumnoEn(_colegioB);

        await Assert.ThrowsAsync<NoEncontradoException>(() => Alta().EjecutarAsync(UsuarioA,
            new AltaRepresentanteDto("Carlos", "carlos@familia.com", null, [ajeno.Id]), CancellationToken.None));

        await _usuarios.DidNotReceiveWithAnyArgs().CrearUsuarioInvitadoAsync(default!, default!, default!, default);
        Assert.Empty(_representantes.Representantes);
    }

    [Theory]
    [InlineData("", "carlos@familia.com")]
    [InlineData("Carlos", "no-es-email")]
    public async Task AltaRepresentante_DatosInvalidos_LanzaDominioExceptionSinCrearCuenta(string nombre, string email)
    {
        await Assert.ThrowsAsync<DominioException>(() => Alta().EjecutarAsync(UsuarioA,
            new AltaRepresentanteDto(nombre, email, null, []), CancellationToken.None));

        await _usuarios.DidNotReceiveWithAnyArgs().CrearUsuarioInvitadoAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task AltaRepresentante_FallaAlGuardar_EliminaLaCuentaCreada()
    {
        _representantes.FallarAlAgregar = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Alta().EjecutarAsync(UsuarioA,
            new AltaRepresentanteDto("Carlos", "carlos@familia.com", null, []), CancellationToken.None));

        await _usuarios.Received(1).EliminarUsuarioAsync("usuario-padre", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListarRepresentantes_SoloLosDeSuColegioConHijosYVehiculos()
    {
        var padre = PadreEn(_colegioA, "padre-a");
        padre.VincularAlumno(AlumnoEn(_colegioA));
        VehiculoDe(padre);
        PadreEn(_colegioB, "padre-b");
        var sut = new ListarRepresentantesUseCase(Contexto(), _representantes, _vehiculos);

        var lista = await sut.EjecutarAsync(UsuarioA, CancellationToken.None);

        var resumen = Assert.Single(lista);
        Assert.Equal(padre.Id, resumen.Id);
        Assert.Equal(1, resumen.NumeroHijos);
        Assert.Equal(1, resumen.NumeroVehiculos);
    }

    [Fact]
    public async Task ObtenerRepresentante_DevuelveHijosConSuClaseVehiculosYAlumnosQueQuedanPorVincular()
    {
        var padre = PadreEn(_colegioA);
        var hijo = AlumnoEn(_colegioA, "Sofía", "3.º A");
        var otro = AlumnoEn(_colegioA, "Luis", "5.º C");
        AlumnoEn(_colegioA, "De baja").DarDeBaja();
        AlumnoEn(_colegioB, "Ajeno");
        padre.VincularAlumno(hijo);
        VehiculoDe(padre, "XY987ZZ", "E200AABB");
        var sut = new ObtenerRepresentanteUseCase(Contexto(), _representantes, _alumnos, _clases, _vehiculos);

        var detalle = await sut.EjecutarAsync(UsuarioA, padre.Id, CancellationToken.None);

        var hijoDto = Assert.Single(detalle.Hijos);
        Assert.Equal("3.º A", hijoDto.Clase);
        Assert.Equal("XY987ZZ", Assert.Single(detalle.Vehiculos).Placa);
        Assert.Equal("E200AABB", detalle.Vehiculos[0].Tag);
        Assert.Equal([otro.Id], detalle.AlumnosParaVincular.Select(a => a.AlumnoId));
    }

    [Fact]
    public async Task ObtenerRepresentante_DeOtroColegio_LanzaNoEncontrado()
    {
        var ajeno = PadreEn(_colegioB);
        var sut = new ObtenerRepresentanteUseCase(Contexto(), _representantes, _alumnos, _clases, _vehiculos);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(UsuarioA, ajeno.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ListarAlumnosParaVincular_ActivosDeSuColegioOrdenadosConSuClase()
    {
        AlumnoEn(_colegioA, "Zoe", "1.º A");
        AlumnoEn(_colegioA, "Ana", "2.º B");
        AlumnoEn(_colegioA, "Baja").DarDeBaja();
        AlumnoEn(_colegioB, "Ajeno");
        var sut = new ListarAlumnosParaVincularUseCase(Contexto(), _alumnos, _clases);

        var lista = await sut.EjecutarAsync(UsuarioA, CancellationToken.None);

        Assert.Equal(["Ana Pérez", "Zoe Pérez"], lista.Select(a => a.NombreCompleto));
        Assert.Equal("2.º B", lista[0].Clase);
    }

    [Fact]
    public async Task VincularAlumno_DeSuColegio_LoVincula()
    {
        var padre = PadreEn(_colegioA);
        var hijo = AlumnoEn(_colegioA);
        var sut = new VincularAlumnoUseCase(Contexto(), _representantes, _alumnos);

        await sut.EjecutarAsync(UsuarioA, padre.Id, hijo.Id, CancellationToken.None);

        Assert.Equal([hijo.Id], padre.Alumnos.Select(v => v.AlumnoId));
    }

    [Fact]
    public async Task VincularAlumno_DeOtroColegio_LanzaNoEncontrado()
    {
        var padre = PadreEn(_colegioA);
        var ajeno = AlumnoEn(_colegioB);
        var sut = new VincularAlumnoUseCase(Contexto(), _representantes, _alumnos);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(UsuarioA, padre.Id, ajeno.Id, CancellationToken.None));
        Assert.Empty(padre.Alumnos);
    }

    [Fact]
    public async Task DesvincularAlumno_LoQuita()
    {
        var padre = PadreEn(_colegioA);
        var hijo = AlumnoEn(_colegioA);
        padre.VincularAlumno(hijo);
        var sut = new DesvincularAlumnoUseCase(Contexto(), _representantes);

        await sut.EjecutarAsync(UsuarioA, padre.Id, hijo.Id, CancellationToken.None);

        Assert.Empty(padre.Alumnos);
    }

    [Fact]
    public async Task AgregarVehiculo_ARepresentanteDeSuColegio_LoGuardaSinTag()
    {
        var padre = PadreEn(_colegioA);
        var sut = new AgregarVehiculoUseCase(Contexto(), _representantes, _vehiculos);

        var id = await sut.EjecutarAsync(UsuarioA, padre.Id, new DatosVehiculoDto("ab 123 cd", "Corolla gris"), CancellationToken.None);

        var vehiculo = Assert.Single(_vehiculos.Vehiculos);
        Assert.Equal(id, vehiculo.Id);
        Assert.Equal("AB123CD", vehiculo.Placa);
        Assert.Null(vehiculo.Tag);
    }

    [Fact]
    public async Task AgregarVehiculo_PlacaRepetidaDelMismoRepresentante_LanzaDominioException()
    {
        var padre = PadreEn(_colegioA);
        VehiculoDe(padre, "AB123CD");
        var sut = new AgregarVehiculoUseCase(Contexto(), _representantes, _vehiculos);

        await Assert.ThrowsAsync<DominioException>(() =>
            sut.EjecutarAsync(UsuarioA, padre.Id, new DatosVehiculoDto("ab 123 cd", null), CancellationToken.None));
    }

    [Fact]
    public async Task AgregarVehiculo_ARepresentanteDeOtroColegio_LanzaNoEncontrado()
    {
        var ajeno = PadreEn(_colegioB);
        var sut = new AgregarVehiculoUseCase(Contexto(), _representantes, _vehiculos);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            sut.EjecutarAsync(UsuarioA, ajeno.Id, new DatosVehiculoDto("AB123CD", null), CancellationToken.None));
    }

    [Fact]
    public async Task AsignarTag_LibreEnSuColegio_LoAsigna()
    {
        var vehiculo = VehiculoDe(PadreEn(_colegioA));
        var sut = new AsignarTagUseCase(Contexto(), _vehiculos);

        await sut.EjecutarAsync(UsuarioA, vehiculo.Id, "e200aabbcc", CancellationToken.None);

        Assert.Equal("E200AABBCC", vehiculo.Tag);
    }

    [Fact]
    public async Task AsignarTag_YaUsadoPorOtroVehiculoDelColegio_LanzaDominioException()
    {
        VehiculoDe(PadreEn(_colegioA, "padre-1"), "AAA111", "E200AABBCC");
        var vehiculo = VehiculoDe(PadreEn(_colegioA, "padre-2"), "BBB222");
        var sut = new AsignarTagUseCase(Contexto(), _vehiculos);

        var ex = await Assert.ThrowsAsync<DominioException>(() =>
            sut.EjecutarAsync(UsuarioA, vehiculo.Id, "e200aabbcc", CancellationToken.None));
        Assert.Contains("AAA111", ex.Message);
        Assert.Null(vehiculo.Tag);
    }

    [Fact]
    public async Task AsignarTag_MismoTagQueEnOtroColegio_Permitido()
    {
        VehiculoDe(PadreEn(_colegioB, "padre-b"), "AAA111", "E200AABBCC");
        var vehiculo = VehiculoDe(PadreEn(_colegioA));
        var sut = new AsignarTagUseCase(Contexto(), _vehiculos);

        await sut.EjecutarAsync(UsuarioA, vehiculo.Id, "E200AABBCC", CancellationToken.None);

        Assert.Equal("E200AABBCC", vehiculo.Tag);
    }

    [Fact]
    public async Task AsignarTag_ElQueYaTieneElPropioVehiculo_Permitido()
    {
        var vehiculo = VehiculoDe(PadreEn(_colegioA), tag: "E200AABBCC");
        var sut = new AsignarTagUseCase(Contexto(), _vehiculos);

        await sut.EjecutarAsync(UsuarioA, vehiculo.Id, "E200AABBCC", CancellationToken.None);

        Assert.Equal("E200AABBCC", vehiculo.Tag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task AsignarTag_Vacio_QuitaElTag(string? tag)
    {
        var vehiculo = VehiculoDe(PadreEn(_colegioA), tag: "E200AABBCC");
        var sut = new AsignarTagUseCase(Contexto(), _vehiculos);

        await sut.EjecutarAsync(UsuarioA, vehiculo.Id, tag, CancellationToken.None);

        Assert.Null(vehiculo.Tag);
    }

    [Fact]
    public async Task AsignarTag_VehiculoDeOtroColegio_LanzaNoEncontrado()
    {
        var ajeno = VehiculoDe(PadreEn(_colegioB, "padre-b"));
        var sut = new AsignarTagUseCase(Contexto(), _vehiculos);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            sut.EjecutarAsync(UsuarioA, ajeno.Id, "E200AABBCC", CancellationToken.None));
        Assert.Null(ajeno.Tag);
    }
}
