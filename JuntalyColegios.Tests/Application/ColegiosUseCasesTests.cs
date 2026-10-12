using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.Seguridad;
using JuntalyColegios.Application.UseCases.Colegios;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Tests.Infrastructure.Fakes;
using NSubstitute;

namespace JuntalyColegios.Tests.Application;

/// <summary>Casos de uso de gestión de colegios y supervisores, con los puertos de salida sustituidos.</summary>
public class ColegiosUseCasesTests
{
    private readonly RepositorioColegiosEnMemoria _colegios = new();
    private readonly RepositorioSupervisoresEnMemoria _supervisores = new();
    private readonly IServicioUsuarios _usuarios = Substitute.For<IServicioUsuarios>();

    private Colegio ColegioExistente(string nombre = "Colegio San José")
    {
        var colegio = Colegio.Crear(nombre, null, null);
        _colegios.Colegios.Add(colegio);
        return colegio;
    }

    [Fact]
    public async Task CrearColegio_DatosValidos_LoGuardaYDevuelveSuId()
    {
        var sut = new CrearColegioUseCase(_colegios);

        var id = await sut.EjecutarAsync(new DatosColegioDto("Colegio San José", "Av. 1", null), CancellationToken.None);

        var guardado = Assert.Single(_colegios.Colegios);
        Assert.Equal(id, guardado.Id);
        Assert.Equal("Colegio San José", guardado.Nombre);
    }

    [Fact]
    public async Task CrearColegio_DatosInvalidos_LanzaDominioExceptionYNoGuarda()
    {
        var sut = new CrearColegioUseCase(_colegios);

        await Assert.ThrowsAsync<DominioException>(() =>
            sut.EjecutarAsync(new DatosColegioDto("", null, null), CancellationToken.None));
        Assert.Empty(_colegios.Colegios);
    }

    [Fact]
    public async Task ListarColegios_DevuelveTodosOrdenadosPorNombre()
    {
        ColegioExistente("Colegio Zeta");
        ColegioExistente("Colegio Alfa");
        var sut = new ListarColegiosUseCase(_colegios);

        var lista = await sut.EjecutarAsync(CancellationToken.None);

        Assert.Equal(["Colegio Alfa", "Colegio Zeta"], lista.Select(c => c.Nombre));
    }

    [Fact]
    public async Task ObtenerColegio_Existente_DevuelveDatosYSupervisores()
    {
        var colegio = ColegioExistente();
        var otro = ColegioExistente("Otro colegio");
        _supervisores.Supervisores.Add(Supervisor.Crear(colegio.Id, "u1", "Luis", Email.Crear("luis@colegio.com")));
        _supervisores.Supervisores.Add(Supervisor.Crear(otro.Id, "u2", "Eva", Email.Crear("eva@otro.com")));
        var sut = new ObtenerColegioUseCase(_colegios, _supervisores);

        var detalle = await sut.EjecutarAsync(colegio.Id, CancellationToken.None);

        Assert.Equal(colegio.Id, detalle.Colegio.Id);
        var supervisor = Assert.Single(detalle.Supervisores);
        Assert.Equal("luis@colegio.com", supervisor.Email);
    }

    [Fact]
    public async Task ObtenerColegio_Inexistente_LanzaNoEncontradoException()
    {
        var sut = new ObtenerColegioUseCase(_colegios, _supervisores);

        await Assert.ThrowsAsync<NoEncontradoException>(() => sut.EjecutarAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EditarColegio_Existente_ActualizaYPersiste()
    {
        var colegio = ColegioExistente();
        var sut = new EditarColegioUseCase(_colegios);

        await sut.EjecutarAsync(colegio.Id, new DatosColegioDto("Nuevo nombre", "Calle 9", "America/Lima"), CancellationToken.None);

        Assert.Equal("Nuevo nombre", colegio.Nombre);
        Assert.Equal(1, _colegios.Actualizaciones);
    }

    [Fact]
    public async Task EditarColegio_Inexistente_LanzaNoEncontradoException()
    {
        var sut = new EditarColegioUseCase(_colegios);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            sut.EjecutarAsync(Guid.NewGuid(), new DatosColegioDto("X", null, null), CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CambiarEstadoColegio_CambiaActivoYPersiste(bool activo)
    {
        var colegio = ColegioExistente();
        if (activo) colegio.Desactivar();
        var sut = new CambiarEstadoColegioUseCase(_colegios);

        await sut.EjecutarAsync(colegio.Id, activo, CancellationToken.None);

        Assert.Equal(activo, colegio.Activo);
        Assert.Equal(1, _colegios.Actualizaciones);
    }

    [Fact]
    public async Task AltaSupervisor_ColegioActivo_CreaUsuarioConRolSupervisorYLoVinculaAlColegio()
    {
        var colegio = ColegioExistente();
        _usuarios.CrearUsuarioInvitadoAsync("luis@colegio.com", "Luis Gómez", Roles.SupervisorColegio, Arg.Any<CancellationToken>())
            .Returns("usuario-luis");
        var sut = new AltaSupervisorUseCase(_colegios, _supervisores, _usuarios);

        var id = await sut.EjecutarAsync(new AltaSupervisorDto(colegio.Id, "Luis Gómez", " Luis@Colegio.com "), CancellationToken.None);

        var supervisor = Assert.Single(_supervisores.Supervisores);
        Assert.Equal(id, supervisor.Id);
        Assert.Equal(colegio.Id, supervisor.ColegioId);
        Assert.Equal("usuario-luis", supervisor.UsuarioId);
        Assert.Equal("luis@colegio.com", supervisor.Email);
    }

    [Fact]
    public async Task AltaSupervisor_FallaAlGuardarElSupervisor_EliminaLaCuentaCreada()
    {
        var colegio = ColegioExistente();
        _usuarios.CrearUsuarioInvitadoAsync(default!, default!, default!, default).ReturnsForAnyArgs("usuario-huerfano");
        _supervisores.FallarAlAgregar = true;
        var sut = new AltaSupervisorUseCase(_colegios, _supervisores, _usuarios);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.EjecutarAsync(new AltaSupervisorDto(colegio.Id, "Luis", "luis@colegio.com"), CancellationToken.None));

        await _usuarios.Received(1).EliminarUsuarioAsync("usuario-huerfano", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AltaSupervisor_ColegioInexistente_LanzaNoEncontradoSinCrearUsuario()
    {
        var sut = new AltaSupervisorUseCase(_colegios, _supervisores, _usuarios);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            sut.EjecutarAsync(new AltaSupervisorDto(Guid.NewGuid(), "Luis", "luis@colegio.com"), CancellationToken.None));
        await _usuarios.DidNotReceiveWithAnyArgs().CrearUsuarioInvitadoAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task AltaSupervisor_ColegioInactivo_LanzaDominioExceptionSinCrearUsuario()
    {
        var colegio = ColegioExistente();
        colegio.Desactivar();
        var sut = new AltaSupervisorUseCase(_colegios, _supervisores, _usuarios);

        await Assert.ThrowsAsync<DominioException>(() =>
            sut.EjecutarAsync(new AltaSupervisorDto(colegio.Id, "Luis", "luis@colegio.com"), CancellationToken.None));
        await _usuarios.DidNotReceiveWithAnyArgs().CrearUsuarioInvitadoAsync(default!, default!, default!, default);
    }

    [Theory]
    [InlineData("Luis", "no-es-email")]
    [InlineData(" ", "luis@colegio.com")]
    public async Task AltaSupervisor_DatosInvalidos_LanzaDominioExceptionSinCrearUsuario(string nombre, string email)
    {
        var colegio = ColegioExistente();
        var sut = new AltaSupervisorUseCase(_colegios, _supervisores, _usuarios);

        await Assert.ThrowsAsync<DominioException>(() =>
            sut.EjecutarAsync(new AltaSupervisorDto(colegio.Id, nombre, email), CancellationToken.None));
        await _usuarios.DidNotReceiveWithAnyArgs().CrearUsuarioInvitadoAsync(default!, default!, default!, default);
    }
}
