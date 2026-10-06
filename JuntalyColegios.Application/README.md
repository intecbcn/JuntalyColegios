# JuntalyColegios.Application

Capa de casos de uso. Orquesta el dominio ([`JuntalyColegios.Domain`](../JuntalyColegios.Domain/)) y define los **puertos** (interfaces) que las capas externas deben implementar o consumir. No depende de EF Core, ASP.NET Core ni de `JuntalyColegios.Infrastructure`.

- `Ports/In/` — puertos de entrada (driving ports): interfaces de los casos de uso que invoca la capa de presentación (controladores).
- `Ports/Out/` — puertos de salida (driven ports): interfaces que la capa de infraestructura debe implementar (repositorios, servicios externos).
- `UseCases/` — implementación de los casos de uso (servicios de aplicación) que implementan `Ports/In` y consumen `Ports/Out`.
- `DTOs/` — objetos de transferencia de datos entre la presentación y los casos de uso (nunca se exponen entidades de dominio directamente a la vista).

Ver [`.agents/arquitectura.md`](../.agents/arquitectura.md) para las reglas de dependencia entre capas.
