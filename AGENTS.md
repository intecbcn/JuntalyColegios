# AGENTS.md — Orquestador de JuntalyColegios

Este archivo es el **orquestador** del proyecto. Cualquier agente (o persona) que trabaje aquí debe leer este documento primero, entender la tarea, y decidir qué subagente(s) de la carpeta [`.agents/`](./.agents/) aplican antes de escribir código.

## 1. Resumen del proyecto

- **Stack**: ASP.NET Core MVC, .NET 10 (`net10.0`), C# con `Nullable` e `ImplicitUsings` habilitados.
- **Arquitectura**: **Hexagonal (puertos y adaptadores)**, con 4 proyectos .NET separados que fuerzan la dirección de dependencia `Web → Infrastructure → Application → Domain`. Detalle completo en [`.agents/arquitectura.md`](./.agents/arquitectura.md).
  - `JuntalyColegios.Domain/` — entidades, value objects, excepciones de dominio. Sin dependencias externas.
  - `JuntalyColegios.Application/` — casos de uso y puertos (`Ports/In`, `Ports/Out`), DTOs. Depende solo de Domain.
  - `JuntalyColegios.Infrastructure/` — adaptadores: `ApplicationDbContext`, migraciones EF Core, repositorios. Implementa los puertos de Application.
  - `JuntalyColegios/` — Web (MVC): Controllers, Views, `Program.cs` (composition root de DI).
- **Principios SOLID**: obligatorios en todo el código, en las 4 capas. Ver checklist en [`.agents/arquitectura.md`](./.agents/arquitectura.md#principios-solid-obligatorios-en-todo-el-código-todas-las-capas). Auditados en **cada** cambio por [`.agents/auditoria.md`](./.agents/auditoria.md), no solo en features grandes.
- **Autenticación**: ASP.NET Core Identity (`AddDefaultIdentity<IdentityUser>`, `RequireConfirmedAccount = true`).
- **Datos**: Entity Framework Core + PostgreSQL (proveedor Npgsql; cadena de conexión vía `.env`/variables de entorno, ver `.env.example`). Contexto en `JuntalyColegios.Infrastructure/Persistence/ApplicationDbContext.cs`, migraciones en `Persistence/Migrations/`.
- **Vistas**: Razor Pages/MVC Views (`JuntalyColegios/Views/`, `JuntalyColegios/Areas/Identity/Pages/`).
- **Estáticos**: `JuntalyColegios/wwwroot/` (Bootstrap 5, jQuery, jquery-validation vendorizados en `wwwroot/lib/`).
- **Tests**: proyecto `JuntalyColegios.Tests/` (xUnit) con carpetas `Application/` (unitarios), `Web/` (controladores), `Integration/` (`WebApplicationFactory`) y `E2E/` (Playwright .NET, Chromium headless contra Kestrel real). **Desarrollo guiado por tests (TDD) obligatorio**: ver §6 y [`.agents/testing.md`](./.agents/testing.md).

### Comandos útiles
```bash
dotnet build JuntalyColegios.slnx
dotnet run --project JuntalyColegios
dotnet test JuntalyColegios.slnx                            # toda la suite (incluye E2E)
dotnet test JuntalyColegios.slnx --filter "Category!=E2E"   # rápida, sin navegador
dotnet ef migrations add <Nombre> --project JuntalyColegios.Infrastructure --startup-project JuntalyColegios
dotnet ef database update --project JuntalyColegios.Infrastructure --startup-project JuntalyColegios
```

## 2. Rol del orquestador

Ante cualquier tarea, el orquestador:
1. Analiza qué pide el usuario y a qué capa(s) del proyecto afecta.
2. Descompone la tarea en pasos, decidiendo qué subagente(s) de `.agents/` intervienen y en qué orden.
3. Delega el detalle de implementación a cada subagente — el orquestador no reemplaza el trabajo de un especialista, lo coordina.
4. Integra los entregables de cada subagente y valida que el resultado final sea coherente.

## 3. Subagentes disponibles

| Subagente | Ubicación | Cuándo usarlo | Cubre |
|---|---|---|---|
| **Arquitectura** | [`.agents/arquitectura.md`](./.agents/arquitectura.md) | Antes de features nuevas o cambios de modelo de datos; decisiones de capa hexagonal/puertos/dependencias | Regla de dependencia hexagonal (Domain/Application/Infrastructure/Web), modelado de dominio, puertos, principios SOLID, nuevas dependencias NuGet |
| **Backend** | [`.agents/backend.md`](./.agents/backend.md) | Controladores, casos de uso, modelos, acceso a datos, migraciones, `Program.cs` | `JuntalyColegios/Controllers/`, `Application/UseCases/`, `Domain/Entities/`, `Infrastructure/Persistence/`, `Program.cs` |
| **Frontend** | [`.agents/frontend.md`](./.agents/frontend.md) | Vistas Razor, layout, estilos, validación cliente | `Views/`, `Areas/Identity/Pages/` (presentación), `wwwroot/css`, `wwwroot/js` |
| **Seguridad** | [`.agents/seguridad.md`](./.agents/seguridad.md) | Auth/Identity, autorización, secretos, vulnerabilidades (OWASP) | `Areas/Identity/` (lógica), manejo de secretos, revisión transversal de autorización/XSS/SQLi |
| **Testing** | [`.agents/testing.md`](./.agents/testing.md) | **Primero en cada feature o bug** (TDD: escribe los tests que fallan antes de implementar) y al final para confirmar el verde; cuando se pide cobertura | `JuntalyColegios.Tests/` (xUnit): unitarios, integración con `WebApplicationFactory` y E2E con Playwright |
| **Auditoría** | [`.agents/auditoria.md`](./.agents/auditoria.md) | **En cada cambio**, sin excepción, antes de darlo por cerrado o de hacer commit | Build, tests, cumplimiento de arquitectura hexagonal y SOLID, migraciones, cierre de hallazgos de los demás subagentes |

## 4. Reglas de enrutamiento

Palabras/señales en la tarea → subagente:

- "vista", "layout", "botón", "estilo", "responsive", "formulario (maquetado)" → **Frontend**
- "controlador", "modelo", "migración", "base de datos", "endpoint", "servicio" → **Backend**
- "estructura", "cómo organizar", "nueva dependencia", "modelar entidad", "capas", "puerto", "adaptador", "SOLID" → **Arquitectura**
- "login", "registro", "rol", "permiso", "autorización", "secreto", "contraseña", "vulnerabilidad" → **Seguridad**
- "test", "tests", "cobertura", "unitario", "integración", "xUnit", "E2E", "Playwright" → **Testing**
- "revisar antes de commit", "¿está listo?", "chequeo final" → **Auditoría**

### Flujos combinados típicos

- **Feature nueva** (ej. módulo de colegios): Arquitectura → **Testing (tests en rojo)** → Backend → Frontend → Seguridad → **Testing (verde)** → **Auditoría**.
- **Cambio de modelo/BD**: Arquitectura → **Testing (rojo)** → Backend (migración) → **Testing (verde)** → **Auditoría**.
- **Bug**: **Testing (test de regresión que reproduce el bug, en rojo)** → Backend o Frontend según la capa afectada → **Testing (verde)** → **Auditoría**.
- **Cualquier cambio en autenticación, roles o secretos**: siempre pasa por Seguridad, sin excepción, antes de Auditoría.
- Testing corre **antes** de Backend/Frontend (escribe los tests y comprueba que fallan por el motivo esperado) y **después** (confirma que pasan); Auditoría verifica que `dotnet test` quede en verde.
- **Regla fija, sin excepción**: **todo flujo termina en Auditoría**, incluso cambios pequeños de un solo archivo — es quien verifica que se respetó la arquitectura hexagonal (regla de dependencia entre capas) y los principios SOLID en el código entregado.

## 5. Cómo invocar un subagente

1. Leer el archivo `.agents/<nombre>.md` completo — define rol, alcance, checklist y qué queda fuera de su alcance (con enlaces a los demás subagentes).
2. Adoptar ese rol para la porción de la tarea que le corresponde.
3. Si la herramienta con la que se trabaja soporta subagentes reales (p. ej. la herramienta `Agent` de Claude Code), se puede pasar el contenido del archivo como instrucciones del subagente junto con la tarea concreta — cada archivo ya incluye frontmatter `name`/`description` pensado para eso.
4. El subagente entrega su resultado en el formato descrito en su sección "Entregable al orquestador"; el orquestador lo integra y decide el siguiente paso.

## 6. Convenciones globales (aplican a todos los subagentes)

- **Arquitectura hexagonal siempre**: ninguna pieza nueva se escribe sin decidir primero en qué capa vive (Domain/Application/Infrastructure/Web) y respetando la regla de dependencia `Web → Infrastructure → Application → Domain`.
- **Principios SOLID siempre**, en toda capa y todo lenguaje del proyecto (C#, no solo backend). Ver detalle en [`.agents/arquitectura.md`](./.agents/arquitectura.md#principios-solid-obligatorios-en-todo-el-código-todas-las-capas).
- **TDD en cada feature o bug, sin excepción**:
  1. **Rojo**: escribir primero los tests de lo pedido (unitarios, integración y, si hay interacción en pantalla, E2E) y ejecutar `dotnet test` para comprobar que **fallan por el motivo esperado** (no por un error de compilación o de configuración).
  2. **Verde**: implementar lo mínimo para que esos tests pasen, sin tocar los tests para "hacerlos pasar".
  3. **Refactor**: limpiar manteniendo la suite completa en verde.
  Al reportar la tarea, indicar el resultado del rojo y del verde.
- **Auditoría en cada cambio**: ningún cambio se considera terminado sin pasar por [`.agents/auditoria.md`](./.agents/auditoria.md), que verifica build, tests y cumplimiento de arquitectura/SOLID.
- Idioma español en UI, mensajes de commit y comentarios orientados al usuario.
- `Nullable` habilitado: no introducir warnings de nulabilidad ni suprimirlos sin justificar.
- No commitear secretos ni cadenas de conexión reales; usar User Secrets (`UserSecretsId` ya está configurado en `JuntalyColegios/JuntalyColegios.csproj`) o variables de entorno.
- No modificar a mano archivos dentro de `wwwroot/lib/` (librerías vendorizadas).
- Reutilizar patrones existentes (Identity, puertos ya definidos en `Application/Ports`, Tag Helpers) antes de introducir uno nuevo.
- **Seguimiento de requisitos**: al completar (o avanzar) cualquier punto listado en [`REQUISITOS.md`](./REQUISITOS.md) —documentación, despliegue, slides, vídeo o funcionalidades—, marcarlo y añadir una línea al "Registro de cambios" de ese archivo.

## 7. Formato de entrega esperado de cada subagente

Cada subagente, al terminar, debe reportar al orquestador:
- **Resumen** de lo que hizo.
- **Archivos tocados** (creados/modificados).
- **Riesgos u observaciones** para el siguiente paso del flujo.
- **Siguiente subagente sugerido**, si corresponde.

El orquestador cierra la tarea recién cuando **Auditoría** da el veredicto de "listo".
