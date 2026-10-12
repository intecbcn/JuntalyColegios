using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.UseCases.Padres;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Tests.Infrastructure.Fakes;

namespace JuntalyColegios.Tests.Application;

/// <summary>Panel del padre: solo ve y gestiona lo de su propia familia.</summary>
public class PanelPadreUseCasesTests
{
    private const string UsuarioPadre = "usuario-padre";
    private const string UsuarioOtroPadre = "usuario-otro-padre";

    private readonly RepositorioColegiosEnMemoria _colegios = new();
    private readonly RepositorioClasesEnMemoria _clases = new();
    private readonly RepositorioAlumnosEnMemoria _alumnos = new();
    private readonly RepositorioRepresentantesEnMemoria _representantes = new();
    private readonly RepositorioVehiculosEnMemoria _vehiculos = new();
    private readonly RepositorioAutorizadosEnMemoria _autorizados = new();
    private readonly Colegio _colegio;
    private readonly Clase _clase;
    private readonly Representante _padre;
    private readonly Representante _otroPadre;

    public PanelPadreUseCasesTests()
    {
        _colegio = Colegio.Crear("Colegio Bolívar", null, null);
        _colegios.Colegios.Add(_colegio);
        _clase = Clase.Crear(_colegio.Id, "3.º A", null);
        _clases.Clases.Add(_clase);
        _padre = Padre(UsuarioPadre);
        _otroPadre = Padre(UsuarioOtroPadre);
    }

    private Representante Padre(string usuarioId)
    {
        var padre = Representante.Crear(_colegio.Id, usuarioId, "Padre " + usuarioId, Email.Crear($"{usuarioId}@familia.com"), null);
        _representantes.Representantes.Add(padre);
        return padre;
    }

    private Alumno HijoDe(Representante padre, string nombres)
    {
        var alumno = Alumno.Crear(_clase, nombres, "Pérez");
        _alumnos.Alumnos.Add(alumno);
        padre.VincularAlumno(alumno);
        return alumno;
    }

    private Vehiculo VehiculoDe(Representante padre, string placa, string? tag = null)
    {
        var vehiculo = Vehiculo.Crear(padre, Placa.Crear(placa), null);
        if (tag is not null) vehiculo.AsignarTag(TagRfid.Crear(tag));
        _vehiculos.Vehiculos.Add(vehiculo);
        return vehiculo;
    }

    private PersonaAutorizada AutorizadoDe(Representante padre, string documento)
    {
        var autorizada = PersonaAutorizada.Crear(padre, "Autorizada", documento, null);
        _autorizados.Autorizados.Add(autorizada);
        return autorizada;
    }

    private ContextoPadre Contexto() => new(_representantes, _colegios);

    [Fact]
    public async Task CualquierOperacion_UsuarioQueNoEsRepresentante_LanzaAccesoDenegado()
    {
        var sut = new ObtenerPanelPadreUseCase(Contexto(), _alumnos, _clases, _vehiculos, _autorizados);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => sut.EjecutarAsync("usuario-cualquiera", CancellationToken.None));
    }

    [Fact]
    public async Task CualquierOperacion_ColegioDesactivado_LanzaAccesoDenegado()
    {
        _colegio.Desactivar();
        var sut = new ObtenerPanelPadreUseCase(Contexto(), _alumnos, _clases, _vehiculos, _autorizados);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => sut.EjecutarAsync(UsuarioPadre, CancellationToken.None));
    }

    [Fact]
    public async Task Panel_MuestraSoloSusHijosActivosConClaseSusVehiculosYSusAutorizados()
    {
        HijoDe(_padre, "Sofía");
        HijoDe(_padre, "Retirado").DarDeBaja();
        HijoDe(_otroPadre, "Ajeno");
        VehiculoDe(_padre, "AB123CD", "E200AABB");
        VehiculoDe(_otroPadre, "ZZ999ZZ");
        AutorizadoDe(_padre, "V-111");
        AutorizadoDe(_otroPadre, "V-999");
        var sut = new ObtenerPanelPadreUseCase(Contexto(), _alumnos, _clases, _vehiculos, _autorizados);

        var panel = await sut.EjecutarAsync(UsuarioPadre, CancellationToken.None);

        Assert.Equal("Colegio Bolívar", panel.NombreColegio);
        var hijo = Assert.Single(panel.Hijos);
        Assert.Equal("Sofía Pérez", hijo.NombreCompleto);
        Assert.Equal("3.º A", hijo.Clase);
        var vehiculo = Assert.Single(panel.Vehiculos);
        Assert.Equal("AB123CD", vehiculo.Placa);
        Assert.Equal("E200AABB", vehiculo.Tag);
        Assert.Equal("V-111", Assert.Single(panel.Autorizados).Documento);
    }

    [Fact]
    public async Task RegistrarMiVehiculo_LoGuardaASuNombreSinTag()
    {
        var sut = new RegistrarMiVehiculoUseCase(Contexto(), _vehiculos);

        var id = await sut.EjecutarAsync(UsuarioPadre, new DatosVehiculoDto("ab123cd", "Corolla gris"), CancellationToken.None);

        var vehiculo = Assert.Single(_vehiculos.Vehiculos);
        Assert.Equal(id, vehiculo.Id);
        Assert.Equal(_padre.Id, vehiculo.RepresentanteId);
        Assert.Null(vehiculo.Tag);
    }

    [Fact]
    public async Task RegistrarMiVehiculo_PlacaQueYaTiene_LanzaDominioException()
    {
        VehiculoDe(_padre, "AB123CD");
        var sut = new RegistrarMiVehiculoUseCase(Contexto(), _vehiculos);

        await Assert.ThrowsAsync<DominioException>(() =>
            sut.EjecutarAsync(UsuarioPadre, new DatosVehiculoDto("AB 123 CD", null), CancellationToken.None));
    }

    [Fact]
    public async Task EliminarMiVehiculo_Propio_LoElimina()
    {
        var vehiculo = VehiculoDe(_padre, "AB123CD");
        var sut = new EliminarMiVehiculoUseCase(Contexto(), _vehiculos);

        await sut.EjecutarAsync(UsuarioPadre, vehiculo.Id, CancellationToken.None);

        Assert.Empty(_vehiculos.Vehiculos);
    }

    [Fact]
    public async Task EliminarMiVehiculo_DeOtraFamilia_LanzaNoEncontradoYNoLoElimina()
    {
        var ajeno = VehiculoDe(_otroPadre, "ZZ999ZZ");
        var sut = new EliminarMiVehiculoUseCase(Contexto(), _vehiculos);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(UsuarioPadre, ajeno.Id, CancellationToken.None));
        Assert.Contains(ajeno, _vehiculos.Vehiculos);
    }

    [Fact]
    public async Task AgregarAutorizado_LoGuardaASuNombre()
    {
        var sut = new AgregarAutorizadoUseCase(Contexto(), _autorizados);

        var id = await sut.EjecutarAsync(UsuarioPadre, new DatosAutorizadoDto("María Gómez", "V-12345678", "Abuela"), CancellationToken.None);

        var autorizada = Assert.Single(_autorizados.Autorizados);
        Assert.Equal(id, autorizada.Id);
        Assert.Equal(_padre.Id, autorizada.RepresentanteId);
    }

    [Fact]
    public async Task AgregarAutorizado_DocumentoYaAutorizado_LanzaDominioException()
    {
        AutorizadoDe(_padre, "V-12345678");
        var sut = new AgregarAutorizadoUseCase(Contexto(), _autorizados);

        await Assert.ThrowsAsync<DominioException>(() =>
            sut.EjecutarAsync(UsuarioPadre, new DatosAutorizadoDto("Otra", "v-12345678", null), CancellationToken.None));
    }

    [Fact]
    public async Task AgregarAutorizado_MismoDocumentoQueEnOtraFamilia_Permitido()
    {
        AutorizadoDe(_otroPadre, "V-12345678");
        var sut = new AgregarAutorizadoUseCase(Contexto(), _autorizados);

        await sut.EjecutarAsync(UsuarioPadre, new DatosAutorizadoDto("María", "V-12345678", null), CancellationToken.None);

        Assert.Equal(2, _autorizados.Autorizados.Count);
    }

    [Fact]
    public async Task EliminarAutorizado_Propio_LoElimina()
    {
        var autorizada = AutorizadoDe(_padre, "V-111");
        var sut = new EliminarAutorizadoUseCase(Contexto(), _autorizados);

        await sut.EjecutarAsync(UsuarioPadre, autorizada.Id, CancellationToken.None);

        Assert.Empty(_autorizados.Autorizados);
    }

    [Fact]
    public async Task EliminarAutorizado_DeOtraFamilia_LanzaNoEncontradoYNoLoElimina()
    {
        var ajena = AutorizadoDe(_otroPadre, "V-999");
        var sut = new EliminarAutorizadoUseCase(Contexto(), _autorizados);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(UsuarioPadre, ajena.Id, CancellationToken.None));
        Assert.Contains(ajena, _autorizados.Autorizados);
    }
}
