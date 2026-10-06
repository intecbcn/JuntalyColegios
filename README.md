# Juntaly Colegios

**Juntaly Colegios** es un producto de [Juntaly.com](https://juntaly.com) para colegios que agiliza la **hora de salida escolar** mediante detección RFID: una antena reconoce el tag del vehículo a distancia, el alumno aparece en pantalla y el colegio lo prepara mientras el carro avanza en la fila.

🌐 **Web del producto**: [colegios.juntaly.com](https://colegios.juntaly.com)

---

## Índice

1. [Descripción general](#1-descripción-general)
2. [Stack tecnológico](#2-stack-tecnológico)
3. [Instalación y ejecución](#3-instalación-y-ejecución)
4. [Estructura del proyecto](#4-estructura-del-proyecto)
5. [Funcionalidades principales](#5-funcionalidades-principales)
6. [Tests automatizados](#6-tests-automatizados)
7. [Usuario y contraseña de prueba](#7-usuario-y-contraseña-de-prueba)
8. [Despliegue](#8-despliegue)
9. [Presentación y vídeo](#9-presentación-y-vídeo)

---

## 1. Descripción general

En muchos colegios, la salida genera filas de coches, esperas y personal buscando alumnos de aula en aula. Juntaly Colegios resuelve ese momento:

- Cada vehículo autorizado lleva un **tag RFID**.
- Una **antena** en el acceso del colegio detecta el tag antes de que el coche llegue a la puerta.
- El sistema muestra en **pantalla** qué alumno(s) corresponden a ese vehículo, para que el personal los prepare mientras el coche avanza.
- Modelo de precio simple: **1 $ por alumno al mes**.

### Público objetivo

El público de referencia son los **colegios de Venezuela**. El producto nace de una necesidad concreta de esa región: la hora de salida concentra filas de vehículos frente a los colegios, largas esperas y una gestión manual de la entrega de alumnos. Por eso la interfaz está en español y el precio se expresa en dólares (1 $ por alumno al mes).

Esta aplicación web es la plataforma de Juntaly Colegios: landing comercial con solicitud de demo, gestión de cuentas de usuario y, sobre esa base, los módulos de gestión del colegio (ver [estado de funcionalidades](#5-funcionalidades-principales)).

## 2. Stack tecnológico

| Área | Tecnología |
|---|---|
| Lenguaje / runtime | C# · .NET 10 (`net10.0`), `Nullable` habilitado |
| Framework web | ASP.NET Core MVC + Razor Pages |
| Arquitectura | Hexagonal (puertos y adaptadores), principios SOLID |
| Autenticación | ASP.NET Core Identity (con confirmación de cuenta por email) |
| Persistencia | Entity Framework Core 10 + Npgsql |
| Base de datos | PostgreSQL alojado en [Supabase](https://supabase.com) |
| Email | SMTP (`System.Net.Mail`) detrás de un puerto `IEmailSender` |
| Frontend | Razor Views, Bootstrap 5, jQuery, jQuery Validation, CSS/JS propios |
| Configuración | `appsettings.json` + User Secrets / variables de entorno / `.env` (DotNetEnv) |
| Tests | xUnit, NSubstitute, `WebApplicationFactory` (integración) y Playwright .NET (E2E en Chromium headless) |
| Herramientas | `dotnet-ef` (tool local, ver `JuntalyColegios/dotnet-tools.json`) |

## 3. Instalación y ejecución

### Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Una base de datos PostgreSQL (por ejemplo un proyecto gratuito en Supabase)
- (Opcional) Una cuenta SMTP para el envío de emails (solicitud de demo, confirmación de cuenta)

### Pasos

```bash
# 1. Clonar el repositorio
git clone https://github.com/intecbcn/JuntalyColegios.git
cd JuntalyColegios

# 2. Restaurar herramientas locales (dotnet-ef) y dependencias
dotnet tool restore --tool-manifest JuntalyColegios/dotnet-tools.json
dotnet restore JuntalyColegios.slnx

# 3. Configurar la cadena de conexión
cp .env.example .env
#    → editar .env con los datos reales de PostgreSQL/Supabase

# 4. (Opcional) Configurar SMTP con User Secrets
dotnet user-secrets set "Smtp:Host" "smtp.ejemplo.com" --project JuntalyColegios
dotnet user-secrets set "Smtp:Port" "587" --project JuntalyColegios
dotnet user-secrets set "Smtp:User" "usuario" --project JuntalyColegios
dotnet user-secrets set "Smtp:Password" "contraseña" --project JuntalyColegios
dotnet user-secrets set "Smtp:From" "no-reply@ejemplo.com" --project JuntalyColegios

# 5. Aplicar las migraciones a la base de datos
dotnet ef database update --project JuntalyColegios.Infrastructure --startup-project JuntalyColegios

# 6. Ejecutar
dotnet run --project JuntalyColegios
```

La aplicación queda disponible en la URL que indique la consola (ver `JuntalyColegios/Properties/launchSettings.json`).

> **Secretos**: el archivo `.env` y los User Secrets **no** se suben al repositorio. En producción, la cadena de conexión y la configuración SMTP se definen como variables de entorno (`ConnectionStrings__DefaultConnection`, `Smtp__Host`, etc.).

### Comandos útiles

```bash
dotnet build JuntalyColegios.slnx
dotnet test JuntalyColegios.slnx
dotnet ef migrations add <Nombre> --project JuntalyColegios.Infrastructure --startup-project JuntalyColegios
```

## 4. Estructura del proyecto

La aplicación está dividida en **4 proyectos** que fuerzan la regla de dependencia de la arquitectura hexagonal: `Web → Infrastructure → Application → Domain`, más un proyecto de tests.

```
JuntalyColegios.slnx
├── JuntalyColegios.Domain/            # Núcleo: entidades, value objects, excepciones (sin dependencias)
├── JuntalyColegios.Application/       # Casos de uso y puertos
│   ├── Ports/In/                      #   puertos de entrada (ej. ISolicitarDemoUseCase)
│   ├── Ports/Out/                     #   puertos de salida (ej. IEmailSender)
│   ├── UseCases/                      #   implementación de casos de uso (ej. SolicitarDemoUseCase)
│   └── DTOs/                          #   objetos de transferencia (ej. SolicitudDemoDto)
├── JuntalyColegios.Infrastructure/    # Adaptadores
│   ├── Persistence/                   #   ApplicationDbContext (EF Core + Identity) y Migrations/
│   └── Email/                         #   SmtpEmailSender (implementa IEmailSender)
├── JuntalyColegios/                   # Web (ASP.NET Core MVC) — composition root
│   ├── Program.cs                     #   configuración e inyección de dependencias
│   ├── Controllers/                   #   HomeController (landing + solicitud de demo)
│   ├── Models/                        #   modelos de vista / request
│   ├── Views/                         #   vistas Razor (landing, layouts)
│   ├── Areas/Identity/Pages/          #   páginas de cuenta (login, registro, gestión) personalizadas
│   └── wwwroot/                       #   CSS, JS, imágenes y librerías cliente
├── JuntalyColegios.Tests/             # Tests (xUnit): Application/, Web/, Integration/, E2E/ (Playwright)
├── .agents/                           # Guías de trabajo por especialidad (arquitectura, backend, ...)
├── AGENTS.md                          # Guía general de desarrollo del proyecto
└── REQUISITOS.md                      # Checklist de entrega y progreso
```

Cada capa tiene su propio README con más detalle: [Domain](JuntalyColegios.Domain/README.md) · [Application](JuntalyColegios.Application/README.md) · [Infrastructure](JuntalyColegios.Infrastructure/README.md).

## 5. Funcionalidades principales

| Funcionalidad | Estado |
|---|---|
| Landing comercial (cómo funciona, instalación, beneficios, precio, FAQ) | ✅ |
| Formulario "Solicita una demo" con envío por email a ventas y protección anti-bots (honeypot + antiforgery) | ✅ |
| Registro, login, confirmación de email y recuperación de contraseña (Identity, en español) | ✅ |
| Gestión de cuenta (perfil, cambio de contraseña, 2FA, datos personales) | ✅ |
| _Próximos módulos: gestión de alumnos, vehículos/tags RFID y pantalla de salida_ | 🚧 |

## 6. Tests automatizados

El proyecto se desarrolla con **TDD**: cada funcionalidad o corrección empieza escribiendo sus tests, que se ejecutan para comprobar que fallan, y solo después se implementa el código hasta que pasan.

Todos los tests viven en `JuntalyColegios.Tests/`:

| Carpeta | Tipo | Herramientas | Qué verifica |
|---|---|---|---|
| `Application/` | Unitarios | xUnit + dobles de prueba | Casos de uso aislados de infraestructura (ej. contenido y escape HTML del correo de demo) |
| `Web/` | Unitarios | xUnit + NSubstitute | Controladores: validación, honeypot anti-bots, manejo de errores |
| `Integration/` | Integración | `WebApplicationFactory` | Pipeline HTTP real: rutas, vistas, antiforgery, páginas de Identity |
| `E2E/` | Extremo a extremo | Playwright .NET (Chromium headless) | Recorridos en navegador: demo interactiva, calculadora de precio, envío del formulario, navegación al login |

Los tests **no usan servicios reales**: el SMTP se sustituye por un doble en memoria y la base de datos no se toca. No necesitan `.env` ni configuración previa; la primera ejecución de los E2E descarga Chromium automáticamente.

```bash
dotnet test JuntalyColegios.slnx                            # toda la suite
dotnet test JuntalyColegios.slnx --filter "Category!=E2E"   # solo unitarios + integración (rápida)
dotnet test JuntalyColegios.slnx --filter "Category=E2E"    # solo E2E con navegador
```

## 7. Usuario y contraseña de prueba

| Usuario | Contraseña |
|---|---|
| _pendiente_ | _pendiente_ |

## 8. Despliegue

- **URL de producción**: [https://colegios.juntaly.com](https://colegios.juntaly.com)
- _Detalles de la infraestructura de despliegue: pendiente._

## 9. Presentación y vídeo

- **Slides**: _pendiente_
- **Vídeo explicativo**: _pendiente_
