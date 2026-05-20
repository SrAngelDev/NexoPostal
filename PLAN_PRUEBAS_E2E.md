# Plan de Pruebas End-to-End — Envío Madrid → Barcelona

Plan paso a paso para validar el flujo completo de NexoPostal con un paquete real desde una **CTA de Madrid** hasta una **CTA de Barcelona** y entrega final al destinatario.

> Asume despliegue ya activo en `https://nexopostal.com` (o el dominio que uses) y que el último commit (`1aa26b5`) ha pasado por GitHub Actions sin errores.

---

## 0. Pre-requisitos antes de empezar

- [ ] **Verificar deploy GHA**: el workflow del último push está verde.
- [ ] **Verificar contenedores arriba** en el servidor:
  ```bash
  ssh root@165.22.26.107 "docker ps --format 'table {{.Names}}\t{{.Status}}'"
  ```
  Deben estar `running`: `gateway`, `modulo-auth`, `modulo-ciudadano`, `modulo-intranet`, `modulo-reparto`, sus 4 postgres, `clientes-app`, `driver-app`, `intranet-app`, `nginx`.
- [ ] **Verificar migración Reparto aplicada** (`UbicacionesRepartidores`):
  ```bash
  ssh root@165.22.26.107 "docker logs --tail 60 nexopostal-modulo-reparto-1 | grep -iE 'migration|UbicacionRepartidor'"
  ```
- [ ] **Salud del Gateway**: `curl -i https://nexopostal.com/api/nexopostal/reparto/dashboard` → `401` (señal de que la ruta existe y exige auth).

## 1. Usuarios y datos maestros que necesitas

Necesitas estos usuarios creados (o créalos antes de empezar):

| Rol               | App           | Para qué                                            |
|-------------------|---------------|------------------------------------------------------|
| Cliente           | clientes-app  | Crear la expedición Madrid → Barcelona               |
| OperarioOficina (Madrid)  | intranet-app  | Recepción en oficina origen                  |
| OperarioCTA (CTA Madrid)  | intranet-app  | Recepción en CTA origen + emitir a CTA destino |
| OperarioCTA (CTA Barcelona)| intranet-app | Recepción en CTA destino                     |
| JefeReparto (CTA Barcelona)| driver-app   | Crear ruta, asignar paradas, ver mapa        |
| Repartidor (CTA Barcelona) | driver-app   | Ejecutar la ruta y entregar el paquete       |
| Destinatario      | clientes-app (opcional) | Consultar seguimiento público            |

**Datos del envío sugeridos:**

- **Origen**: Calle Gran Vía 1, 28013 Madrid → Oficina/CTA Madrid.
- **Destino**: Av. Diagonal 100, 08019 Barcelona → CTA Barcelona.
- **Producto**: paquete estándar, 2 kg.

---

## 2. Cliente — Crear la expedición

App: **clientes-app** (`https://nexopostal.com/`).

- [ ] Login como **Cliente**.
- [ ] Ir a **Nuevo envío** / crear expedición.
- [ ] Rellenar:
  - Origen: Madrid (CP 28013).
  - Destino: Barcelona (CP 08019).
  - Datos del destinatario (nombre, email, teléfono).
  - Servicio / contra-reembolso si aplica.
- [ ] Confirmar pago (Stripe en modo test).
- [ ] **Anotar `numeroExpedicion`** y `numeroSeguimiento` que devuelve la respuesta.

**Comprobaciones:**
- [ ] La expedición aparece en "Mis envíos" en estado `Pendiente` / `Creada`.
- [ ] Consulta pública: `GET https://nexopostal.com/api/nexopostal/ciudadano/seguimiento/{numeroSeguimiento}` devuelve estado inicial.

---

## 3. Oficina origen (Madrid) — Recepción

App: **intranet-app**, usuario **OperarioOficina (Madrid)**.

- [ ] Login. Ir a recepción / entrada de paquetes.
- [ ] Escanear / introducir `numeroExpedicion`.
- [ ] Confirmar recogida en oficina origen.

**Comprobaciones:**
- [ ] El tracking público muestra evento `Recogido en oficina origen` con fecha/hora.
- [ ] El paquete aparece como "pendiente de envío a CTA".

---

## 4. CTA origen (Madrid) — Salida a CTA destino

App: **intranet-app**, usuario **OperarioCTA (CTA Madrid)**.

- [ ] Login. Ir a flujo de CTA.
- [ ] Marcar **recepción en CTA Madrid** del paquete.
- [ ] Generar / asociar a **envío inter-CTA Madrid → Barcelona**.
- [ ] Marcar el envío inter-CTA como `EnTransito`.

**Comprobaciones:**
- [ ] Tracking público: aparecen eventos `En CTA origen` y `En tránsito a CTA destino`.

---

## 5. CTA destino (Barcelona) — Recepción

App: **intranet-app**, usuario **OperarioCTA (CTA Barcelona)**.

- [ ] Login. Ir a recepciones.
- [ ] Confirmar **recepción del envío inter-CTA** en Barcelona.
- [ ] Verificar que el paquete queda en estado `EnCTADestino` / `PendienteAsignacion`.

**Comprobaciones:**
- [ ] Tracking público: evento `Llegado a CTA destino`.
- [ ] En el microservicio Reparto, el paquete debería estar disponible para ser asignado a una ruta de reparto:
  ```bash
  curl -H "Authorization: Bearer $JEFE_TOKEN" https://nexopostal.com/api/nexopostal/reparto/entregas/pendientes-asignacion
  ```
  El paquete aparece si **ya está en una ruta** Planificada que quieras reorganizar. Para que el JefeReparto lo asigne por primera vez, normalmente al recibirlo en la CTA se crea la entrega asociada a una ruta del día.

---

## 6. JefeReparto (Barcelona) — Planificar reparto

App: **driver-app** (`https://nexopostal.com/driver/` o el subdominio que uses).

### 6.1 Login y dashboard del jefe

- [ ] Login como **JefeReparto**.
- [ ] Debe redirigir automáticamente a **`/dashboard-jefe`** (no a `/`).
- [ ] Ver las 3 cards rápidas: **Gestión de Rutas**, **Mapa en tiempo real**, **Asignar paradas**.
- [ ] Ver las métricas del día (rutas hoy, repartidores activos, etc.).

### 6.2 Gestión de Rutas

- [ ] Ir a **Gestión de Rutas**.
- [ ] Crear una ruta nueva para hoy:
  - Asignar **Repartidor (Barcelona)**.
  - Estado inicial: `Planificada`.
  - Añadir la entrega correspondiente al paquete de Madrid.
- [ ] Confirmar que la ruta queda en estado `Planificada`.

### 6.3 Asignar paradas (opcional, si quieres mover entregas entre rutas)

- [ ] Ir a **Asignar paradas**.
- [ ] Si tu paquete aparece, seleccionar otra ruta planificada del día y pulsar **Asignar**.
- [ ] Verificar mensaje "Entrega XXX reasignada correctamente" y que desaparece de la lista.
- [ ] Volver a Gestión de Rutas y confirmar que la entrega cambió de ruta.

### 6.4 Mapa en tiempo real

- [ ] Ir a **Mapa en tiempo real**.
- [ ] Inicialmente, si nadie está repartiendo aún, el mapa estará vacío (esperado).
- [ ] Lo verás poblarse en el paso 7.

---

## 7. Repartidor (Barcelona) — Ejecutar la ruta

App: **driver-app**, usuario **Repartidor**.

> Usa un móvil o el modo dispositivo del DevTools con permisos de **geolocalización** y **cámara** habilitados (HTTPS obligatorio).

### 7.1 Login

- [ ] Login como Repartidor.
- [ ] Debe ir al **dashboard simplificado** con solo dos cards: **Ruta activa y paradas** y **Escanear paquetes**.
- [ ] Si intentas navegar a `/dashboard-jefe`, `/gestion-rutas`, `/mapa-tiempo-real` o `/asignar-paradas`, el guard te debe **devolver al dashboard** (no autorizado).

### 7.2 Iniciar ruta

- [ ] Ir a **Ruta activa y paradas**.
- [ ] Debe mostrar la ruta planificada con la entrega del paquete de Madrid.
- [ ] Pulsar **Iniciar ruta** → debe llamar a `POST /api/nexopostal/reparto/rutas/{id}/iniciar`.
- [ ] La ruta cambia a estado `EnCurso`.
  - 🔍 **Verificación clave**: este endpoint era el bug recién corregido (`ruta-iniciar/.../iniciar` → `rutas/.../iniciar`). Si no funcionara, revisa Network en DevTools.

### 7.3 GPS en tiempo real

- [ ] Aceptar permiso de geolocalización.
- [ ] La app empezará a enviar `POST /api/nexopostal/reparto/ubicacion` periódicamente.
- [ ] Desde **otra pestaña** logueado como JefeReparto: abrir **Mapa en tiempo real**.
- [ ] Debe aparecer el marcador del repartidor con su nombre, código y código de ruta. El mapa refresca cada 15 s.

### 7.4 Entrega del paquete

- [ ] En la app del repartidor, ir a la parada del paquete Madrid→Barcelona.
- [ ] Marcar como **Entregada** con:
  - Foto (opcional).
  - Firma del destinatario.
  - Comentario.
- [ ] Confirmar → debe llamar a `POST /api/nexopostal/reparto/confirmar?entregaId=...`.

**Comprobaciones:**
- [ ] La entrega queda en estado `Entregada`.
- [ ] Tracking público: evento `Entregado` con timestamp.

### 7.5 Finalizar ruta

- [ ] Tras entregar todas las paradas, pulsar **Finalizar ruta** → `POST /api/nexopostal/reparto/rutas/{id}/finalizar`.
- [ ] La ruta queda en estado `Finalizada`.

---

## 8. Cliente final — Verificación

- [ ] Como **Cliente** (clientes-app) → "Mis envíos" → la expedición debe figurar como `Entregada`.
- [ ] Consulta de tracking público: timeline completo:
  1. Creada
  2. Recogida en oficina origen
  3. En CTA Madrid
  4. En tránsito a CTA Barcelona
  5. En CTA destino
  6. En reparto
  7. Entregada
- [ ] Si el envío era contra-reembolso, comprobar que el cobro se ha registrado.

---

## 9. Casos a probar también (negativos / edge)

- [ ] **Rol incorrecto**: con cuenta `Cliente`, intentar loguearse en driver-app → debe rechazar.
- [ ] **JefeReparto en rutas operativas**: intentar `POST /api/nexopostal/reparto/rutas/{id}/iniciar` con token de jefe → `403 Forbidden`.
- [ ] **Repartidor en endpoints de jefe**: `GET /ubicaciones-activas` con token de repartidor → `403`.
- [ ] **Reasignar entrega ya entregada**: debe devolver error de negocio (estado inválido).
- [ ] **Reasignar a ruta no Planificada (EnCurso/Finalizada)**: debe devolver error.
- [ ] **Sin permiso de geolocalización**: la app debe mostrar aviso y no romper.
- [ ] **Operario intenta cambio manual de estado**: con `OperarioCTA`/`OperarioOficina`, llamar `PUT /api/asignaciones/{id}/iniciar` o `/completar` o `/cancelar` → `403 Forbidden` (solo Admin/Supervisor).
- [ ] **Buscador encuentra tarea propia**: en intranet-app, panel "Confirmar paso" con código de una tarea Pendiente del operario → `GET /api/asignaciones/buscar` devuelve la tarea con `modoSugerido` y la UI dispara `POST /api/scan/procesar` automáticamente.
- [ ] **Paquete fuera de tus tareas**: el operario teclea/escanea un código que no aparece en sus tareas → `GET /api/asignaciones/buscar` responde `404 { message: "Paquete fuera de tus tareas" }`. La UI abre modal bloqueante; al rellenar motivo y enviar, `POST /api/incidencias/reportar-fuera-tareas` crea incidencia tipo `PaqueteFueraDeTareas` visible para el Supervisor.
- [ ] **Encadenamiento por escaneo**: tras escanear `RecepcionOficina` se debe crear automáticamente la tarea `SalidaOficinaACta`; al escanearla, `Recepcion` en CTA; y así hasta `DisponibleParaReparto` (que NO crea siguiente tarea, sólo emite SignalR `PaqueteDisponibleParaReparto`).

---

## 10. Útiles rápidos (curl)

Obtener token (ajusta credenciales):
```bash
TOKEN=$(curl -s https://nexopostal.com/api/nexopostal/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"jefe@x.com","password":"..."}' | jq -r .token)
```

Ubicaciones activas:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  https://nexopostal.com/api/nexopostal/reparto/ubicaciones-activas
```

Entregas pendientes de asignación:
```bash
curl -H "Authorization: Bearer $TOKEN" \
  https://nexopostal.com/api/nexopostal/reparto/entregas/pendientes-asignacion
```

Reasignar entrega:
```bash
curl -X PATCH -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"nuevaRutaId": 42}' \
  https://nexopostal.com/api/nexopostal/reparto/entregas/123/reasignar
```

Logs en vivo de Reparto:
```bash
ssh root@165.22.26.107 "docker logs -f --tail 100 nexopostal-modulo-reparto-1"
```

---

## 11. Checklist final de validación

- [ ] Flujo completo Madrid→Barcelona sin errores.
- [ ] Separación de roles funcionando (jefe ≠ repartidor).
- [ ] Mapa en tiempo real muestra al repartidor.
- [ ] Asignar paradas funciona y respeta reglas de negocio.
- [ ] Tracking público completo y coherente.
- [ ] No hay errores 500 en logs de ningún microservicio durante la prueba.
