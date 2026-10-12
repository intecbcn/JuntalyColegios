using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.UseCases.Colegios;
using JuntalyColegios.Application.UseCases.Salida;
using JuntalyColegios.Application.UseCases.Supervision;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Tests.Infrastructure.Fakes;
using JuntalyColegios.Tests.Infrastructure;

namespace JuntalyColegios.Tests.Application;

/// <summary>
/// Núcleo del producto: una lectura de tag genera la llamada de los hijos del dueño del vehículo solo si
/// están en su franja de retiro, sin duplicar llamadas mientras el carro avanza en la fila.
/// </summary>
public class SalidaUseCasesTests
{
    private const string UsuarioSupervisor = "supervisor-a";
    private const string Tag = "E200AABBCC";

    private readonly RepositorioColegiosEnMemoria _colegios = new();
    private readonly RepositorioSupervisoresEnMemoria _supervisores = new();
    private readonly RepositorioClasesEnMemoria _clases = new();
    private readonly RepositorioAlumnosEnMemoria _alumnos = new();
    private readonly RepositorioRepresentantesEnMemoria _representantes = new();
    private readonly RepositorioVehiculosEnMemoria _vehiculos = new();
    private readonly RepositorioAutorizadosEnMemoria _autorizados = new();
    private readonly RepositorioFranjasEnMemoria _franjas = new();
    private readonly RepositorioDeteccionesEnMemoria _detecciones = new();
    // Lunes 12/10/2026 16:05 UTC = 12:05 en Caracas.
    private readonly RelojDePrueba _reloj = new(new DateTimeOffset(2026, 10, 12, 16, 5, 0, TimeSpan.Zero));

    private readonly Colegio _colegio;
    private readonly Clase _primaria;
    private readonly Clase _bachillerato;
    private readonly Representante _padre;
    private readonly Vehiculo _carro;

    public SalidaUseCasesTests()
    {
        _colegio = Colegio.Crear("Colegio Bolívar", null, "America/Caracas");
        _colegios.Colegios.Add(_colegio);
        _supervisores.Supervisores.Add(Supervisor.Crear(_colegio.Id, UsuarioSupervisor, "Supervisor", Email.Crear("sup@colegio.com")));
        _primaria = ClaseEn(_colegio, "3.º A");
        _bachillerato = ClaseEn(_colegio, "5.º Año");
        _padre = PadreEn(_colegio, "padre-1");
        _carro = VehiculoDe(_padre, "AB123CD", Tag);
        // Salida general lunes 12:00–13:00; bachillerato sale los lunes 14:00–15:00.
        Franja(null, "12:00", "13:00");
        Franja(_bachillerato.Id, "14:00", "15:00");
    }

    private Clase ClaseEn(Colegio colegio, string nombre)
    {
        var clase = Clase.Crear(colegio.Id, nombre, null);
        _clases.Clases.Add(clase);
        return clase;
    }

    private Representante PadreEn(Colegio colegio, string usuarioId)
    {
        var padre = Representante.Crear(colegio.Id, usuarioId, "Carlos Pérez", Email.Crear($"{usuarioId}@familia.com"), null);
        _representantes.Representantes.Add(padre);
        return padre;
    }

    private Vehiculo VehiculoDe(Representante padre, string placa, string? tag)
    {
        var vehiculo = Vehiculo.Crear(padre, Placa.Crear(placa), "Corolla gris");
        if (tag is not null) vehiculo.AsignarTag(TagRfid.Crear(tag));
        _vehiculos.Vehiculos.Add(vehiculo);
        return vehiculo;
    }

    private Alumno HijoEn(Clase clase, string nombres, Representante? padre = null)
    {
        var alumno = Alumno.Crear(clase, nombres, "Pérez");
        _alumnos.Alumnos.Add(alumno);
        (padre ?? _padre).VincularAlumno(alumno);
        return alumno;
    }

    private void Franja(Guid? claseId, string inicio, string fin) =>
        _franjas.Franjas.Add(FranjaRetiro.Crear(_colegio.Id, claseId, DayOfWeek.Monday, TimeOnly.Parse(inicio), TimeOnly.Parse(fin)));

    /// <summary>Pone el reloj a una hora local de Caracas del lunes 12/10/2026.</summary>
    private void Hora(string horaLocalCaracas) =>
        _reloj.SetUtcNow(new DateTimeOffset(new DateOnly(2026, 10, 12).ToDateTime(TimeOnly.Parse(horaLocalCaracas)), TimeSpan.FromHours(-4)));

    private RegistroDeDetecciones Registro() =>
        new(_vehiculos, _representantes, _alumnos, _franjas, _detecciones, _detecciones, _reloj);

    private ContextoSupervisor Contexto() => new(_supervisores, _colegios);

    [Fact]
    public async Task Deteccion_EnFranja_LlamaASusHijosActivosYGuardaLaDeteccion()
    {
        var sofia = HijoEn(_primaria, "Sofía");
        HijoEn(_primaria, "De baja").DarDeBaja();

        var resultado = await Registro().RegistrarAsync(_colegio, "e200aabbcc", simulada: false, CancellationToken.None);

        Assert.Equal(ResultadoDeteccion.GeneraRecogida, resultado.Resultado);
        Assert.Equal("AB123CD", resultado.Placa);
        Assert.Equal(["Sofía Pérez"], resultado.AlumnosLlamados);
        var recogida = Assert.Single(_detecciones.Recogidas);
        Assert.Equal(sofia.Id, recogida.AlumnoId);
        Assert.Equal(_carro.Id, recogida.VehiculoId);
        var deteccion = Assert.Single(_detecciones.Detecciones);
        Assert.Equal(ResultadoDeteccion.GeneraRecogida, deteccion.Resultado);
        Assert.Equal(Tag, deteccion.Tag);
    }

    [Fact]
    public async Task Deteccion_FueraDeFranja_NoLlamaANadieYQuedaRegistrada()
    {
        HijoEn(_primaria, "Sofía");
        Hora("10:30");

        var resultado = await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);

        Assert.Equal(ResultadoDeteccion.FueraDeFranja, resultado.Resultado);
        Assert.Empty(resultado.AlumnosLlamados);
        Assert.Empty(_detecciones.Recogidas);
        Assert.Equal(ResultadoDeteccion.FueraDeFranja, Assert.Single(_detecciones.Detecciones).Resultado);
    }

    [Fact]
    public async Task Deteccion_HijosEnClasesConFranjasDistintas_SoloLlamaALosQueEstanEnFranja()
    {
        HijoEn(_primaria, "Sofía");
        HijoEn(_bachillerato, "Andrés");

        var mediodia = await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);
        Hora("14:30");
        var tarde = await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);

        Assert.Equal(["Sofía Pérez"], mediodia.AlumnosLlamados);
        Assert.Equal(["Andrés Pérez"], tarde.AlumnosLlamados);
    }

    [Fact]
    public async Task Deteccion_TagDesconocido_NoLlamaANadieYQuedaRegistrada()
    {
        HijoEn(_primaria, "Sofía");

        var resultado = await Registro().RegistrarAsync(_colegio, "E200FFFFFF", false, CancellationToken.None);

        Assert.Equal(ResultadoDeteccion.TagDesconocido, resultado.Resultado);
        Assert.Empty(_detecciones.Recogidas);
        Assert.Null(Assert.Single(_detecciones.Detecciones).VehiculoId);
    }

    [Fact]
    public async Task Deteccion_TagDeOtroColegio_EsDesconocidoEnEste()
    {
        var otro = Colegio.Crear("Otro", null, null);
        var padreOtro = PadreEn(otro, "padre-otro");
        VehiculoDe(padreOtro, "ZZ999ZZ", "E200OTRO01");

        var resultado = await Registro().RegistrarAsync(_colegio, "E200OTRO01", false, CancellationToken.None);

        Assert.Equal(ResultadoDeteccion.TagDesconocido, resultado.Resultado);
    }

    [Fact]
    public async Task Deteccion_TagConFormatoInvalido_LanzaDominioException()
    {
        await Assert.ThrowsAsync<DominioException>(() => Registro().RegistrarAsync(_colegio, "<script>", false, CancellationToken.None));
        Assert.Empty(_detecciones.Detecciones);
    }

    [Fact]
    public async Task Deteccion_RepetidaEnMenosDeCincoMinutos_EsDuplicadaYNoVuelveALlamar()
    {
        HijoEn(_primaria, "Sofía");
        await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);
        _reloj.Advance(TimeSpan.FromMinutes(2));

        var resultado = await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);

        Assert.Equal(ResultadoDeteccion.Duplicada, resultado.Resultado);
        Assert.Single(_detecciones.Recogidas);
        Assert.Equal(2, _detecciones.Detecciones.Count);
    }

    [Fact]
    public async Task Deteccion_PasadaLaVentanaConLaRecogidaAunPendiente_NoDuplicaLaLlamada()
    {
        HijoEn(_primaria, "Sofía");
        await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);
        _reloj.Advance(TimeSpan.FromMinutes(10));

        var resultado = await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);

        Assert.Equal(ResultadoDeteccion.Duplicada, resultado.Resultado);
        Assert.Single(_detecciones.Recogidas);
    }

    [Fact]
    public async Task Deteccion_PasadaLaVentanaYYaEntregado_VuelveALlamar()
    {
        HijoEn(_primaria, "Sofía");
        await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);
        _detecciones.Recogidas[0].MarcarEntregada(_reloj.GetUtcNow());
        _reloj.Advance(TimeSpan.FromMinutes(10));

        var resultado = await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);

        Assert.Equal(ResultadoDeteccion.GeneraRecogida, resultado.Resultado);
        Assert.Equal(2, _detecciones.Recogidas.Count);
    }

    [Fact]
    public async Task Deteccion_VehiculoSinHijosActivos_SinAlumnos()
    {
        var resultado = await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);

        Assert.Equal(ResultadoDeteccion.SinAlumnos, resultado.Resultado);
        Assert.Empty(_detecciones.Recogidas);
    }

    [Fact]
    public void ClaveDeAntena_GeneraClavesLargasYDistintas_YSuHashEsEstable()
    {
        var clave1 = ClaveDeAntena.Generar();
        var clave2 = ClaveDeAntena.Generar();

        Assert.True(clave1.Length >= 40);
        Assert.NotEqual(clave1, clave2);
        Assert.Equal(ClaveDeAntena.Hash(clave1), ClaveDeAntena.Hash(clave1));
        Assert.NotEqual(clave1, ClaveDeAntena.Hash(clave1));
    }

    [Fact]
    public async Task GenerarClaveAntena_GuardaElHashYDevuelveLaClaveUnaVez()
    {
        var sut = new GenerarClaveAntenaUseCase(_colegios);

        var clave = await sut.EjecutarAsync(_colegio.Id, CancellationToken.None);

        Assert.Equal(ClaveDeAntena.Hash(clave), _colegio.HashClaveAntena);
    }

    [Fact]
    public async Task GenerarClaveAntena_ColegioInexistente_LanzaNoEncontrado()
    {
        var sut = new GenerarClaveAntenaUseCase(_colegios);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Antena_ClaveValida_RegistraEnSuColegio()
    {
        HijoEn(_primaria, "Sofía");
        var clave = await new GenerarClaveAntenaUseCase(_colegios).EjecutarAsync(_colegio.Id, CancellationToken.None);
        var sut = new RegistrarDeteccionAntenaUseCase(_colegios, Registro());

        var resultado = await sut.EjecutarAsync(clave, Tag, CancellationToken.None);

        Assert.Equal(ResultadoDeteccion.GeneraRecogida, resultado.Resultado);
        Assert.False(_detecciones.Detecciones.Single().Simulada);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("clave-incorrecta")]
    public async Task Antena_ClaveAusenteOIncorrecta_LanzaAccesoDenegadoSinRegistrar(string? clave)
    {
        await new GenerarClaveAntenaUseCase(_colegios).EjecutarAsync(_colegio.Id, CancellationToken.None);
        var sut = new RegistrarDeteccionAntenaUseCase(_colegios, Registro());

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => sut.EjecutarAsync(clave, Tag, CancellationToken.None));
        Assert.Empty(_detecciones.Detecciones);
    }

    [Fact]
    public async Task Antena_ClaveAnteriorTrasRegenerarla_LanzaAccesoDenegado()
    {
        var generar = new GenerarClaveAntenaUseCase(_colegios);
        var anterior = await generar.EjecutarAsync(_colegio.Id, CancellationToken.None);
        await generar.EjecutarAsync(_colegio.Id, CancellationToken.None);
        var sut = new RegistrarDeteccionAntenaUseCase(_colegios, Registro());

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => sut.EjecutarAsync(anterior, Tag, CancellationToken.None));
    }

    [Fact]
    public async Task Antena_ColegioDesactivado_LanzaAccesoDenegado()
    {
        var clave = await new GenerarClaveAntenaUseCase(_colegios).EjecutarAsync(_colegio.Id, CancellationToken.None);
        _colegio.Desactivar();
        var sut = new RegistrarDeteccionAntenaUseCase(_colegios, Registro());

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => sut.EjecutarAsync(clave, Tag, CancellationToken.None));
    }

    [Fact]
    public async Task SimularLlegada_VehiculoConTag_RegistraUnaDeteccionSimulada()
    {
        HijoEn(_primaria, "Sofía");
        var sut = new SimularLlegadaUseCase(Contexto(), _vehiculos, Registro());

        var resultado = await sut.EjecutarAsync(UsuarioSupervisor, _carro.Id, CancellationToken.None);

        Assert.Equal(ResultadoDeteccion.GeneraRecogida, resultado.Resultado);
        Assert.True(Assert.Single(_detecciones.Detecciones).Simulada);
    }

    [Fact]
    public async Task SimularLlegada_VehiculoSinTag_LanzaDominioException()
    {
        var sinTag = VehiculoDe(_padre, "SINTAG1", null);
        var sut = new SimularLlegadaUseCase(Contexto(), _vehiculos, Registro());

        await Assert.ThrowsAsync<DominioException>(() => sut.EjecutarAsync(UsuarioSupervisor, sinTag.Id, CancellationToken.None));
    }

    [Fact]
    public async Task SimularLlegada_VehiculoDeOtroColegio_LanzaNoEncontrado()
    {
        var otro = Colegio.Crear("Otro", null, null);
        var ajeno = VehiculoDe(PadreEn(otro, "padre-otro"), "ZZ999ZZ", "E200OTRO01");
        var sut = new SimularLlegadaUseCase(Contexto(), _vehiculos, Registro());

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(UsuarioSupervisor, ajeno.Id, CancellationToken.None));
    }

    [Fact]
    public async Task PantallaSalida_MuestraLasPendientesConClaseVehiculoYAutorizadosDeLaFamilia()
    {
        HijoEn(_primaria, "Sofía");
        _autorizados.Autorizados.Add(PersonaAutorizada.Crear(_padre, "Elena Pérez", "V-5555555", "Abuela"));
        VehiculoDe(_padre, "SINTAG1", null);
        await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);
        var sut = new ObtenerPantallaSalidaUseCase(Contexto(), _detecciones, _alumnos, _clases, _vehiculos, _representantes, _autorizados, _reloj);

        var pantalla = await sut.EjecutarAsync(UsuarioSupervisor, CancellationToken.None);

        var pendiente = Assert.Single(pantalla.Pendientes);
        Assert.Equal("Sofía Pérez", pendiente.Alumno);
        Assert.Equal("3.º A", pendiente.Clase);
        Assert.Equal("AB123CD", pendiente.Placa);
        Assert.Equal("Corolla gris", pendiente.Vehiculo);
        Assert.Equal("Carlos Pérez", pendiente.Representante);
        Assert.Equal(new DateTime(2026, 10, 12, 12, 5, 0), pendiente.DetectadoEnLocal);
        Assert.Equal("Elena Pérez", Assert.Single(pendiente.Autorizados).Nombre);
        // Solo los vehículos con tag se pueden simular.
        Assert.Equal(["AB123CD"], pantalla.VehiculosParaSimular.Select(v => v.Placa));
    }

    [Fact]
    public async Task MarcarEntregada_LaQuitaDeLaPantalla()
    {
        HijoEn(_primaria, "Sofía");
        await Registro().RegistrarAsync(_colegio, Tag, false, CancellationToken.None);
        var recogida = _detecciones.Recogidas.Single();
        _reloj.Advance(TimeSpan.FromMinutes(3));

        await new MarcarEntregadaUseCase(Contexto(), _detecciones, _reloj).EjecutarAsync(UsuarioSupervisor, recogida.Id, CancellationToken.None);

        Assert.Equal(_reloj.GetUtcNow(), recogida.EntregadoEn);
        var pantalla = await new ObtenerPantallaSalidaUseCase(Contexto(), _detecciones, _alumnos, _clases, _vehiculos, _representantes, _autorizados, _reloj)
            .EjecutarAsync(UsuarioSupervisor, CancellationToken.None);
        Assert.Empty(pantalla.Pendientes);
    }

    [Fact]
    public async Task MarcarEntregada_DeOtroColegio_LanzaNoEncontrado()
    {
        var otro = Colegio.Crear("Otro", null, null);
        _colegios.Colegios.Add(otro);
        var deteccion = Deteccion.Crear(otro.Id, "E200OTRO01", Guid.NewGuid(), _reloj.GetUtcNow(), ResultadoDeteccion.GeneraRecogida, false);
        var ajena = Recogida.Crear(deteccion, Guid.NewGuid());
        _detecciones.Recogidas.Add(ajena);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            new MarcarEntregadaUseCase(Contexto(), _detecciones, _reloj).EjecutarAsync(UsuarioSupervisor, ajena.Id, CancellationToken.None));
        Assert.True(ajena.Pendiente);
    }
}
