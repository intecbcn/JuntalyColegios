# Requisitos de entrega del proyecto

Checklist de seguimiento: qué se pide, qué está hecho y qué falta. Actualizar este archivo cada vez que se complete un punto.

- **Fecha límite de entrega**: 26/10/2026
- **Leyenda**: `[x]` hecho · `[~]` en progreso · `[ ]` pendiente

---

## 1. Documentación completa (`README.md`)

- [x] a. Descripción general del proyecto
- [x] b. Stack tecnológico utilizado
- [x] c. Instalación y ejecución
- [x] d. Estructura del proyecto
- [x] e. Funcionalidades principales
- [ ] f. Usuario y contraseña de prueba (el proyecto tiene login)

## 2. Código fuente

- [~] a. Repositorio **público** en GitHub — remoto actual: `https://github.com/intecbcn/JuntalyColegios.git` (verificar que sea público)
- Nota: un repositorio privado solo se admite si está perfectamente justificado (habría que conceder acceso al revisor).

## 3. Despliegue / publicación

- [~] Aplicación desplegada y funcionando — dominio: https://colegios.juntaly.com
- [x] URL de acceso añadida al `README.md`

## 4. Presentación (slides)

- [ ] Slides creadas (Google Slides, PowerPoint, Canva…)
- [ ] URL pública o documento subido al repositorio
- [ ] Enlace añadido al `README.md`

## 5. Vídeo explicativo

- [ ] Vídeo con explicación propia **capturando la pantalla** (obligatorio)
- [ ] (Opcional) grabación de la cámara
- [ ] URL pública del vídeo (YouTube, Drive…)
- [ ] Enlace añadido al `README.md`

## 6. Contenido obligatorio en el repositorio

- [~] Documentación (`README.md` creado; faltan usuario de prueba, slides y vídeo)
- [ ] Información del despliegue (puede ir en el `README.md`)
- [ ] Slides (enlace en la documentación o el documento subido)

## 7. Datos para el formulario de entrega

Mismos campos y orden que el formulario.

| Campo | Valor | Estado |
|---|---|---|
| Nombre | Alejandro | [x] |
| Apellidos | Contreras | [x] |
| Email | _(el de inscripción; no se publica en el repo)_ | [ ] |
| URL Repositorio de GitHub | https://github.com/intecbcn/JuntalyColegios _(confirmar que sea público)_ | [~] |
| URL Despliegue o Publicación | https://colegios.juntaly.com _(verificar que esté desplegado)_ | [~] |
| URL Slides | | [ ] |
| URL Vídeo | | [ ] |
| Usuario de prueba | | [ ] |
| Contraseña del usuario de prueba | | [ ] |

---

## 8. Funcionalidades de la aplicación (progreso)

Registro vivo de lo construido y lo pendiente en la propia aplicación.

- [x] Arquitectura hexagonal en 4 proyectos (Domain / Application / Infrastructure / Web)
- [x] Autenticación con ASP.NET Core Identity
- [x] Landing page + solicitud de demo (con tests unitarios, de integración y E2E)
- [x] Entorno de tests automatizados: xUnit + `WebApplicationFactory` + Playwright .NET, con TDD obligatorio en cada feature
- [ ] _(añadir aquí las siguientes funcionalidades a medida que se definan)_

---

## Registro de cambios

| Fecha | Cambio |
|---|---|
| 2026-10-06 | Creación del checklist de requisitos |
| 2026-10-06 | Tabla del formulario de entrega alineada con el formulario real; nombre y apellidos completados |
| 2026-10-06 | `README.md` creado (descripción, stack, instalación, estructura, funcionalidades); URL colegios.juntaly.com |
| 2026-10-06 | Entorno de tests (24 tests: unitarios, integración y E2E) y regla TDD; corregidos 2 bugs detectados por los tests: inyección de HTML en el correo de demo y mensaje de error tras un envío correcto del formulario |
