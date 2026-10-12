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
- [x] f. Usuario y contraseña de prueba (el proyecto tiene login): supervisor@demo.juntaly.com y padre@demo.juntaly.com / Demo123!

## 2. Código fuente

- [x] a. Repositorio **público** en GitHub: https://github.com/intecbcn/JuntalyColegios
- Nota: un repositorio privado solo se admite si está perfectamente justificado (habría que conceder acceso al revisor).

## 3. Despliegue / publicación

- [x] Aplicación desplegada y funcionando — dominio: https://colegios.juntaly.com (IIS + Cloudflare + Supabase, verificado el 12/10/2026)
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
- [x] Información del despliegue (`README.md` §8: IIS + Cloudflare + Supabase, publicación y variables)
- [ ] Slides (enlace en la documentación o el documento subido)

## 7. Datos para el formulario de entrega

Mismos campos y orden que el formulario.

| Campo | Valor | Estado |
|---|---|---|
| Nombre | Alejandro | [x] |
| Apellidos | Contreras | [x] |
| Email | _(el de inscripción; no se publica en el repo)_ | [ ] |
| URL Repositorio de GitHub | https://github.com/intecbcn/JuntalyColegios | [x] |
| URL Despliegue o Publicación | https://colegios.juntaly.com | [x] |
| URL Slides | | [ ] |
| URL Vídeo | | [ ] |
| Usuario de prueba | supervisor@demo.juntaly.com (también padre@demo.juntaly.com) | [x] |
| Contraseña del usuario de prueba | Demo123! | [x] |

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
- [x] Historial de recogidas (padre), registro de detecciones (supervisor) y datos de demostración con cuentas de prueba

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
| 2026-10-12 | Fase 8: historial de recogidas del padre, registro de detecciones del supervisor y datos de demostración (Demo__Sembrar) con cuentas de prueba de supervisor y padre. TDD: 13 tests en rojo → verde. Las consultas por fecha se ordenan ya en la base de datos (conversor solo para SQLite en tests). Suite 348/348 |
| 2026-10-12 | Migraciones aplicadas en Supabase (esquema anterior de Identity, vacío, recreado). Seguridad: corregido el aviso crítico de Supabase «rls_disabled_in_public» (las tablas, incluida AspNetUsers, eran legibles con la clave pública anon): RLS en todas las tablas y permisos retirados a anon/authenticated, también por defecto para tablas futuras. Verificado en la base: 19/19 tablas con RLS y sin acceso para anon |
| 2026-10-12 | Despliegue preparado: claves de Data Protection en la base (sobreviven a reciclajes de IIS; TDD 1 test rojo → verde, migración `ClavesDataProtection` aplicada con RLS), README actualizado (configuración, estructura, funcionalidades, tests, usuarios de prueba y despliegue en IIS) y paquete de publicación. Pendiente: copiar al servidor IIS y verificar en producción. Suite 349/349 |
| 2026-10-12 | Fix despliegue IIS: la app no arrancaba sin cadena de conexión (error críptico de Npgsql). Ahora falla con un mensaje que indica la variable a definir y carga el `.env` desde la carpeta del sitio (test de regresión rojo → verde). Suite 350/350 |
| 2026-10-12 | Despliegue verificado en producción con las cuentas de prueba: login y redirección por rol, simulador (llama a Sofía, Diego fuera de su franja), entrega, panel del padre con historial, accesos denegados entre perfiles; Supabase con 20/20 tablas con RLS y claves de Data Protection persistidas. Demo: franja general de todo el día para que el revisor pueda probar a cualquier hora (en Caracas son 6 h menos que en España); aplicado también en producción. Suite 355/355 |
| 2026-10-12 | Rate limiting en la API de la antena: 120 lecturas/min por antena (configurable con `Antena__PeticionesPorMinuto`), particionado por clave para que un colegio no consuma el cupo de otro; 429 con Retry-After. TDD: test en rojo → verde. Suite 358/358 |
| 2026-10-12 | Administración: editar (nombre y email, también el de su cuenta) y eliminar supervisores (se elimina su cuenta). TDD: 19 tests en rojo → verde. Suite 378/378 |
| 2026-10-12 | UI: campos editables con tono propio antes del foco; login con botón para ver la contraseña (test E2E rojo → verde), «Recordarme» a la derecha y enlaces separados del botón. Suite 379/379 |
| 2026-10-12 | Pantallas de llamada estilo turnero (portería y aulas) con enlace secreto revocable, solo lectura y datos mínimos; refresco automático, pantalla completa con aviso sonoro y diseño responsive (E2E en TV, tablet y móvil sin scroll horizontal). TDD: 30 tests en rojo → verde. Migración `PantallasLlamadas` aplicada en Supabase con RLS. Suite 407/407 |
| 2026-10-12 | Paneles de administración, supervisor y padres con la identidad visual de Juntaly (layout propio con menú plegable en móvil, `app.css`; se retira `site.css` de la plantilla) y diseño responsive: tablas con scroll dentro de su tarjeta, cabeceras y botones que se apilan en pantallas estrechas. TDD: 3 tests de integración + 2 E2E a 390 px en rojo → verde. Suite 355/355 |
