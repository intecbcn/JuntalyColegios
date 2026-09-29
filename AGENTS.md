# AGENTS.md — Orquestador de JuntalyColegios

Este archivo es el **orquestador** del proyecto. Cualquier agente (o persona) que trabaje aquí debe leer este documento primero, entender la tarea, y decidir qué subagente(s) de la carpeta [`agente/`](./agente/) aplican antes de escribir código.

## 1. Resumen del proyecto

- **Stack**: ASP.NET Core MVC, .NET 10 (`net10.0`), C# con `Nullable` e `ImplicitUsings` habilitados.
- **Solución**: `JuntalyColegios.slnx` → un único proyecto `JuntalyColegios/JuntalyColegios.csproj`.
- **Autenticación**: ASP.NET Core Identity (`AddDefaultIdentity<IdentityUser>`, `RequireConfirmedAccount = true`).
- **Datos**: Entity Framework Core + SQL Server (`JuntalyColegios/Data/ApplicationDbContext.cs`, migraciones en `Data/Migrations/`).
- **Vistas**: Razor Pages/MVC Views (`JuntalyColegios/Views/`, `JuntalyColegios/Areas/Identity/Pages/`).
- **Estáticos**: `JuntalyColegios/wwwroot/` (Bootstrap 5, jQuery, jquery-validation vendorizados en `wwwroot/lib/`).
- **Sin tests automatizados todavía.**

### Comandos útiles
```bash
dotnet build
dotnet run --project JuntalyColegios
dotnet ef migrations add <Nombre> --project JuntalyColegios
dotnet ef database update --project JuntalyColegios
```

## 2. Rol del orquestador

Ante cualquier tarea, el orquestador:
1. Analiza qué pide el usuario y a qué capa(s) del proyecto afecta.
2. Descompone la tarea en pasos, decidiendo qué subagente(s) de `agente/` intervienen y en qué orden.
3. Delega el detalle de implementación a cada subagente — el orquestador no reemplaza el trabajo de un especialista, lo coordina.
4. Integra los entregables de cada subagente y valida que el resultado final sea coherente.

## 3. Subagentes disponibles

| Subagente | Ubicación | Cuándo usarlo | Cubre |
|---|---|---|---|
| **Arquitectura** | [`agente/arquitectura.md`](./agente/arquitectura.md) | Antes de features nuevas o cambios de modelo de datos; decisiones de estructura/carpetas/dependencias | Estructura de capas, modelado de dominio, nuevas dependencias NuGet, configuración por entorno |
| **Backend** | [`agente/backend.md`](./agente/backend.md) | Controladores, modelos, acceso a datos, migraciones, `Program.cs` | `Controllers/`, `Models/`, `Data/`, `Program.cs` |
| **Frontend** | [`agente/frontend.md`](./agente/frontend.md) | Vistas Razor, layout, estilos, validación cliente | `Views/`, `Areas/Identity/Pages/` (presentación), `wwwroot/css`, `wwwroot/js` |
| **Seguridad** | [`agente/seguridad.md`](./agente/seguridad.md) | Auth/Identity, autorización, secretos, vulnerabilidades (OWASP) | `Areas/Identity/` (lógica), manejo de secretos, revisión transversal de autorización/XSS/SQLi |
| **Testing** | [`agente/testing.md`](./agente/testing.md) | Al agregar/modificar lógica en controladores, modelos o datos; al corregir bugs; cuando se pide cobertura | `JuntalyColegios.Tests/` (xUnit): tests unitarios de controladores/modelos y de integración con `WebApplicationFactory` |
| **Auditoría** | [`agente/auditoria.md`](./agente/auditoria.md) | Al cerrar cualquier tarea, antes de dar por lista una feature o de hacer commit | Build, tests, consistencia, migraciones, cierre de hallazgos de los demás subagentes |

## 4. Reglas de enrutamiento

Palabras/señales en la tarea → subagente:

- "vista", "layout", "botón", "estilo", "responsive", "formulario (maquetado)" → **Frontend**
- "controlador", "modelo", "migración", "base de datos", "endpoint", "servicio" → **Backend**
- "estructura", "cómo organizar", "nueva dependencia", "modelar entidad", "capas" → **Arquitectura**
- "login", "registro", "rol", "permiso", "autorización", "secreto", "contraseña", "vulnerabilidad" → **Seguridad**
- "test", "tests", "cobertura", "unitario", "integración", "xUnit" → **Testing**
- "revisar antes de commit", "¿está listo?", "chequeo final" → **Auditoría**

### Flujos combinados típicos

- **Feature nueva** (ej. módulo de colegios): Arquitectura → Backend → Frontend → Seguridad → Testing → Auditoría.
- **Cambio de modelo/BD**: Arquitectura → Backend (migración) → Testing → Auditoría.
- **Bug**: Backend o Frontend según la capa afectada → Testing (test de regresión) → Auditoría.
- **Cualquier cambio en autenticación, roles o secretos**: siempre pasa por Seguridad, sin excepción, antes de Auditoría.
- Testing corre después de Backend/Frontend (necesita el código ya escrito para testearlo) y antes de Auditoría (que verifica que `dotnet test` pase en verde).

## 5. Cómo invocar un subagente

1. Leer el archivo `agente/<nombre>.md` completo — define rol, alcance, checklist y qué queda fuera de su alcance (con enlaces a los demás subagentes).
2. Adoptar ese rol para la porción de la tarea que le corresponde.
3. Si la herramienta con la que se trabaja soporta subagentes reales (p. ej. la herramienta `Agent` de Claude Code), se puede pasar el contenido del archivo como instrucciones del subagente junto con la tarea concreta — cada archivo ya incluye frontmatter `name`/`description` pensado para eso.
4. El subagente entrega su resultado en el formato descrito en su sección "Entregable al orquestador"; el orquestador lo integra y decide el siguiente paso.

## 6. Convenciones globales (aplican a todos los subagentes)

- Idioma español en UI, mensajes de commit y comentarios orientados al usuario.
- `Nullable` habilitado: no introducir warnings de nulabilidad ni suprimirlos sin justificar.
- No commitear secretos ni cadenas de conexión reales; usar User Secrets (`UserSecretsId` ya está configurado en `JuntalyColegios.csproj`) o variables de entorno.
- No modificar a mano archivos dentro de `wwwroot/lib/` (librerías vendorizadas).
- Reutilizar patrones existentes (Identity, `ApplicationDbContext`, Tag Helpers) antes de introducir uno nuevo.

## 7. Formato de entrega esperado de cada subagente

Cada subagente, al terminar, debe reportar al orquestador:
- **Resumen** de lo que hizo.
- **Archivos tocados** (creados/modificados).
- **Riesgos u observaciones** para el siguiente paso del flujo.
- **Siguiente subagente sugerido**, si corresponde.

El orquestador cierra la tarea recién cuando **Auditoría** da el veredicto de "listo".
