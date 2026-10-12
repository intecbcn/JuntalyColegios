using System.Text.Encodings.Web;
using System.Text.Unicode;
using JuntalyColegios.Configuracion;
using JuntalyColegios.Infrastructure.Demo;
using JuntalyColegios.Infrastructure.Identity;
using JuntalyColegios.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;

// Carga variables de entorno desde .env (credenciales de Supabase) antes de construir la configuración.
// Busca .env subiendo desde el directorio actual hasta encontrarlo (p.ej. la raíz del repo),
// ya que el working directory difiere entre "dotnet run" y "dotnet ef". No se commitea (ver .gitignore);
// en producción las variables se definen en el entorno real y no requieren este archivo.
try
{
    DotNetEnv.Env.TraversePath().Load();
}
catch (FileNotFoundException)
{
    // No hay .env (p.ej. en producción las variables se definen directamente en el entorno).
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
// Razor escapa por defecto todo lo que no es ASCII (á → &#xE1;). Se dejan legibles los caracteres
// Unicode del español; lo peligroso en HTML (<, >, &, comillas) se sigue escapando siempre.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// Puertos de Application con sus adaptadores de Infrastructure y casos de uso (ver RegistroDeServicios).
builder.Services.AddJuntalyColegios();

var app = builder.Build();

// Roles de la aplicación y administrador inicial (idempotente; ver InicializadorIdentidad).
await InicializadorIdentidad.InicializarAsync(app.Services);

// Datos de demostración con cuentas de prueba (Demo__Sembrar=true). Idempotente.
if (app.Configuration.GetValue<bool>("Demo:Sembrar"))
{
    var contrasenaDemo = app.Configuration["Demo:Contrasena"]
        ?? throw new InvalidOperationException("Demo:Sembrar está activo pero falta Demo:Contrasena.");
    await SembradorDemo.SembrarAsync(app.Services, new OpcionesDemo(contrasenaDemo, app.Configuration["Demo:ClaveAntena"]));
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Inicio}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
