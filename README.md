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
- (Opcional) Una cuenta SMTP para el envío de emails (invitaciones, recuperación de contraseña, solicitud de demo)

### Pasos

```bash
# 1. Clonar el repositorio
git clone https://github.com/intecbcn/JuntalyColegios.git
cd JuntalyColegios

# 2. Restaurar herramientas locales (dotnet-ef) y dependencias
dotnet tool restore --tool-manifest JuntalyColegios/dotnet-tools.json
dotnet restore JuntalyColegios.slnx

# 3. Configurar la base de datos, el administrador inicial y (opcional) los datos de demostración
cp .env.example .env
#    → editar .env (ver la tabla de variables más abajo)

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

La aplicación queda disponible en la URL que indique la consola (ver `JuntalyColegios/Properties/launchSettings.json`). Al arrancar crea los roles, el administrador inicial y, si se activan, los datos de demostración.

### Variables de configuración

| Variable | Obligatoria | Uso |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Sí | Cadena de conexión de PostgreSQL |
| `AdminInicial__Email` / `AdminInicial__Password` | Recomendada | Administrador de la plataforma (rol AdminApp), se crea al arrancar si no existe |
| `App__UrlBase` | En producción | URL pública con la que se construyen los enlaces de los emails (por defecto `https://colegios.juntaly.com`) |
| `Smtp__Host`, `Smtp__Port`, `Smtp__User`, `Smtp__Password`, `Smtp__From` | Para enviar emails | Invitaciones, recuperación de contraseña y solicitud de demo |
| `Demo__Sembrar` | No | `true` crea el colegio de demostración con las cuentas de prueba |
| `Demo__Contrasena` | Si `Demo__Sembrar` | Contraseña de las cuentas de prueba |
| `Demo__ClaveAntena` | No | Clave de antena del colegio de demo, para probar la API de la antena |
| `Antena__PeticionesPorMinuto` | No | Lecturas por minuto permitidas a cada antena (por defecto 120); al superarlas la API responde 429 |

> **Secretos**: el archivo `.env` y los User Secrets **no** se suben al repositorio. En producción las variables se definen en el entorno del servidor (ver [Despliegue](#8-despliegue)).

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
├── JuntalyColegios.Domain/            # Núcleo, sin dependencias
│   ├── Entities/                      #   Colegio, Clase, Alumno, Representante, Vehiculo, PersonaAutorizada,
│   │                                  #   FranjaRetiro, Deteccion, Recogida, Supervisor
│   ├── ValueObjects/                  #   Email, Placa, TagRfid
│   └── Servicios/                     #   HorarioDeRetiro (¿está el alumno en su franja?)
├── JuntalyColegios.Application/       # Casos de uso y puertos
│   ├── Ports/In/                      #   un puerto de entrada por operación de negocio
│   ├── Ports/Out/                     #   repositorios, servicio de usuarios, email
│   ├── UseCases/                      #   Colegios (admin), Supervision, Padres, Salida (RFID), Historial
│   └── DTOs/
├── JuntalyColegios.Infrastructure/    # Adaptadores
│   ├── Persistence/                   #   ApplicationDbContext, configuraciones, repositorios y Migrations/
│   ├── Identity/                      #   roles, administrador inicial y alta de usuarios con invitación
│   ├── Email/                         #   SmtpEmailSender
│   └── Demo/                          #   datos de demostración
├── JuntalyColegios/                   # Web (ASP.NET Core MVC) — composition root
│   ├── Program.cs, Configuracion/     #   arranque e inyección de dependencias
│   ├── Controllers/                   #   landing, /Panel (redirección por rol) y API de la antena
│   ├── Areas/Admin/                   #   administrador: colegios, supervisores y clave de antena
│   ├── Areas/Colegio/                 #   supervisor: clases, alumnos, padres, tags, franjas, salida, detecciones
│   ├── Areas/Padres/                  #   panel de la familia
│   ├── Areas/Identity/Pages/          #   login y gestión de cuenta (registro público cerrado)
│   └── wwwroot/                       #   CSS, JS (salida.js: refresco de la pantalla de salida) e imágenes
├── JuntalyColegios.Tests/             # Tests (xUnit): Domain/, Application/, Web/, Integration/, E2E/ (Playwright)
├── .agents/                           # Guías de trabajo por especialidad (arquitectura, backend, ...)
├── AGENTS.md                          # Guía general de desarrollo del proyecto
└── REQUISITOS.md                      # Checklist de entrega y progreso
```

Cada capa tiene su propio README con más detalle: [Domain](JuntalyColegios.Domain/README.md) · [Application](JuntalyColegios.Application/README.md) · [Infrastructure](JuntalyColegios.Infrastructure/README.md).

## 5. Funcionalidades principales

La aplicación tiene tres perfiles y las cuentas se crean **en cascada** (no hay registro público): el administrador da de alta colegios y supervisores, y cada supervisor da de alta a los padres de su colegio. Cada cuenta nueva recibe un email para establecer su contraseña.

| Perfil | Qué puede hacer |
|---|---|
| **Administrador** (`/Admin`) | Crear, editar y desactivar colegios (con su zona horaria); dar de alta supervisores; generar la clave de la antena RFID de cada colegio |
| **Supervisor del colegio** (`/Colegio`) | Clases y alumnos; padres y su vínculo con los hijos; vehículos y asignación del tag RFID; **franjas horarias de retiro** por día de la semana (para todo el colegio o por clase); **pantalla de salida**; registro de detecciones |
| **Padre / representante** (`/Padres`) | Ver a sus hijos y su clase; registrar sus vehículos (el tag lo asigna el colegio); autorizar a otras personas a recoger; historial de recogidas |

### Flujo de la salida escolar (RFID)

1. La antena del colegio lee el tag de un vehículo y lo envía a `POST /api/antena/detecciones` con la clave del colegio (`X-Antena-Clave`).
2. Se identifica el vehículo y la familia, y se llama **solo a los hijos que están en su franja de retiro** (en la hora local del colegio). Fuera de franja, con un tag desconocido o si el vehículo ya se detectó en los últimos 5 minutos, no se llama a nadie; toda lectura queda en el registro de detecciones.
3. La **pantalla de salida** se actualiza sola y muestra cada alumno llamado con su clase, el vehículo y las personas autorizadas de la familia; el personal pulsa «Entregado» al entregarlo.
4. La pantalla incluye un **simulador de llegada** para probar el flujo sin antena.

### Seguridad

- Cada supervisor solo accede a los datos de su colegio y cada padre a los de su familia (lo ajeno responde 404).
- La clave de la antena se guarda solo como hash y se muestra una única vez al generarla; cada antena tiene un límite de lecturas por minuto (429 con `Retry-After` al superarlo).
- Antiforgery en todos los formularios, emails con contenido escapado y hosts permitidos restringidos para no envenenar los enlaces de recuperación.
- En Supabase todas las tablas tienen RLS y sin permisos para los roles públicos de su API REST.

## 6. Tests automatizados

El proyecto se desarrolla con **TDD**: cada funcionalidad o corrección empieza escribiendo sus tests, que se ejecutan para comprobar que fallan, y solo después se implementa el código hasta que pasan. La suite tiene **358 tests**.

Todos los tests viven en `JuntalyColegios.Tests/`:

| Carpeta | Tipo | Herramientas | Qué verifica |
|---|---|---|---|
| `Domain/` | Unitarios | xUnit | Reglas de las entidades: validaciones, franjas y su prioridad, tags, recogidas |
| `Application/` | Unitarios | xUnit + repositorios en memoria + NSubstitute | Casos de uso, aislamiento entre colegios y familias, regla de llamada a la salida (con un reloj controlado) |
| `Web/` | Unitarios | xUnit + NSubstitute | Controladores: validación, errores de negocio, respuestas de la API |
| `Integration/` | Integración | `WebApplicationFactory` + SQLite en memoria | Pipeline HTTP real: roles y accesos, antiforgery, API de la antena, invitaciones por email, datos de demo |
| `E2E/` | Extremo a extremo | Playwright .NET (Chromium headless) | Recorridos en navegador de cada perfil, incluida la actualización automática de la pantalla de salida |

Los tests **no usan servicios reales**: el SMTP se sustituye por un doble en memoria y PostgreSQL por SQLite en memoria. No necesitan `.env` ni configuración previa; la primera ejecución de los E2E descarga Chromium automáticamente.

```bash
dotnet test JuntalyColegios.slnx                            # toda la suite
dotnet test JuntalyColegios.slnx --filter "Category!=E2E"   # solo unitarios + integración (rápida)
dotnet test JuntalyColegios.slnx --filter "Category=E2E"    # solo E2E con navegador
```

## 7. Usuario y contraseña de prueba

En https://colegios.juntaly.com hay un colegio de demostración («U. E. Colegio Demo Juntaly») con clases, familias, vehículos con tag e historial:

| Perfil | Usuario | Contraseña |
|---|---|---|
| Supervisor del colegio | `supervisor@demo.juntaly.com` | `Demo123!` |
| Padre / representante | `padre@demo.juntaly.com` | `Demo123!` |

Recorrido sugerido:

1. Entrar como **supervisor** → **Pantalla de salida** → en el simulador elegir `AB123CD · Andrea Martínez` → **Simular llegada**. Se llama a *Sofía Martínez* (franja general de todo el día); su hermano *Diego* (5.º Año, franja propia de 13:00 a 14:00 entre semana) solo se llama dentro de su franja.
2. Pulsar **Entregado**, y revisar **Franjas de retiro** y **Registro de detecciones**.
3. Entrar como **padre** → ver a sus hijos, el vehículo con su tag, la persona autorizada y el historial de recogidas.

## 8. Despliegue

- **URL de producción**: [https://colegios.juntaly.com](https://colegios.juntaly.com)
- **Servidor**: Windows con **IIS** y el [ASP.NET Core Hosting Bundle de .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0), detrás de **Cloudflare**.
- **Base de datos**: PostgreSQL en **Supabase** (RLS activo en todas las tablas).

### Publicar una versión

```bash
# 1. Aplicar las migraciones pendientes a la base de producción
dotnet ef database update --project JuntalyColegios.Infrastructure --startup-project JuntalyColegios

# 2. Generar el paquete
dotnet publish JuntalyColegios/JuntalyColegios.csproj -c Release -o publicacion
```

3. Detener el sitio (o dejar un `app_offline.htm`), copiar el contenido de `publicacion/` a la carpeta del sitio en el servidor y volver a arrancarlo.
4. La primera vez, definir las variables de entorno del sitio (tabla de la [sección 3](#variables-de-configuración)) en el `web.config` del servidor, dentro de `<aspNetCore>`, **sin subirlo al repositorio**:

```xml
<aspNetCore processPath="dotnet" arguments=".\JuntalyColegios.dll" stdoutLogEnabled="false" hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="ConnectionStrings__DefaultConnection" value="Host=...;Password=..." />
    <environmentVariable name="AdminInicial__Email" value="..." />
    <environmentVariable name="AdminInicial__Password" value="..." />
    <environmentVariable name="Smtp__Host" value="..." />
    <!-- Smtp__Port, Smtp__User, Smtp__Password, Smtp__From -->
    <environmentVariable name="Demo__Sembrar" value="true" />
    <environmentVariable name="Demo__Contrasena" value="Demo123!" />
  </environmentVariables>
</aspNetCore>
```

> Al publicar, `dotnet publish` genera un `web.config` nuevo: conservar el bloque `<environmentVariables>` del servidor (o definir las variables en el *Configuration Editor* de IIS para el sitio).
>
> Alternativa: copiar un archivo `.env` (mismo formato que `.env.example`) a la carpeta del sitio, junto a `JuntalyColegios.dll`. La app lo carga al arrancar y no se sobrescribe al publicar. Si falta la cadena de conexión, la app no arranca y el log indica qué variable definir.

Notas:

- Las claves de Data Protection (sesiones, antiforgery, enlaces de invitación) se guardan en la base de datos, así que sobreviven a los reciclajes del app pool.
- Si Cloudflare o IIS presentan la app con otro nombre de host, añadirlo a `AllowedHosts` en `appsettings.json`.
- La app necesita la base de datos al arrancar (crea roles, administrador y datos de demo): si Supabase pausa el proyecto por inactividad, hay que restaurarlo desde su panel.

## 9. Presentación y vídeo

- **Slides**: _pendiente_
- **Vídeo explicativo**: _pendiente_
