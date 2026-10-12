using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.UseCases.Supervision;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Tests.Infrastructure.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace JuntalyColegios.Tests.Application;

/// <summary>Configuración de franjas de retiro por el supervisor, limitada a su colegio.</summary>
public class FranjasUseCasesTests
{
    private const string UsuarioA = "supervisor-a";

    private static readonly DayOfWeek[] LunesAViernes =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    private readonly RepositorioColegiosEnMemoria _colegios = new();
    private readonly RepositorioSupervisoresEnMemoria _supervisores = new();
    private readonly RepositorioClasesEnMemoria _clases = new();
    private readonly RepositorioFranjasEnMemoria _franjas = new();
    // Lunes 12/10/2026 16:05 UTC = 12:05 en Caracas.
    private readonly FakeTimeProvider _reloj = new(new DateTimeOffset(2026, 10, 12, 16, 5, 0, TimeSpan.Zero));
    private readonly Colegio _colegioA;
    private readonly Colegio _colegioB;
    private readonly Clase _primeroA;

    public FranjasUseCasesTests()
    {
        _colegioA = ColegioConSupervisor("Colegio A", UsuarioA);
        _colegioB = ColegioConSupervisor("Colegio B", "supervisor-b");
        _primeroA = Clase.Crear(_colegioA.Id, "1.º A", null);
        _clases.Clases.Add(_primeroA);
    }

    private Colegio ColegioConSupervisor(string nombre, string usuarioId)
    {
        var colegio = Colegio.Crear(nombre, null, "America/Caracas");
        _colegios.Colegios.Add(colegio);
        _supervisores.Supervisores.Add(Supervisor.Crear(colegio.Id, usuarioId, "Supervisor", Email.Crear($"{usuarioId}@colegio.com")));
        return colegio;
    }

    private ContextoSupervisor Contexto() => new(_supervisores, _colegios);

    private static TimeOnly H(string hora) => TimeOnly.Parse(hora);

    private CrearFranjasUseCase Crear() => new(Contexto(), _franjas, _clases);

    [Fact]
    public async Task CrearFranjas_LunesAViernesParaTodoElColegio_CreaUnaPorDia()
    {
        var creadas = await Crear().EjecutarAsync(UsuarioA,
            new DatosFranjasDto(null, LunesAViernes, H("12:00"), H("13:00")), CancellationToken.None);

        Assert.Equal(5, creadas);
        Assert.All(_franjas.Franjas, f =>
        {
            Assert.Equal(_colegioA.Id, f.ColegioId);
            Assert.Null(f.ClaseId);
        });
        Assert.Equal(LunesAViernes, _franjas.Franjas.Select(f => f.DiaSemana).Order());
    }

    [Fact]
    public async Task CrearFranjas_ParaUnaClaseDeSuColegio_LaGuardaConLaClase()
    {
        await Crear().EjecutarAsync(UsuarioA,
            new DatosFranjasDto(_primeroA.Id, [DayOfWeek.Friday], H("11:00"), H("11:30")), CancellationToken.None);

        Assert.Equal(_primeroA.Id, Assert.Single(_franjas.Franjas).ClaseId);
    }

    [Fact]
    public async Task CrearFranjas_ClaseDeOtroColegio_LanzaNoEncontrado()
    {
        var ajena = Clase.Crear(_colegioB.Id, "Ajena", null);
        _clases.Clases.Add(ajena);

        await Assert.ThrowsAsync<NoEncontradoException>(() => Crear().EjecutarAsync(UsuarioA,
            new DatosFranjasDto(ajena.Id, [DayOfWeek.Monday], H("12:00"), H("13:00")), CancellationToken.None));
        Assert.Empty(_franjas.Franjas);
    }

    [Fact]
    public async Task CrearFranjas_SinDias_LanzaDominioException()
    {
        await Assert.ThrowsAsync<DominioException>(() => Crear().EjecutarAsync(UsuarioA,
            new DatosFranjasDto(null, [], H("12:00"), H("13:00")), CancellationToken.None));
    }

    [Fact]
    public async Task CrearFranjas_UnDiaSeSolapa_NoCreaNingunaYIndicaElDia()
    {
        _franjas.Franjas.Add(FranjaRetiro.Crear(_colegioA.Id, null, DayOfWeek.Wednesday, H("12:30"), H("14:00")));

        var ex = await Assert.ThrowsAsync<DominioException>(() => Crear().EjecutarAsync(UsuarioA,
            new DatosFranjasDto(null, LunesAViernes, H("12:00"), H("13:00")), CancellationToken.None));

        Assert.Contains("miércoles", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(_franjas.Franjas);
    }

    [Fact]
    public async Task CrearFranjas_MismasHorasQueElColegioPeroParaUnaClase_Permitido()
    {
        _franjas.Franjas.Add(FranjaRetiro.Crear(_colegioA.Id, null, DayOfWeek.Monday, H("12:00"), H("13:00")));

        await Crear().EjecutarAsync(UsuarioA,
            new DatosFranjasDto(_primeroA.Id, [DayOfWeek.Monday], H("12:00"), H("13:00")), CancellationToken.None);

        Assert.Equal(2, _franjas.Franjas.Count);
    }

    [Fact]
    public async Task CrearFranjas_SolapeConFranjaDeOtroColegio_NoCuenta()
    {
        _franjas.Franjas.Add(FranjaRetiro.Crear(_colegioB.Id, null, DayOfWeek.Monday, H("12:00"), H("13:00")));

        await Crear().EjecutarAsync(UsuarioA,
            new DatosFranjasDto(null, [DayOfWeek.Monday], H("12:00"), H("13:00")), CancellationToken.None);

        Assert.Equal(2, _franjas.Franjas.Count);
    }

    [Fact]
    public async Task ListarFranjas_SoloLasDeSuColegioOrdenadasDeLunesADomingoYConElAmbito()
    {
        _franjas.Franjas.Add(FranjaRetiro.Crear(_colegioA.Id, null, DayOfWeek.Sunday, H("09:00"), H("10:00")));
        _franjas.Franjas.Add(FranjaRetiro.Crear(_colegioA.Id, _primeroA.Id, DayOfWeek.Monday, H("14:00"), H("15:00")));
        _franjas.Franjas.Add(FranjaRetiro.Crear(_colegioA.Id, null, DayOfWeek.Monday, H("12:00"), H("13:00")));
        _franjas.Franjas.Add(FranjaRetiro.Crear(_colegioB.Id, null, DayOfWeek.Monday, H("08:00"), H("09:00")));
        var sut = new ListarFranjasUseCase(Contexto(), _franjas, _clases, _reloj);

        var resultado = await sut.EjecutarAsync(UsuarioA, CancellationToken.None);

        Assert.Equal(
            ["Monday 12:00 Todo el colegio", "Monday 14:00 1.º A", "Sunday 09:00 Todo el colegio"],
            resultado.Franjas.Select(f => $"{f.DiaSemana} {f.HoraInicio:HH\\:mm} {f.Ambito}"));
        Assert.Equal(["1.º A"], resultado.Clases.Select(c => c.Nombre));
    }

    [Fact]
    public async Task ListarFranjas_IndicaLaHoraLocalDelColegioYSiAhoraSeLlamaALosAlumnos()
    {
        _franjas.Franjas.Add(FranjaRetiro.Crear(_colegioA.Id, null, DayOfWeek.Monday, H("12:00"), H("13:00")));
        var sut = new ListarFranjasUseCase(Contexto(), _franjas, _clases, _reloj);

        var dentro = await sut.EjecutarAsync(UsuarioA, CancellationToken.None);
        _reloj.Advance(TimeSpan.FromHours(2));
        var fuera = await sut.EjecutarAsync(UsuarioA, CancellationToken.None);

        Assert.Equal(new DateTime(2026, 10, 12, 12, 5, 0), dentro.HoraLocalActual);
        Assert.True(dentro.ColegioEnFranjaAhora);
        Assert.False(fuera.ColegioEnFranjaAhora);
    }

    [Fact]
    public async Task EliminarFranja_Propia_LaElimina()
    {
        var franja = FranjaRetiro.Crear(_colegioA.Id, null, DayOfWeek.Monday, H("12:00"), H("13:00"));
        _franjas.Franjas.Add(franja);
        var sut = new EliminarFranjaUseCase(Contexto(), _franjas);

        await sut.EjecutarAsync(UsuarioA, franja.Id, CancellationToken.None);

        Assert.Empty(_franjas.Franjas);
    }

    [Fact]
    public async Task EliminarFranja_DeOtroColegio_LanzaNoEncontradoYNoLaElimina()
    {
        var ajena = FranjaRetiro.Crear(_colegioB.Id, null, DayOfWeek.Monday, H("12:00"), H("13:00"));
        _franjas.Franjas.Add(ajena);
        var sut = new EliminarFranjaUseCase(Contexto(), _franjas);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(UsuarioA, ajena.Id, CancellationToken.None));
        Assert.Contains(ajena, _franjas.Franjas);
    }
}
