using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.UseCases.Historial;
using JuntalyColegios.Application.UseCases.Padres;
using JuntalyColegios.Application.UseCases.Supervision;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Tests.Infrastructure;
using JuntalyColegios.Tests.Infrastructure.Fakes;

namespace JuntalyColegios.Tests.Application;

/// <summary>Historial de recogidas del padre y registro de detecciones del supervisor.</summary>
public class HistorialUseCasesTests
{
    private const string UsuarioSupervisor = "supervisor-a";
    private const string UsuarioPadre = "padre-1";
    // Lunes 12/10/2026 16:05 UTC = 12:05 en Caracas.
    private static readonly DateTimeOffset Base = new(2026, 10, 12, 16, 5, 0, TimeSpan.Zero);

    private readonly RepositorioColegiosEnMemoria _colegios = new();
    private readonly RepositorioSupervisoresEnMemoria _supervisores = new();
    private readonly RepositorioClasesEnMemoria _clases = new();
    private readonly RepositorioAlumnosEnMemoria _alumnos = new();
    private readonly RepositorioRepresentantesEnMemoria _representantes = new();
    private readonly RepositorioVehiculosEnMemoria _vehiculos = new();
    private readonly RepositorioDeteccionesEnMemoria _detecciones = new();
    private readonly Colegio _colegio;
    private readonly Colegio _otroColegio;
    private readonly Clase _clase;
    private readonly Representante _padre;
    private readonly Vehiculo _carro;

    public HistorialUseCasesTests()
    {
        _colegio = Colegio.Crear("Colegio Bolívar", null, "America/Caracas");
        _otroColegio = Colegio.Crear("Otro", null, "America/Caracas");
        _colegios.Colegios.AddRange([_colegio, _otroColegio]);
        _supervisores.Supervisores.Add(Supervisor.Crear(_colegio.Id, UsuarioSupervisor, "Supervisor", Email.Crear("sup@colegio.com")));
        _clase = Clase.Crear(_colegio.Id, "3.º A", null);
        _clases.Clases.Add(_clase);
        _padre = Representante.Crear(_colegio.Id, UsuarioPadre, "Carlos Pérez", Email.Crear("carlos@familia.com"), null);
        _representantes.Representantes.Add(_padre);
        _carro = Vehiculo.Crear(_padre, Placa.Crear("AB123CD"), null);
        _carro.AsignarTag(TagRfid.Crear("E200AABB"));
        _vehiculos.Vehiculos.Add(_carro);
    }

    private Alumno Hijo(string nombres)
    {
        var alumno = Alumno.Crear(_clase, nombres, "Pérez");
        _alumnos.Alumnos.Add(alumno);
        _padre.VincularAlumno(alumno);
        return alumno;
    }

    private Deteccion Detectar(Guid colegioId, DateTimeOffset cuando, ResultadoDeteccion resultado, Guid? vehiculoId, string tag = "E200AABB", bool simulada = false)
    {
        var deteccion = Deteccion.Crear(colegioId, tag, vehiculoId, cuando, resultado, simulada);
        _detecciones.Detecciones.Add(deteccion);
        return deteccion;
    }

    private Recogida Recoger(Alumno alumno, DateTimeOffset cuando, DateTimeOffset? entregado = null)
    {
        var recogida = Recogida.Crear(Detectar(alumno.ColegioId, cuando, ResultadoDeteccion.GeneraRecogida, _carro.Id), alumno.Id);
        if (entregado is { } e) recogida.MarcarEntregada(e);
        _detecciones.Recogidas.Add(recogida);
        return recogida;
    }

    private ObtenerHistorialPadreUseCase HistorialPadre() =>
        new(new ContextoPadre(_representantes, _colegios), _alumnos, _vehiculos, _detecciones);

    [Fact]
    public async Task HistorialPadre_SusRecogidasDeLaMasRecienteALaMasAntiguaEnHoraLocal()
    {
        var sofia = Hijo("Sofía");
        var retirado = Hijo("Retirado");
        Recoger(sofia, Base.AddDays(-1), Base.AddDays(-1).AddMinutes(4));
        Recoger(retirado, Base.AddDays(-2), Base.AddDays(-2).AddMinutes(2));
        retirado.DarDeBaja();
        Recoger(sofia, Base);

        var historial = await HistorialPadre().EjecutarAsync(UsuarioPadre, CancellationToken.None);

        Assert.Equal(3, historial.Count);
        Assert.Equal(new DateTime(2026, 10, 12, 12, 5, 0), historial[0].LlegadaLocal);
        Assert.Null(historial[0].EntregaLocal);
        Assert.Equal(new DateTime(2026, 10, 11, 12, 9, 0), historial[1].EntregaLocal);
        Assert.Equal("Retirado Pérez", historial[2].Alumno);
        Assert.All(historial, h => Assert.Equal("AB123CD", h.Placa));
    }

    [Fact]
    public async Task HistorialPadre_NoIncluyeRecogidasDeOtrasFamilias()
    {
        Hijo("Sofía");
        var ajeno = Alumno.Crear(_clase, "Ajeno", "López");
        _alumnos.Alumnos.Add(ajeno);
        Recoger(ajeno, Base);

        Assert.Empty(await HistorialPadre().EjecutarAsync(UsuarioPadre, CancellationToken.None));
    }

    [Fact]
    public async Task HistorialPadre_MuestraComoMaximoLasVeinteMasRecientes()
    {
        var sofia = Hijo("Sofía");
        for (var dia = 0; dia < 25; dia++)
            Recoger(sofia, Base.AddDays(-dia), Base.AddDays(-dia).AddMinutes(3));

        var historial = await HistorialPadre().EjecutarAsync(UsuarioPadre, CancellationToken.None);

        Assert.Equal(ObtenerHistorialPadreUseCase.Maximo, historial.Count);
        Assert.Equal(new DateTime(2026, 10, 12, 12, 5, 0), historial[0].LlegadaLocal);
    }

    [Fact]
    public async Task HistorialPadre_UsuarioSinFamilia_LanzaAccesoDenegado()
    {
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => HistorialPadre().EjecutarAsync("otro-usuario", CancellationToken.None));
    }

    private ListarDeteccionesUseCase Detecciones() =>
        new(new ContextoSupervisor(_supervisores, _colegios), _detecciones, _vehiculos, _representantes);

    [Fact]
    public async Task ListarDetecciones_LasDeSuColegioDeLaMasRecienteALaMasAntiguaConVehiculoYFamilia()
    {
        Detectar(_colegio.Id, Base.AddMinutes(-30), ResultadoDeteccion.FueraDeFranja, _carro.Id);
        Detectar(_colegio.Id, Base, ResultadoDeteccion.TagDesconocido, null, "E200FFFF", simulada: true);
        Detectar(_otroColegio.Id, Base, ResultadoDeteccion.GeneraRecogida, null, "E200OTRO");

        var lista = await Detecciones().EjecutarAsync(UsuarioSupervisor, CancellationToken.None);

        Assert.Equal(2, lista.Count);
        Assert.Equal("E200FFFF", lista[0].Tag);
        Assert.Null(lista[0].Placa);
        Assert.True(lista[0].Simulada);
        Assert.Equal(ResultadoDeteccion.FueraDeFranja, lista[1].Resultado);
        Assert.Equal("AB123CD", lista[1].Placa);
        Assert.Equal("Carlos Pérez", lista[1].Familia);
        Assert.Equal(new DateTime(2026, 10, 12, 11, 35, 0), lista[1].DetectadoEnLocal);
    }

    [Fact]
    public async Task ListarDetecciones_MuestraComoMaximoLasCincuentaMasRecientes()
    {
        for (var i = 0; i < 60; i++)
            Detectar(_colegio.Id, Base.AddMinutes(-i), ResultadoDeteccion.Duplicada, _carro.Id);

        var lista = await Detecciones().EjecutarAsync(UsuarioSupervisor, CancellationToken.None);

        Assert.Equal(ListarDeteccionesUseCase.Maximo, lista.Count);
        Assert.Equal(new DateTime(2026, 10, 12, 12, 5, 0), lista[0].DetectadoEnLocal);
    }
}
