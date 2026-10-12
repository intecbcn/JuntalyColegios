using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Excepciones;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.UseCases.Colegios;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;
using JuntalyColegios.Tests.Infrastructure.Fakes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace JuntalyColegios.Tests.Application;

/// <summary>Edición y baja de supervisores por el administrador.</summary>
public class SupervisoresUseCasesTests
{
    private readonly RepositorioColegiosEnMemoria _colegios = new();
    private readonly RepositorioSupervisoresEnMemoria _supervisores = new();
    private readonly IServicioUsuarios _usuarios = Substitute.For<IServicioUsuarios>();
    private readonly Colegio _colegio;
    private readonly Colegio _otroColegio;
    private readonly Supervisor _luis;

    public SupervisoresUseCasesTests()
    {
        _colegio = Colegio.Crear("Colegio A", null, null);
        _otroColegio = Colegio.Crear("Colegio B", null, null);
        _colegios.Colegios.AddRange([_colegio, _otroColegio]);
        _luis = Supervisor.Crear(_colegio.Id, "usuario-luis", "Luis", Email.Crear("luis@colegio.com"));
        _supervisores.Supervisores.Add(_luis);
    }

    private EditarSupervisorUseCase Editar() => new(_supervisores, _usuarios);

    private EliminarSupervisorUseCase Eliminar() => new(_supervisores, _usuarios);

    [Fact]
    public async Task EditarSupervisor_SoloNombre_LoGuardaSinTocarLaCuenta()
    {
        await Editar().EjecutarAsync(_colegio.Id, _luis.Id, new DatosSupervisorDto("Luis Gómez", "luis@colegio.com"), CancellationToken.None);

        Assert.Equal("Luis Gómez", _luis.Nombre);
        Assert.Equal(1, _supervisores.Actualizaciones);
        await _usuarios.DidNotReceiveWithAnyArgs().CambiarEmailAsync(default!, default!, default);
    }

    [Fact]
    public async Task EditarSupervisor_NuevoEmail_CambiaTambienElEmailDeSuCuenta()
    {
        await Editar().EjecutarAsync(_colegio.Id, _luis.Id, new DatosSupervisorDto("Luis", " Luis.Gomez@Colegio.com "), CancellationToken.None);

        Assert.Equal("luis.gomez@colegio.com", _luis.Email);
        await _usuarios.Received(1).CambiarEmailAsync("usuario-luis", "luis.gomez@colegio.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditarSupervisor_EmailDeOtraCuenta_NoCambiaNada()
    {
        _usuarios.CambiarEmailAsync(default!, default!, default).ThrowsAsyncForAnyArgs(new DominioException("Ya existe una cuenta con ese email."));

        await Assert.ThrowsAsync<DominioException>(() =>
            Editar().EjecutarAsync(_colegio.Id, _luis.Id, new DatosSupervisorDto("Otro nombre", "ocupado@colegio.com"), CancellationToken.None));

        Assert.Equal("Luis", _luis.Nombre);
        Assert.Equal("luis@colegio.com", _luis.Email);
        Assert.Equal(0, _supervisores.Actualizaciones);
    }

    [Theory]
    [InlineData(" ", "luis@colegio.com")]
    [InlineData("Luis", "no-es-email")]
    public async Task EditarSupervisor_DatosInvalidos_LanzaDominioExceptionSinTocarLaCuenta(string nombre, string email)
    {
        await Assert.ThrowsAsync<DominioException>(() =>
            Editar().EjecutarAsync(_colegio.Id, _luis.Id, new DatosSupervisorDto(nombre, email), CancellationToken.None));

        await _usuarios.DidNotReceiveWithAnyArgs().CambiarEmailAsync(default!, default!, default);
    }

    [Fact]
    public async Task EditarSupervisor_DeOtroColegio_LanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Editar().EjecutarAsync(_otroColegio.Id, _luis.Id, new DatosSupervisorDto("X", "x@colegio.com"), CancellationToken.None));
    }

    [Fact]
    public async Task EliminarSupervisor_LoQuitaYEliminaSuCuenta()
    {
        await Eliminar().EjecutarAsync(_colegio.Id, _luis.Id, CancellationToken.None);

        Assert.Empty(_supervisores.Supervisores);
        await _usuarios.Received(1).EliminarUsuarioAsync("usuario-luis", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EliminarSupervisor_DeOtroColegio_LanzaNoEncontradoSinEliminarNada()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() => Eliminar().EjecutarAsync(_otroColegio.Id, _luis.Id, CancellationToken.None));

        Assert.Single(_supervisores.Supervisores);
        await _usuarios.DidNotReceiveWithAnyArgs().EliminarUsuarioAsync(default!, default);
    }
}
