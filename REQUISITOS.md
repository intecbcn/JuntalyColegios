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

- [x] a. Repositorio **público** en GitHub: https://github.com/intecbcn/JuntalyColegios
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
| URL Repositorio de GitHub | https://github.com/intecbcn/JuntalyColegios | [x] |
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
- [x] Roles (AdminApp, SupervisorColegio, Padre), administrador inicial por configuración, registro público cerrado (alta en cascada) y menú por rol
- [x] Gestión de colegios y supervisores (AdminApp): alta, edición, activar/desactivar y alta de supervisor con invitación por email
- [x] Clases y alumnos (supervisor): clases por colegio, alta/edición/baja de alumnos, aislamiento entre colegios
- [x] Padres, vínculo con alumnos y tags RFID (supervisor): alta con invitación, hijos, vehículos y tag único por colegio
- [x] Panel de padres: hijos y su clase, vehículos (con estado del tag) y personas autorizadas
- [x] Franjas horarias de retiro por día de la semana (colegio y por clase, hora local del colegio; sin franja no se llama a nadie)
- [x] Flujo RFID: API de la antena con clave por colegio, detecciones (en franja → llamada; fuera de franja, duplicada o tag desconocido → solo registro), pantalla de salida con actualización automática y simulador
- [ ] Historial de recogidas, registro de detecciones y datos demo

---

## Registro de cambios

| Fecha | Cambio |
|---|---|
| 2026-10-06 | Creación del checklist de requisitos |
| 2026-10-06 | Tabla del formulario de entrega alineada con el formulario real; nombre y apellidos completados |
| 2026-10-06 | `README.md` creado (descripción, stack, instalación, estructura, funcionalidades); URL colegios.juntaly.com |
| 2026-10-06 | Entorno de tests (24 tests: unitarios, integración y E2E) y regla TDD; corregidos 2 bugs detectados por los tests: inyección de HTML en el correo de demo y mensaje de error tras un envío correcto del formulario |
| 2026-10-12 | Repositorio de GitHub pasado a público (revisado antes el historial: sin secretos) |
| 2026-10-12 | Fase 0 del módulo funcional: tests de integración sobre SQLite en memoria y utilidad de inicio de sesión (TDD: 2 tests en rojo → verde; suite 26/26) |
| 2026-10-12 | Fase 1: roles, administrador inicial, registro público cerrado y menú por rol (TDD: 13 tests en rojo → verde). Seguridad: cerrada también RegisterConfirmation, que entregaba enlaces de confirmación de cualquier cuenta y permitía enumerar emails (test de regresión). Suite 41/41 |
| 2026-10-12 | Fase 2: gestión de colegios y alta de supervisores con invitación por email (TDD: 55 tests en rojo → verde; incluye compensación de cuentas huérfanas). Migración `ModuloColegios` (pendiente de aplicar en Supabase). Panel por rol tras el login (/Panel) y "Mi panel" en la landing. Suite 105/105 |
| 2026-10-12 | Fix: «¿Olvidaste tu contraseña?» no enviaba el email (Identity UI usaba un emisor no-op); restringido AllowedHosts para evitar envenenar los enlaces de recuperación con un Host falso (2 tests de regresión rojo → verde) |
| 2026-10-12 | Fase 3: clases y alumnos del supervisor, aislados por colegio (lo ajeno → 404, sin colegio activo → 403). TDD: 49 tests en rojo → verde. Migración `ClasesYAlumnos` (pendiente de aplicar). Suite 156/156 |
| 2026-10-12 | Fase 4: alta de padres por el supervisor con invitación, vínculo con hijos, vehículos y tags RFID (únicos por colegio). TDD: 55 tests en rojo → verde. Migración `PadresYVehiculos` (pendiente de aplicar). Suite 211/211 |
| 2026-10-12 | Fase 5: panel de padres (/Padres) con hijos, vehículos y personas autorizadas, limitado a la propia familia. TDD: 32 tests en rojo → verde; refactor de la regla de placa única compartida con el supervisor. Migración `PersonasAutorizadas` (pendiente de aplicar). Suite 243/243 |
| 2026-10-12 | Fase 6: franjas horarias de retiro por día de la semana, por colegio y por clase (la de la clase prevalece ese día), en hora local del colegio y con reloj inyectable (TimeProvider). TDD: 40 tests en rojo → verde. Corregido un test inestable de la fase 4. Migración `FranjasRetiro` (pendiente de aplicar). Suite 283/283 |
| 2026-10-12 | Fase 7: flujo RFID completo (API de antena con clave por colegio guardada como hash, detecciones, recogidas, anti-duplicado de 5 min, pantalla de salida con refresco automático, autorizados visibles y simulador). TDD: 49 tests en rojo → verde. Migración `DeteccionesYRecogidas` (pendiente de aplicar). Pendiente: limitar peticiones (rate limiting) en la API de la antena. Suite 333/333 |
