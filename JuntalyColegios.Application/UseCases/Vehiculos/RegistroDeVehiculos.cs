using JuntalyColegios.Application.DTOs;
using JuntalyColegios.Application.Ports.Out;
using JuntalyColegios.Domain.Entities;
using JuntalyColegios.Domain.Exceptions;
using JuntalyColegios.Domain.ValueObjects;

namespace JuntalyColegios.Application.UseCases.Vehiculos;

/// <summary>
/// Alta de un vehículo de un representante, común al supervisor y al propio padre: una misma placa no
/// se registra dos veces para el mismo representante.
/// </summary>
internal static class RegistroDeVehiculos
{
    public static async Task<Guid> RegistrarAsync(
        IRepositorioVehiculos vehiculos, Representante representante, DatosVehiculoDto datos, CancellationToken cancellationToken)
    {
        var vehiculo = Vehiculo.Crear(representante, Placa.Crear(datos.Placa), datos.Descripcion);
        var yaRegistrado = (await vehiculos.ListarPorColegioAsync(representante.ColegioId, cancellationToken))
            .Any(v => v.RepresentanteId == representante.Id && v.Placa == vehiculo.Placa);
        if (yaRegistrado)
            throw new DominioException($"El vehículo {vehiculo.Placa} ya está registrado.");

        await vehiculos.AgregarAsync(vehiculo, cancellationToken);
        return vehiculo.Id;
    }
}
