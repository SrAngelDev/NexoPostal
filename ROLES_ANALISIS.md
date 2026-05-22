# Análisis de Roles — NexoPostal

> **Estado: IMPLEMENTADO** — Estructura definitiva tras la reestructuración completa.  
> Todas las apps y microservicios están actualizados con esta nueva nomenclatura.

---

## Resumen final (estructura implementada)

| Rol | Enum value | App | Tipo de usuario |
|-----|-----------|-----|-----------------|
| `Cliente` | 0 | clientes-app | Ciudadano que envía paquetes |
| `Admin` | 1 | intranet-app | Administrador del sistema |
| `OperarioOficina` | 2 | intranet-app | Trabaja en una oficina postal |
| `OperarioCTA` | 3 | intranet-app | Gestiona clasificación y logística en CTA |
| `Supervisor` | 4 | intranet-app | Supervisa personal, incidencias y métricas |
| `Repartidor` | 5 | driver-app | Reparte paquetes a domicilio |
| `JefeReparto` | 7 | driver-app | Planifica rutas y gestiona el equipo de reparto |

> Nota: El valor 6 fue intencionalmente omitido del enum (era `RepartidorLogistico`, rol eliminado).

---

## Mapa de acceso por microservicio

### Nexopostal.Auth
- Todos los roles tienen acceso a `/api/auth/login` y `/api/auth/me`

### Nexopostal.Intranet

| Controller / Endpoint | Admin | OperarioOficina | OperarioCTA | Supervisor |
|---|:---:|:---:|:---:|:---:|
| **ScanController** (clase) | ✅ | ✅ | ✅ | ❌ |
| Scan — modos RecepcionOficina, EntregaOficinaDestino, SalidaAReparto | ✅ | ✅ | ❌ | ❌ |
| Scan — modos RecepcionCta, Clasificacion, DespachoTroncal, RecepcionTroncal | ✅ | ❌ | ✅ | ❌ |
| **AsignacionesController** (clase) | ✅ | ✅ | ✅ | ✅ |
| Asignaciones — Crear tarea | ✅ | ✅ | ✅ | ❌ |
| Asignaciones — Iniciar/Completar/Cancelar tarea (manual) | ✅ | ❌ | ❌ | ✅ |
| Asignaciones — Mis pendientes / Mis en progreso | ❌ | ✅ | ✅ | ❌ |
| Asignaciones — Buscar por código (`?codigo=`) | ❌ | ✅ | ✅ | ❌ |
| Asignaciones — Ver todas del CTA | ✅ | ❌ | ✅ | ✅ |
| **AdmisionController** | ✅ | ❌ | ✅ | ❌ |
| **IncidenciasController** (clase) | ✅ | ❌ | ❌ | ✅ |
| Incidencias — Reportar paquete fuera de tareas | ✅ | ✅ | ✅ | ✅ |
| **CtasController** (clase) | ✅ | ✅ | ✅ | ✅ |
| Ctas — Dashboard | ✅ | ❌ | ✅ | ✅ |
| **MovimientosController** | ✅ | ❌ | ✅ | ❌ |
| **HistorialController** — Ver historial / último evento | ✅ | ✅ | ✅ | ✅ |
| **HistorialController** — Registrar evento manual | ✅ | ❌ | ✅ | ❌ |
| **OperariosController** (clase) | ✅ | ✅ | ✅ | ✅ |
| Operarios — Crear / Desactivar | ✅ | ❌ | ❌ | ✅ |
| **IntranetHub** (SignalR) | ✅ | ✅ | ✅ | ✅ |

### Nexopostal.Reparto

| Endpoint | Admin | Repartidor | JefeReparto |
|---|:---:|:---:|:---:|
| GET /repartidores | ✅ | ❌ | ✅ |
| POST /repartidores | ✅ | ❌ | ✅ |
| GET /mi-perfil | ❌ | ✅ | ✅ |
| GET /rutas | ✅ | ❌ | ✅ |
| POST /rutas | ✅ | ❌ | ✅ |
| GET /rutas/{id} | ✅ | ✅ | ✅ |
| GET /ruta (activa) | ❌ | ✅ | ✅ |
| POST /rutas/{id}/iniciar | ❌ | ✅ | ✅ |
| POST /rutas/{id}/finalizar | ❌ | ✅ | ✅ |
| POST /rutas/{id}/entregas | ✅ | ❌ | ✅ |
| GET /entregas | ✅ | ✅ | ✅ |
| POST /confirmar | ❌ | ✅ | ✅ |
| GET /dashboard | ✅ | ❌ | ✅ |

---

## Descripción detallada de cada rol


**App:** `clientes-app`  
**Quién es:** Cualquier ciudadano que se registra para enviar paquetes.

### Lo que puede hacer hoy:
- Registrarse y hacer login (público)
- **Cotizar** un envío sin autenticación
- **Crear un envío** y pagar con Stripe Checkout
- Ver **sus envíos** (`/mis-envios`)
- Hacer **tracking público** de cualquier envío (`/track/{numero}`)
- Gestionar su **perfil** (DNI, teléfono, dirección predeterminada)
- Gestionar su **agenda de direcciones favoritas**
- Descargar **etiqueta PDF** y **factura PDF**

### Acceso a APIs:
- `Nexopostal.Ciudadano` — Envíos, perfil, pagos, tarifas
- `Nexopostal.Auth` — Login, registro, perfil, cambio de contraseña

### ⚠️ Observaciones:
- El rol `Cliente` es el por defecto al registrarse (`defaultValue: "Cliente"` en la BD)
- No tiene acceso a ninguna información interna

---

## 2. Admin

**App:** `intranet-app`  
**Quién es:** Administrador técnico/operativo de toda la red NexoPostal.

### Lo que puede hacer hoy:
- **Todo** lo que pueden hacer OperarioJefe, OperarioLogistico y OperarioOficina
- Ver el **dashboard global** (`GET /api/ctas/dashboard-global`) — exclusivo Admin
- En SignalR: se une a **todos** los grupos de todos los CTAs simultáneamente
- Crear y desactivar operarios
- Admitir paquetes, gestionar movimientos, reportar incidencias

### Acceso a APIs (intranet):
| Endpoint | Acceso |
|----------|--------|
| `GET /api/ctas/dashboard-global` | Solo Admin |
| Todos los demás endpoints de Intranet | Admin + otros roles |

### ⚠️ Observaciones:
- Es un superusuario que no está asignado a ningún CTA real; el código lo trata con casos especiales (`if (User.IsInRole("Admin")) { ... }`)
- Actualmente el seed lo asigna también como `OperarioJefe` en todos los CTAs de la BD de Intranet, lo que es un workaround

---

## 3. OperarioOficina

**App:** `intranet-app`  
**Quién es:** Trabaja físicamente en una oficina postal. Recibe paquetes, los escanea, y los prepara para salir a reparto.

### Lo que puede hacer hoy:
- **Escanear paquetes** (todos los modos: RecepcionOficina, EntregaOficinaDestino, SalidaAReparto, etc.)
- Ver el **historial interno** de un paquete
- Ver sus **tareas asignadas** (`/mis-pendientes`, `/mis-en-progreso`)
- **Iniciar** y **completar** tareas asignadas
- Ver información de **su CTA/oficina**
- Consultar **oficinas postales** y su lista de operarios

### Lo que NO puede hacer:
- Crear asignaciones (asignar paquetes a operarios)
- Ver el dashboard del CTA
- Gestionar movimientos troncales entre CTAs
- Admitir paquetes en la red logística
- Reportar o gestionar incidencias
- Crear o desactivar operarios

### Flujo típico:
1. Recibe paquete en ventanilla → escanea con modo `RecepcionOficina`
2. Recibe asignación de tarea → la inicia y completa
3. Escanea paquetes para salida a reparto → modo `SalidaAReparto`

### ⚠️ Observaciones / Problemas detectados:
- En `AsignacionesController`, los endpoints `iniciar` y `completar` solo permiten `Admin,OperarioOficina`, pero el endpoint `crear` asignaciones solo permite `Admin,OperarioLogistico`. Esto parece correcto pero hay que verificar si el OperarioOficina también debería poder asignarse tareas a sí mismo.
- No tiene acceso al dashboard del CTA, pero sí puede ver el detalle de los CTAs y operarios, lo que puede ser excesivo.

---

## 4. OperarioLogistico

**App:** `intranet-app`  
**Quién es:** Trabaja en un CTA (Centro de Tratamiento Automatizado). Coordina la clasificación y asignación de tareas a los operarios de oficina.

### Lo que puede hacer hoy:
- **Todo lo que puede OperarioOficina**, MÁS:
- **Crear asignaciones** — asignar paquetes a operarios de su CTA
- **Cancelar asignaciones**
- Ver **todas las asignaciones** de su CTA
- **Admitir paquetes** en la red logística (`POST /api/admision/paquete`)
- Gestionar **movimientos troncales** entre CTAs (crear, despachar, recibir)
- **Registrar eventos** de historial manualmente
- Ver el **dashboard del CTA**
- Recibe notificaciones SignalR: `PaqueteRecibidoEnCta`, `MovimientoDespachado`, `MovimientoRecibido`

### Lo que NO puede:
- Reportar incidencias
- Crear o desactivar operarios
- Ver el dashboard global (solo Admin)

### Flujo típico:
1. Recibe notificación SignalR de nuevo paquete en su CTA
2. Crea asignación de tarea de clasificación para un OperarioOficina
3. Despacha movimientos troncales (paquetes que van a otro CTA)
4. Recibe confirmación de llegada de paquetes de otro CTA

### ⚠️ Observaciones / Problemas detectados:
- El OperarioLogistico puede admitir paquetes (`/api/admision`), pero también puede el OperarioJefe. ¿Tiene sentido que el jefe admita paquetes directamente?
- Tiene acceso a escanear paquetes con todos los modos, incluyendo los de oficina como `RecepcionOficina`. ¿Debería un logístico de CTA poder hacer recepción en oficina?

---

## 5. OperarioJefe

**App:** `intranet-app`  
**Quién es:** Responsable de zona. Supervisa tanto oficinas postales como CTAs.

### Lo que puede hacer hoy:
- **Todo lo que puede OperarioLogistico**, MÁS:
- **Reportar incidencias** (paquetes dañados, extraviados, etc.)
- **Actualizar el estado de incidencias** (Abierta → EnRevision → Resuelta → Cerrada)
- **Crear nuevos operarios** y asignarlos a CTAs
- **Desactivar operarios** (soft delete)
- Recibe notificaciones SignalR: incidencias + todos los eventos de logístico y operario

### Lo que NO puede:
- Ver el dashboard global (solo Admin)

### Flujo típico:
1. Supervisa el dashboard del CTA para detectar problemas
2. Reporta incidencia cuando detecta un paquete dañado o extraviado
3. Da de alta a nuevos operarios y los asigna a CTAs u oficinas
4. Escala incidencias y registra la resolución

### ⚠️ Observaciones / Problemas detectados:
- **Incoherencia importante:** El OperarioJefe tiene acceso a todos los modos de escaneo (como el OperarioOficina y OperarioLogistico). ¿Debería un jefe estar escaneando paquetes en el día a día?
- El OperarioJefe puede también admitir paquetes. La responsabilidad de este rol no está bien delimitada: ¿es jefe de oficina, jefe de CTA, o jefe de zona?

---

## 6. Repartidor

**App:** `driver-app`  
**Quién es:** Repartidor de última milla. Sale de la oficina con paquetes y los entrega a domicilio.

### Lo que puede hacer hoy:
- Ver sus rutas de reparto del día
- Ver el detalle de cada entrega en la ruta
- Registrar el resultado de cada entrega (Entregado, Ausente, DireccionIncorrecta, Rechazado...)
- Capturar firma digital y foto de entrega
- Ver mapa con paradas y su posición GPS
- Navegar a siguiente parada (Google Maps / Waze)

### Flujo típico:
1. Login en driver-app
2. Ve su ruta del día (creada por RepartidorLogistico o RepartidorJefe)
3. Sale de la oficina → inicia la ruta
4. Registra cada entrega con firma/foto
5. Devuelve paquetes no entregados a la oficina

### ⚠️ Observaciones / Problemas detectados:
- **No puede crear ni ver rutas de otros repartidores** (bien)
- No está claro en el código si tiene acceso a algún dashboard o solo a la vista de ruta activa

---

## 7. RepartidorLogistico

**App:** `driver-app`  
**Quién es:** Actualmente **no está bien definido** en el código.

### Estado actual en el código:
- El guard de `driver-app` lo incluye junto a Repartidor y RepartidorJefe
- `isRepartidorLogistico()` existe en el AuthService
- En el seed de datos tiene asignado a Sofía Navarro con vehículo Moto

### ¿Qué debería hacer? (propuesta a debatir):
- ¿Tiene acceso a crear/asignar rutas?
- ¿Puede ver las rutas de todos los repartidores de su oficina?
- ¿Puede gestionar la carga de paquetes desde la oficina a los repartidores?

### ⚠️ Problema:
- **Este rol no tiene funcionalidad diferenciada del Repartidor básico en el código actual.**

---

## 8. RepartidorJefe

**App:** `driver-app`  
**Quién es:** Actualmente **no está bien definido** en el código.

### Estado actual en el código:
- El guard de `driver-app` lo incluye junto a Repartidor y RepartidorLogistico
- `isRepartidorJefe()` existe en el AuthService
- En el seed de datos tiene asignado a Javier Torres con furgoneta

### ¿Qué debería hacer? (propuesta a debatir):
- ¿Puede crear rutas de reparto?
- ¿Puede ver el dashboard de reparto de su oficina?
- ¿Puede gestionar el equipo de repartidores?
- ¿Puede manejar incidencias de reparto?

### ⚠️ Problema:
- **Este rol no tiene funcionalidad diferenciada del Repartidor básico en el código actual.**

---

## Resumen de problemas / preguntas abiertas

1. **¿Qué hace exactamente RepartidorLogistico vs Repartidor?** No hay diferenciación real en el código.

2. **¿Qué hace exactamente RepartidorJefe?** No hay diferenciación real. ¿Debería poder crear rutas y ver a todos sus repartidores?

3. **¿El OperarioJefe es jefe de oficina o jefe de CTA?** Actualmente tiene acceso a ambos ámbitos, lo que puede ser excesivo o incorrecto.

4. **¿El OperarioLogistico debería poder hacer RecepcionOficina?** Parece un error de diseño que un operario de CTA tenga modos de escaneo de oficina postal.

5. **¿El OperarioOficina debería poder auto-asignarse tareas** o siempre depende de que el OperarioLogistico se las asigne?

6. **¿Tiene sentido separar OperarioOficina de OperarioLogistico?** ¿O deberían ser un solo rol con permisos granulares?

7. **¿Deben existir 3 niveles en repartidores** (Repartidor, RepartidorLogistico, RepartidorJefe) o con 2 (Repartidor, RepartidorJefe) es suficiente?

---

## Propuesta de matriz de acceso simplificada

|  | Cliente | OperarioOficina | OperarioLogistico | OperarioJefe | Admin | Repartidor | RepartidorJefe |
|--|---------|-----------------|-------------------|--------------|-------|------------|----------------|
| Crear envíos / pagar | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Tracking público | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Escanear (oficina) | ❌ | ✅ | ❌? | ✅? | ✅ | ❌ | ❌ |
| Escanear (CTA) | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Asignar tareas | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Ejecutar tareas | ❌ | ✅ | ❌ | ❌? | ✅ | ❌ | ❌ |
| Movimientos troncales | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Incidencias | ❌ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ |
| Crear operarios | ❌ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ |
| Dashboard CTA | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Dashboard global | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Ver rutas propias | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Crear rutas | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ✅? |
| Ver rutas de equipo | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ✅? |


