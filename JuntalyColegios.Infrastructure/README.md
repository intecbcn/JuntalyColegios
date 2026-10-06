# JuntalyColegios.Infrastructure

Capa de adaptadores. Implementa los puertos de salida definidos en [`JuntalyColegios.Application/Ports/Out`](../JuntalyColegios.Application/Ports/Out/) usando tecnología concreta (EF Core, PostgreSQL, Identity).

- `Persistence/` — `ApplicationDbContext` (Identity + EF Core), `Persistence/Migrations/` (migraciones EF Core).
- `Persistence/Repositories/` — implementaciones concretas de los repositorios declarados como puertos en `Application/Ports/Out`.
- `Identity/` — adaptadores relacionados con ASP.NET Core Identity que no son puramente de presentación (ej. servicios de envío de email para confirmación de cuenta).

Los comandos de migración de EF Core se ejecutan apuntando aquí como proyecto de migraciones y a `JuntalyColegios` (Web) como proyecto de arranque:

```bash
dotnet ef migrations add <Nombre> --project JuntalyColegios.Infrastructure --startup-project JuntalyColegios
dotnet ef database update --project JuntalyColegios.Infrastructure --startup-project JuntalyColegios
```

Ver [`.agents/arquitectura.md`](../.agents/arquitectura.md) para las reglas de dependencia entre capas.
