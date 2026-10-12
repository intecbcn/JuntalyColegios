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
| Base de datos | PostgreSQL |
| Email | SMTP (`System.Net.Mail`) detrás de un puerto `IEmailSender` |
| Frontend | Razor Views, Bootstrap 5, jQuery, jQuery Validation, CSS/JS propios |
| Configuración | `appsettings.json` + User Secrets / variables de entorno / `.env` (DotNetEnv) |
| Tests | xUnit, NSubstitute, `WebApplicationFactory` (integración) y Playwright .NET (E2E en Chromium headless) |
| Herramientas | `dotnet-ef` (tool local, ver `.config/dotnet-tools.json`) |

## 3. Instalación y ejecución

### Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Una base de datos **PostgreSQL** vacía (14 o superior). Por ejemplo, instalando [PostgreSQL](https://www.postgresql.org/download/) o con Docker:
  ```bash
  docker run -d --name juntaly-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=juntaly_colegios -p 5432:5432 postgres:16
  ```
- (Opcional) Una cuenta SMTP. **No hace falta para probar en local**: sin SMTP, en desarrollo los emails (invitaciones, recuperación de contraseña) se escriben en la consola.

### Pasos

```bash
# 1. Clonar el repositorio
git clone https://github.com/intecbcn/JuntalyColegios.git
cd JuntalyColegios

# 2. Restaurar la herramienta dotnet-ef (manifiesto en .config/) y las dependencias
dotnet tool restore
dotnet restore JuntalyColegios.slnx

# 3. Configurar: copiar el ejemplo y poner la cadena de conexión de tu PostgreSQL
cp .env.example .env
#    → en .env, ConnectionStrings__DefaultConnection (con el Docker de arriba:
#      Host=localhost;Port=5432;Database=juntaly_colegios;Username=postgres;Password=postgres)

# 4. Crear las tablas
dotnet ef database update --project JuntalyColegios.Infrastructure --startup-project JuntalyColegios

# 5. Ejecutar
dotnet run --project JuntalyColegios --launch-profile https
```

La aplicación queda en **https://localhost:7158** (perfil `https` de `JuntalyColegios/Properties/launchSettings.json`). Al arrancar crea los roles, el administrador y, con la configuración de `.env.example`, el **colegio de demostración**.

### Probar en local

Con el `.env` copiado de `.env.example` (solo cambiando la cadena de conexión):

| Perfil | Usuario | Contraseña | Entra en |
|---|---|---|---|
| Administrador | `admin@ejemplo.com` | `Admin123!` | `/Admin` |
| Supervisor del colegio de demo | `supervisor@demo.juntaly.com` | `Demo123!` | `/Colegio` |
| Padre / representante de demo | `padre@demo.juntaly.com` | `Demo123!` | `/Padres` |

- **Pantalla de salida y simulador**: como supervisor, *Pantalla de salida* → elegir `AB123CD` → *Simular llegada* (ver el [recorrido sugerido](#7-usuario-y-contraseña-de-prueba)).
- **Pantallas de llamada**: como supervisor, *Pantallas de llamada* → *Crear y ver enlace* → abrir el enlace en otra ventana o en el móvil.
- **Altas con invitación** (supervisores, padres): sin SMTP, el email con el enlace para establecer la contraseña aparece en la consola donde corre `dotnet run`.
- **API de la antena** (clave `Demo__ClaveAntena` del `.env.example`):
  ```bash
  curl -k -X POST https://localhost:7158/api/antena/detecciones \
       -H "X-Antena-Clave: clave-antena-demo-local" -H "Content-Type: application/json" \
       -d '{"tag":"E20000172211014418900001"}'
  ```

### Variables de configuración

| Variable | Obligatoria | Uso |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Sí | Cadena de conexión de PostgreSQL |
| `AdminInicial__Email` / `AdminInicial__Password` | Recomendada | Administrador de la plataforma (rol AdminApp), se crea al arrancar si no existe |
| `Demo__Sembrar` | No | `true` crea el colegio de demostración con las cuentas de prueba |
| `Demo__Contrasena` | Si `Demo__Sembrar` | Contraseña de las cuentas de prueba |
| `Demo__ClaveAntena` | No | Clave de antena del colegio de demo, para probar la API de la antena |
| `Smtp__Host`, `Smtp__Port`, `Smtp__User`, `Smtp__Password`, `Smtp__From` | En un servidor | Envío de invitaciones, recuperación de contraseña y solicitud de demo (en desarrollo, sin SMTP, se escriben en la consola) |
| `AllowedHosts` | En un servidor | Dominio público de la app (además de `localhost`), para rechazar peticiones con otro `Host` |
| `App__UrlBase` | En un servidor | URL pública con la que se construyen los enlaces de los emails (en desarrollo, `https://localhost:7158`) |
| `Antena__PeticionesPorMinuto` | No | Lecturas por minuto permitidas a cada antena (por defecto 120); al superarlas la API responde 429 |

> **Secretos**: el archivo `.env` y los User Secrets **no** se suben al repositorio.

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
| **Supervisor del colegio** (`/Colegio`) | Clases y alumnos; padres y su vínculo con los hijos; vehículos y asignación del tag RFID; **franjas horarias de retiro** por día de la semana (para todo el colegio o por clase); **pantalla de salida**; **pantallas de llamada** para portería y aulas; registro de detecciones |
| **Padre / representante** (`/Padres`) | Ver a sus hijos y su clase; registrar sus vehículos (el tag lo asigna el colegio); autorizar a otras personas a recoger; historial de recogidas |

### Flujo de la salida escolar (RFID)

1. La antena del colegio lee el tag de un vehículo y lo envía a `POST /api/antena/detecciones` con la clave del colegio (`X-Antena-Clave`).
2. Se identifica el vehículo y la familia, y se llama **solo a los hijos que están en su franja de retiro** (en la hora local del colegio). Fuera de franja, con un tag desconocido o si el vehículo ya se detectó en los últimos 5 minutos, no se llama a nadie; toda lectura queda en el registro de detecciones.
3. La **pantalla de salida** se actualiza sola y muestra cada alumno llamado con su clase, el vehículo y las personas autorizadas de la familia; el personal pulsa «Entregado» al entregarlo.
4. La pantalla incluye un **simulador de llegada** para probar el flujo sin antena.
5. **Pantallas de llamada** estilo turnero para una TV en la portería o una tablet en cada aula: se abren con un enlace secreto que genera el supervisor (sin iniciar sesión, revocable), muestran en grande el último alumno llamado y los que esperan (solo nombre, clase y placa), suenan al llegar uno nuevo y se adaptan a TV, tablet y móvil.

### Seguridad

- Cada supervisor solo accede a los datos de su colegio y cada padre a los de su familia (lo ajeno responde 404).
- La clave de la antena se guarda solo como hash y se muestra una única vez al generarla; cada antena tiene un límite de lecturas por minuto (429 con `Retry-After` al superarlo).
- Antiforgery en todos los formularios, emails con contenido escapado y hosts permitidos restringidos para no envenenar los enlaces de recuperación.
- Todas las tablas tienen RLS y sin permisos para los roles públicos que algunos servicios de PostgreSQL gestionado exponen por API REST.

## 6. Tests automatizados

El proyecto se desarrolla con **TDD**: cada funcionalidad o corrección empieza escribiendo sus tests, que se ejecutan para comprobar que fallan, y solo después se implementa el código hasta que pasan. La suite tiene **407 tests**.

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

En https://colegios.juntaly.com (y en local, ver [Probar en local](#probar-en-local)) hay un colegio de demostración («U. E. Colegio Demo Juntaly») con clases, familias, vehículos con tag e historial:

| Perfil | Usuario | Contraseña |
|---|---|---|
| Supervisor del colegio | `supervisor@demo.juntaly.com` | `Demo123!` |
| Padre / representante | `padre@demo.juntaly.com` | `Demo123!` |

Recorrido sugerido:

1. Entrar como **supervisor** → **Pantalla de salida** → en el simulador elegir `AB123CD · Andrea Martínez` → **Simular llegada**. Se llama a *Sofía Martínez* (franja general de todo el día); su hermano *Diego* (5.º Año, franja propia de 13:00 a 14:00 entre semana) solo se llama dentro de su franja.
2. Pulsar **Entregado**, y revisar **Franjas de retiro** y **Registro de detecciones**.
3. Entrar como **padre** → ver a sus hijos, el vehículo con su tag, la persona autorizada y el historial de recogidas.

## 8. Despliegue

- **URL**: [https://colegios.juntaly.com](https://colegios.juntaly.com)

La aplicación es una app ASP.NET Core estándar: se puede publicar en cualquier servidor con el [runtime de ASP.NET Core 10](https://dotnet.microsoft.com/download/dotnet/10.0) y una base PostgreSQL.

```bash
# 1. Aplicar las migraciones pendientes a la base de datos del servidor
dotnet ef database update --project JuntalyColegios.Infrastructure --startup-project JuntalyColegios

# 2. Generar el paquete
dotnet publish JuntalyColegios/JuntalyColegios.csproj -c Release -o publicacion
```

3. Copiar el contenido de `publicacion/` al servidor y arrancarlo.
4. Definir la configuración del servidor (ver la [tabla de variables](#variables-de-configuración)) como variables de entorno o en un archivo `.env` junto a `JuntalyColegios.dll`; `dotnet publish` no lo incluye ni lo sobrescribe. Además de la cadena de conexión: `ASPNETCORE_ENVIRONMENT=Production`, `AllowedHosts`, `App__UrlBase`, `AdminInicial__*` y `Smtp__*`.

Notas:

- La app necesita la base de datos al arrancar (crea roles, administrador y datos de demo). Si falta la cadena de conexión no arranca y el log indica qué variable definir.
- Las claves de Data Protection (sesiones, antiforgery, enlaces de invitación) se guardan en la base de datos: sobreviven a los reinicios de la aplicación.
- Toda migración que crea tablas activa RLS y retira permisos a los roles públicos de la API REST que exponen algunos servicios de PostgreSQL gestionado.

## 9. Presentación y vídeo

- **Slides**: _pendiente_
- **Vídeo explicativo**: _pendiente_
