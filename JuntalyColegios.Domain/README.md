# JuntalyColegios.Domain

Núcleo de la arquitectura hexagonal. Contiene el modelo de dominio puro: **no depende de ningún otro proyecto ni de frameworks externos** (sin EF Core, sin ASP.NET Core).

- `Entities/` — entidades de dominio (identidad propia, ciclo de vida).
- `ValueObjects/` — objetos de valor inmutables (sin identidad propia).
- `Exceptions/` — excepciones de dominio (reglas de negocio violadas).

Ver [`agente/arquitectura.md`](../agente/arquitectura.md) para las reglas de dependencia entre capas.
