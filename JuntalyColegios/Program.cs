using System.Text.Encodings.Web;
using System.Text.Unicode;
using JuntalyColegios.Configuracion;
using JuntalyColegios.Infrastructure.Demo;
using JuntalyColegios.Infrastructure.Identity;
using JuntalyColegios.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;

// Carga variables de entorno desde .env (credenciales de Supabase) antes de construir la configuración.
// Primero junto a la aplicación (en IIS el directorio de trabajo no es la carpeta del sitio) y si no,
// subiendo desde el directorio actual (la raíz del repo con "dotnet run" o "dotnet ef"). No se commitea
// (ver .gitignore); en producción también pueden definirse en el entorno (web.config) sin este archivo.
var envJuntoALaApp = Path.Combine(AppContext.BaseDirectory, ".env");
try
{
    if (File.Exists(envJuntoALaApp)) DotNetEnv.Env.Load(envJuntoALaApp);
    else DotNetEnv.Env.TraversePath().Load();
}
catch (FileNotFoundException)
{
    // No hay .env: las variables vienen del entorno.
}

var builder = WebApplication.CreateBuilder(args);

// Sin cadena de conexión la app no puede arrancar (crea roles y datos al iniciar): se falla con un mensaje claro.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Falta la cadena de conexión: define la variable de entorno ConnectionStrings__DefaultConnection " +
        "(en IIS, en el web.config del sitio) o un archivo .env junto a la aplicación.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
// Claves de Data Protection en la base de datos: las cookies de sesión, el antiforgery y los enlaces de
// invitación siguen siendo válidos tras reiniciar o reciclar el app pool de IIS.
builder.Services.AddDataProtection()
    .SetApplicationName("JuntalyColegios")
    .PersistKeysToDbContext<ApplicationDbContext>();

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
