using JuntalyColegios.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace JuntalyColegios.Tests.Integration;

/// <summary>
/// Regresión del despliegue: sin cadena de conexión la app caía con un error críptico de Npgsql
/// ("The ConnectionString property has not been initialized") al crear los roles.
/// </summary>
public class ConfiguracionArranqueTests
{
    private sealed class SinCadenaDeConexion : JuntalyWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("ConnectionStrings:DefaultConnection", "");
        }
    }

    [Fact]
    public void Arranque_CadenaDeConexionVacia_FallaIndicandoLaVariableQueFalta()
    {
        using var factory = new SinCadenaDeConexion();

        var ex = Assert.ThrowsAny<Exception>(() => factory.Services);

        Assert.Contains("ConnectionStrings__DefaultConnection", ex.ToString());
    }
}
