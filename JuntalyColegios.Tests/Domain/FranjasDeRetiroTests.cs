using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.Servicios;

namespace JuntalyColegios.Tests.Domain;

public class FranjasDeRetiroTests
{
    private static readonly Guid ColegioId = Guid.NewGuid();
    private static readonly Guid ClaseA = Guid.NewGuid();
    private static readonly Guid ClaseB = Guid.NewGuid();

    private static TimeOnly H(string hora) => TimeOnly.Parse(hora);

    // 12/10/2026 es lunes.
    private static DateTime Lunes(string hora) => new DateOnly(2026, 10, 12).ToDateTime(H(hora));

    private static FranjaRetiro Franja(DayOfWeek dia, string inicio, string fin, Guid? claseId = null) =>
        FranjaRetiro.Crear(ColegioId, claseId, dia, H(inicio), H(fin));

    [Fact]
    public void Crear_DatosValidos()
    {
        var franja = Franja(DayOfWeek.Monday, "12:00", "13:00", ClaseA);

        Assert.NotEqual(Guid.Empty, franja.Id);
        Assert.Equal(ColegioId, franja.ColegioId);
        Assert.Equal(ClaseA, franja.ClaseId);
        Assert.Equal(DayOfWeek.Monday, franja.DiaSemana);
        Assert.Equal(H("12:00"), franja.HoraInicio);
        Assert.Equal(H("13:00"), franja.HoraFin);
    }

    [Theory]
    [InlineData("13:00", "12:00")]
    [InlineData("12:00", "12:00")]
    public void Crear_InicioNoAnteriorAlFin_LanzaDominioException(string inicio, string fin)
    {
        Assert.Throws<DominioException>(() => Franja(DayOfWeek.Monday, inicio, fin));
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, "12:00", true)]
    [InlineData(DayOfWeek.Monday, "12:05", true)]
    [InlineData(DayOfWeek.Monday, "13:00", true)]
    [InlineData(DayOfWeek.Monday, "11:59", false)]
    [InlineData(DayOfWeek.Monday, "13:01", false)]
    [InlineData(DayOfWeek.Tuesday, "12:05", false)]
    public void Contiene_EvaluaDiaYHoraConLimitesIncluidos(DayOfWeek dia, string hora, bool esperado)
    {
        Assert.Equal(esperado, Franja(DayOfWeek.Monday, "12:00", "13:00").Contiene(dia, H(hora)));
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, "12:30", "13:30", null, true)]
    [InlineData(DayOfWeek.Monday, "11:00", "14:00", null, true)]
    [InlineData(DayOfWeek.Monday, "13:00", "14:00", null, false)]
    [InlineData(DayOfWeek.Tuesday, "12:30", "13:30", null, false)]
    [InlineData(DayOfWeek.Monday, "12:30", "13:30", "clase", false)]
    public void SeSolapaCon_SoloMismoDiaMismoAmbitoYHorasQueSeCruzan(DayOfWeek dia, string inicio, string fin, string? ambito, bool esperado)
    {
        var existente = Franja(DayOfWeek.Monday, "12:00", "13:00");

        var nueva = Franja(dia, inicio, fin, ambito is null ? null : ClaseA);

        Assert.Equal(esperado, nueva.SeSolapaCon(existente));
    }

    [Fact]
    public void Colegio_HoraLocal_ConvierteElInstanteALaZonaHorariaDelColegio()
    {
        var instante = new DateTimeOffset(2026, 10, 12, 16, 5, 0, TimeSpan.Zero);

        Assert.Equal(Lunes("12:05"), Colegio.Crear("Caracas", null, "America/Caracas").HoraLocal(instante));
        Assert.Equal(Lunes("18:05"), Colegio.Crear("Madrid", null, "Europe/Madrid").HoraLocal(instante));
    }

    [Theory]
    [InlineData("12:05", true)]
    [InlineData("11:59", false)]
    public void Horario_FranjaDelColegio_AplicaATodasLasClases(string hora, bool esperado)
    {
        var horario = new HorarioDeRetiro([Franja(DayOfWeek.Monday, "12:00", "13:00")]);

        Assert.Equal(esperado, horario.EstaEnFranja(ClaseA, Lunes(hora)));
        Assert.Equal(esperado, horario.EstaEnFranja(ClaseB, Lunes(hora)));
    }

    [Fact]
    public void Horario_Sabado_SinFranja_Fuera()
    {
        var horario = new HorarioDeRetiro([Franja(DayOfWeek.Monday, "12:00", "13:00")]);

        Assert.False(horario.EstaEnFranja(ClaseA, new DateOnly(2026, 10, 17).ToDateTime(H("12:05"))));
    }

    [Fact]
    public void Horario_SinNingunaFranja_NoSeLlamaANadie()
    {
        Assert.False(new HorarioDeRetiro([]).EstaEnFranja(ClaseA, Lunes("12:05")));
    }

    [Fact]
    public void Horario_FranjaDeLaClaseEseDia_PrevaleceSobreLaDelColegio()
    {
        var horario = new HorarioDeRetiro(
        [
            Franja(DayOfWeek.Monday, "12:00", "13:00"),
            Franja(DayOfWeek.Monday, "14:00", "15:00", ClaseA)
        ]);

        Assert.False(horario.EstaEnFranja(ClaseA, Lunes("12:05")));
        Assert.True(horario.EstaEnFranja(ClaseA, Lunes("14:30")));
        Assert.True(horario.EstaEnFranja(ClaseB, Lunes("12:05")));
        Assert.False(horario.EstaEnFranja(ClaseB, Lunes("14:30")));
    }

    [Fact]
    public void Horario_ClaseSinFranjaEseDia_UsaLaDelColegio()
    {
        var horario = new HorarioDeRetiro(
        [
            Franja(DayOfWeek.Tuesday, "12:00", "13:00"),
            Franja(DayOfWeek.Monday, "14:00", "15:00", ClaseA)
        ]);

        var martes = new DateOnly(2026, 10, 13).ToDateTime(H("12:05"));

        Assert.True(horario.EstaEnFranja(ClaseA, martes));
    }

    [Fact]
    public void Horario_VariasFranjasElMismoDia_BastaConEstarEnUna()
    {
        var horario = new HorarioDeRetiro(
        [
            Franja(DayOfWeek.Monday, "12:00", "12:30"),
            Franja(DayOfWeek.Monday, "15:00", "15:30")
        ]);

        Assert.True(horario.EstaEnFranja(ClaseA, Lunes("15:10")));
        Assert.False(horario.EstaEnFranja(ClaseA, Lunes("13:00")));
    }
}
