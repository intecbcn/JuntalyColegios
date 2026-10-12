using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Application.UseCases.Pantallas;
using JuntalyColegios.Application.UseCases.Supervision;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Tests.Infrastructure;
using JuntalyColegios.Tests.Infrastructure.Fakes;

namespace JuntalyColegios.Tests.Application;

/// <summary>
/// Pantallas de llamada (portería y aulas) a las que se accede con un enlace secreto: el supervisor las
/// genera y revoca; la pantalla muestra solo lo imprescindible de las recogidas pendientes.
/// </summary>
public class PantallasUseCasesTests
{
    private const string UsuarioSupervisor = "supervisor-a";
    private static readonly DateTimeOffset Ahora = new(2026, 10, 12, 16, 5, 0, TimeSpan.Zero);

    private readonly RepositorioColegiosEnMemoria _colegios = new();
    private readonly RepositorioSupervisoresEnMemoria _supervisores = new();
    private readonly RepositorioClasesEnMemoria _clases = new();
    private readonly RepositorioAlumnosEnMemoria _alumnos = new();
    private readonly RepositorioRepresentantesEnMemoria _representantes = new();
    private readonly RepositorioVehiculosEnMemoria _vehiculos = new();
    private readonly RepositorioDeteccionesEnMemoria _detecciones = new();
    private readonly RepositorioPantallasEnMemoria _pantallas = new();
    private readonly RelojDePrueba _reloj = new(Ahora);
    private readonly Colegio _colegio;
    private readonly Colegio _otroColegio;
    private readonly Clase _primero;
    private readonly Clase _quinto;
    private readonly Vehiculo _carro;

    public PantallasUseCasesTests()
    {
        _colegio = Colegio.Crear("Colegio Bolívar", null, "America/Caracas");
        _otroColegio = Colegio.Crear("Otro", null, "America/Caracas");
        _colegios.Colegios.AddRange([_colegio, _otroColegio]);
        _supervisores.Supervisores.Add(Supervisor.Crear(_colegio.Id, UsuarioSupervisor, "Supervisor", Email.Crear("sup@colegio.com")));
        _primero = Clase.Crear(_colegio.Id, "1.º A", null);
        _quinto = Clase.Crear(_colegio.Id, "5.º Año", null);
        _clases.Clases.AddRange([_primero, _quinto]);
        var padre = Representante.Crear(_colegio.Id, "padre", "Carlos Pérez", Email.Crear("carlos@familia.com"), null);
        _representantes.Representantes.Add(padre);
        _carro = Vehiculo.Crear(padre, Placa.Crear("AB123CD"), "Corolla");
        _vehiculos.Vehiculos.Add(_carro);
    }

    private ContextoSupervisor Contexto() => new(_supervisores, _colegios);

    private Recogida Llamar(Clase clase, string nombres, DateTimeOffset cuando)
    {
        var alumno = Alumno.Crear(clase, nombres, "Pérez");
        _alumnos.Alumnos.Add(alumno);
        var recogida = Recogida.Crear(Deteccion.Crear(clase.ColegioId, "E200AABB", _carro.Id, cuando, ResultadoDeteccion.GeneraRecogida, false), alumno.Id);
        _detecciones.Recogidas.Add(recogida);
        return recogida;
    }

    private CrearPantallaUseCase Crear() => new(Contexto(), _pantallas, _clases);

    private ObtenerPantallaPublicaUseCase Publica() =>
        new(_pantallas, _colegios, _clases, _alumnos, _vehiculos, _detecciones, _reloj);

    [Fact]
    public async Task CrearPantalla_Porteria_DevuelveUnTokenSecretoYGuardaSoloSuHash()
    {
        var token = await Crear().EjecutarAsync(UsuarioSupervisor, null, CancellationToken.None);

        var pantalla = Assert.Single(_pantallas.Pantallas);
        Assert.True(token.Length >= 40);
        Assert.True(pantalla.EsPorteria);
        Assert.Equal(TokenSeguro.Hash(token), pantalla.HashToken);
        Assert.NotEqual(token, pantalla.HashToken);
    }

    [Fact]
    public async Task CrearPantalla_DeUnAulaDeOtroColegio_LanzaNoEncontrado()
    {
        var ajena = Clase.Crear(_otroColegio.Id, "Ajena", null);
        _clases.Clases.Add(ajena);

        await Assert.ThrowsAsync<NoEncontradoException>(() => Crear().EjecutarAsync(UsuarioSupervisor, ajena.Id, CancellationToken.None));
        Assert.Empty(_pantallas.Pantallas);
    }

    [Fact]
    public async Task CrearPantalla_OtraVezParaElMismoAmbito_RegeneraElEnlaceYElAnteriorDejaDeValer()
    {
        var anterior = await Crear().EjecutarAsync(UsuarioSupervisor, _primero.Id, CancellationToken.None);

        var nuevo = await Crear().EjecutarAsync(UsuarioSupervisor, _primero.Id, CancellationToken.None);

        Assert.Single(_pantallas.Pantallas);
        Assert.NotEqual(anterior, nuevo);
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => Publica().EjecutarAsync(anterior, CancellationToken.None));
        await Publica().EjecutarAsync(nuevo, CancellationToken.None);
    }

    [Fact]
    public async Task ListarPantallas_LasDeSuColegioConSuAmbito()
    {
        await Crear().EjecutarAsync(UsuarioSupervisor, _quinto.Id, CancellationToken.None);
        await Crear().EjecutarAsync(UsuarioSupervisor, null, CancellationToken.None);
        _pantallas.Pantallas.Add(PantallaLlamadas.Crear(_otroColegio.Id, null, "AJENA"));
        var sut = new ListarPantallasUseCase(Contexto(), _pantallas, _clases);

        var resultado = await sut.EjecutarAsync(UsuarioSupervisor, CancellationToken.None);

        Assert.Equal(["Portería", "Aula 5.º Año"], resultado.Pantallas.Select(p => p.Nombre));
        Assert.Equal(["1.º A", "5.º Año"], resultado.Clases.Select(c => c.Nombre));
    }

    [Fact]
    public async Task RevocarPantalla_ElEnlaceDejaDeFuncionar()
    {
        var token = await Crear().EjecutarAsync(UsuarioSupervisor, null, CancellationToken.None);
        var sut = new RevocarPantallaUseCase(Contexto(), _pantallas);

        await sut.EjecutarAsync(UsuarioSupervisor, _pantallas.Pantallas[0].Id, CancellationToken.None);

        Assert.Empty(_pantallas.Pantallas);
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => Publica().EjecutarAsync(token, CancellationToken.None));
    }

    [Fact]
    public async Task RevocarPantalla_DeOtroColegio_LanzaNoEncontrado()
    {
        var ajena = PantallaLlamadas.Crear(_otroColegio.Id, null, "AJENA");
        _pantallas.Pantallas.Add(ajena);
        var sut = new RevocarPantallaUseCase(Contexto(), _pantallas);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(UsuarioSupervisor, ajena.Id, CancellationToken.None));
        Assert.Contains(ajena, _pantallas.Pantallas);
    }

    [Fact]
    public async Task PantallaPorteria_ElUltimoLlamadoEnGrandeYElRestoDeMasRecienteAMasAntiguo()
    {
        Llamar(_primero, "Sofía", Ahora.AddMinutes(-4));
        Llamar(_quinto, "Diego", Ahora.AddMinutes(-1));
        Llamar(_primero, "Luis", Ahora.AddMinutes(-2));
        Llamar(_primero, "Entregado", Ahora.AddMinutes(-3)).MarcarEntregada(Ahora);
        var token = await Crear().EjecutarAsync(UsuarioSupervisor, null, CancellationToken.None);

        var pantalla = await Publica().EjecutarAsync(token, CancellationToken.None);

        Assert.Equal("Portería", pantalla.Titulo);
        Assert.Equal("Colegio Bolívar", pantalla.NombreColegio);
        Assert.NotNull(pantalla.LlamandoAhora);
        Assert.Equal("Diego Pérez", pantalla.LlamandoAhora.Alumno);
        Assert.Equal("5.º Año", pantalla.LlamandoAhora.Clase);
        Assert.Equal("AB123CD", pantalla.LlamandoAhora.Placa);
        Assert.Equal(new DateTime(2026, 10, 12, 12, 4, 0), pantalla.LlamandoAhora.LlamadoLocal);
        Assert.Equal(["Luis Pérez", "Sofía Pérez"], pantalla.Anteriores.Select(l => l.Alumno));
        Assert.Equal(new DateTime(2026, 10, 12, 12, 5, 0), pantalla.HoraLocal);
    }

    [Fact]
    public async Task PantallaDeAula_SoloLosAlumnosDeEsaClase()
    {
        Llamar(_primero, "Sofía", Ahora.AddMinutes(-4));
        Llamar(_quinto, "Diego", Ahora.AddMinutes(-1));
        var token = await Crear().EjecutarAsync(UsuarioSupervisor, _primero.Id, CancellationToken.None);

        var pantalla = await Publica().EjecutarAsync(token, CancellationToken.None);

        Assert.Equal("Aula 1.º A", pantalla.Titulo);
        Assert.Equal("Sofía Pérez", pantalla.LlamandoAhora!.Alumno);
        Assert.Empty(pantalla.Anteriores);
    }

    [Fact]
    public async Task PantallaSinPendientes_NadieLlamando()
    {
        var token = await Crear().EjecutarAsync(UsuarioSupervisor, null, CancellationToken.None);

        var pantalla = await Publica().EjecutarAsync(token, CancellationToken.None);

        Assert.Null(pantalla.LlamandoAhora);
        Assert.Empty(pantalla.Anteriores);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("token-inventado")]
    public async Task PantallaPublica_TokenAusenteOInvalido_LanzaAccesoDenegado(string? token)
    {
        await Crear().EjecutarAsync(UsuarioSupervisor, null, CancellationToken.None);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => Publica().EjecutarAsync(token, CancellationToken.None));
    }

    [Fact]
    public async Task PantallaPublica_ColegioDesactivado_LanzaAccesoDenegado()
    {
        var token = await Crear().EjecutarAsync(UsuarioSupervisor, null, CancellationToken.None);
        _colegio.Desactivar();

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => Publica().EjecutarAsync(token, CancellationToken.None));
    }
}
