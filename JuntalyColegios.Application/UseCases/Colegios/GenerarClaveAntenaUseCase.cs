using JuntalyColegios.Application.Ports.In;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Application.UseCases.Salida;

namespace JuntalyColegios.Application.UseCases.Colegios;

/// <summary>Genera una clave de antena nueva (la anterior deja de valer). Solo se guarda su hash.</summary>
public sealed class GenerarClaveAntenaUseCase(IRepositorioColegios colegios) : IGenerarClaveAntenaUseCase
{
    public async Task<string> EjecutarAsync(Guid colegioId, CancellationToken cancellationToken)
    {
        var colegio = await colegios.ObtenerExistenteAsync(colegioId, cancellationToken);
        var clave = ClaveDeAntena.Generar();
        colegio.EstablecerHashClaveAntena(ClaveDeAntena.Hash(clave));
        await colegios.ActualizarAsync(colegio, cancellationToken);
        return clave;
    }
}
