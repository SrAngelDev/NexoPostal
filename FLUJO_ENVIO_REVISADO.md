# Flujo de envío revisado (origen → destino) — Análisis y plan de cambios

> Documento de diseño y auditoría del flujo "ideal" descrito por el cliente del proyecto,
> comparado contra lo que ya está implementado en el código actual. Sirve como guía
> para identificar qué hay que **modificar**, qué hay que **añadir** y qué **se reutiliza**.

---

## 1. Flujo objetivo (lo que tiene que pasar)

### 1.1 Reglas de negocio firmes

1. Un envío **solo puede iniciarse de dos formas**:
   - **Online**: el cliente paga por internet desde `clientes-app`, se genera la etiqueta y la factura.
   - **En oficina**: el cliente lleva el paquete físicamente a una oficina y el **operario de oficina** lo da de alta y cobra.
2. **No existe recogida a domicilio**. El repartidor nunca va al domicilio del remitente.
   El remitente **siempre** entrega el paquete en una oficina.
3. En el flujo online, el cliente **selecciona durante el alta la oficina origen** donde va a depositar
   el paquete. Esa oficina queda fijada en el envío.
4. Cuando un operario escanea un paquete en modo "Recepción en oficina":
   - Si el paquete pertenece a su oficina → recepción válida → estado `RecogidoEnOrigen`.
   - Si no pertenece a su oficina → **bloqueo** con mensaje
     `"Este paquete debe entregarse en la oficina X (CP X, ciudad X)"`.
5. Tras el escaneo correcto en oficina origen, el sistema genera **automáticamente**
   una asignación al operario del **CTA correspondiente a esa oficina** para clasificar.
6. Cuando el CTA origen clasifica, el estado pasa a `EnTransitoHaciaCentroDestino`,
   la asignación del operario CTA origen se **cierra** y se crea **otra asignación** automática
   para el operario del **CTA destino**.
7. El operario CTA destino **solo clasifica**. Al confirmar la clasificación, el sistema
   bifurca según el tipo de entrega del envío:
   - **Entrega a domicilio**: aparece en la bandeja del **JefeReparto** del CTA destino
     como "Disponible para reparto". El JefeReparto crea/edita una ruta y la asigna a un
     repartidor. El repartidor inicia la ruta y entrega.
   - **Entrega en oficina destino**: se crea automáticamente una asignación al operario
     de la oficina destino. El paquete sale del CTA hacia esa oficina. Cuando llegue,
     el operario lo escanea como `DepositadoEnOficina` y el destinatario podrá recogerlo.

### 1.2 Estados públicos que verá el cliente

`PendientePago` → `Admitido` → `EnTransito` → `EnReparto` o `EnOficina` →
`Entregado` / `Incidencia` / `Devuelto`.

### 1.3 Estados internos clave (granularidad operativa)

```
PendientePago
   ↓ pago confirmado / alta en oficina
PendienteRecogida
   ↓ escaneo "RecepcionOficina" en la oficina correcta
RecogidoEnOrigen
   ↓ escaneo "SalidaOficinaACta"
EnTransitoACentroOrigen
   ↓ escaneo "RecepcionCta" en CTA origen
RecibidoEnCentroOrigen
   ↓ escaneo "Clasificacion" en CTA origen
ClasificadoParaExpedicion
   ↓ escaneo "DespachoTroncal"
EnTransitoHaciaCentroDestino
   ↓ escaneo "RecepcionTroncal" en CTA destino
RecibidoEnCentroDestino
   ↓ escaneo "Clasificacion" en CTA destino
        ├─ TipoEntrega = Domicilio → DisponibleParaReparto
        │                                ↓ JefeReparto crea ruta y asigna repartidor
        │                            AsignadoARuta
        │                                ↓ repartidor inicia ruta
        │                            EnReparto → Entregado / Ausente / Incidencia
        │
        └─ TipoEntrega = Oficina → EnTransitoAOficinaDestino
                                      ↓ escaneo "EntregaOficinaDestino" en oficina destino
                                  DepositadoEnOficina
                                      ↓ destinatario recoge
                                  Entregado
```

---

## 2. Auditoría del código actual

> Análisis hecho sobre el repositorio en la fecha de este documento. Para cada
> punto del flujo objetivo se indica **qué hay**, **qué falta** y **dónde tocarlo**.

### 2.1 Alta online (cliente paga) — ✅ casi completo

- [microservicios/Nexopostal/Nexopostal.Ciudadano/Controllers/EnviosController.cs](microservicios/Nexopostal/Nexopostal.Ciudadano/Controllers/EnviosController.cs)
  expone `POST /api/envios/crear` → crea `Envio` en estado `Admitido` / `PendienteRecogida`,
  genera `NumeroSeguimiento` y `NumeroExpedicion`, calcula tarifa.
- Pago Stripe y procesamiento posterior (etiqueta, factura, email, notificación a Intranet
  por `POST /api/admision/interno/paquete`) ya están funcionando — ver
  [FLUJO_ENVIO_PAGO_A_ENTREGA.md](FLUJO_ENVIO_PAGO_A_ENTREGA.md) sección 2.
- [microservicios/Nexopostal/Nexopostal.Intranet/Controllers/AdmisionController.cs](microservicios/Nexopostal/Nexopostal.Intranet/Controllers/AdmisionController.cs)
  expone el endpoint interno con `X-Service-Key`.

**Lo que falta para cumplir las reglas nuevas:**

- El modelo `Envio` ([Models/Envio.cs](microservicios/Nexopostal/Nexopostal.Ciudadano/Models/Envio.cs))
  guarda solo `Destino` y `CodigoPostalDestino`. **No** guarda:
  - `OficinaOrigenId` (la oficina elegida por el remitente para depositar el paquete).
  - `OficinaDestinoId` (oficina destino cuando el destinatario quiere recogida en oficina).
  - `TipoEntrega` (`Domicilio` | `Oficina`).
- `CrearEnvioDto` y `EnvioCreadoDto` tampoco transportan esos campos.
- En `clientes-app` no hay todavía pantalla para elegir oficina origen ni tipo de entrega.

### 2.2 Alta en oficina (operario de oficina) — ❌ NO existe

No hay ningún endpoint ni servicio en `Nexopostal.Intranet` ni en `Nexopostal.Ciudadano`
para que un operario de oficina dé de alta un envío entregado físicamente por un cliente
que no usó la web. Tampoco hay UI en `intranet-app`.

### 2.3 Validación "oficina correcta" al escanear — ❌ NO existe

[microservicios/Nexopostal/Nexopostal.Intranet/Services/ScanProcessorService.cs](microservicios/Nexopostal/Nexopostal.Intranet/Services/ScanProcessorService.cs)
en `ProcesarRecepcionOficina` solo registra el evento. **No** compara
`req.OficinaJsonId` contra la oficina origen del envío (que ni siquiera está persistida hoy).

Cualquier operario de cualquier oficina podría aceptar el paquete. **Hay que añadir
la verificación y el mensaje de error claro**.

### 2.4 Cadena de escaneos y autoasignación — ✅ implementada (parcialmente correcta)

El `ScanProcessorService` ya encadena tareas vía `AutoAsignarTareaEnCtaAsync` y
`CerrarTareaSiExisteAsync`:

| Escaneo                  | Cierra tarea         | Crea siguiente tarea                              |
| ------------------------ | -------------------- | ------------------------------------------------- |
| `SalidaOficinaACta`      | —                    | `Recepcion` (CTA origen)                          |
| `RecepcionCta`           | `Recepcion`          | `Clasificacion` (CTA origen)                      |
| `Clasificacion` (origen) | `Clasificacion`      | `DespachoTroncal` si hay troncal programado       |
| `DespachoTroncal`        | `DespachoTroncal`    | —                                                 |
| `RecepcionTroncal`       | —                    | `Clasificacion` (CTA destino)                     |
| `Clasificacion` (dest.)  | `Clasificacion`      | `DisponibleParaReparto`                           |
| `DisponibleParaReparto`  | `DisponibleParaReparto` | — (notifica a JefeReparto vía SignalR)         |

La autoasignación elige operario del CTA correcto (probablemente con balanceo de carga
en `AutoAsignarTareaEnCtaAsync` → ver
[Services/AsignacionService.cs](microservicios/Nexopostal/Nexopostal.Intranet/Services/AsignacionService.cs)).

**Lo que falta:**

- Tras el primer `RecepcionOficina` no se crea automáticamente la tarea `Recepcion` en
  el CTA correspondiente. Hoy hace falta un segundo escaneo `SalidaOficinaACta`.
  En el flujo objetivo, **una vez confirmado `RecogidoEnOrigen`**, la asignación al
  operario CTA debe aparecer ya (o como muy tarde tras el escaneo de salida, que es
  lo que ya pasa). Conviene revisarlo con el cliente: el comportamiento actual encaja
  con un caso real (paquete físicamente sigue en oficina hasta la valija), pero el
  enunciado pide la asignación "automática" tras `RecogidoEnOrigen`.
- La bifurcación oficina vs domicilio en `Clasificacion` del CTA destino **no usa**
  `TipoEntrega`: siempre encadena a `DisponibleParaReparto`. Falta leer el campo del
  envío (cuando se añada) y elegir entre `DisponibleParaReparto` o
  `EntregaOficinaDestino` + crear tarea en la oficina destino.
- Modo `EntregaOficinaDestino` existe en `ScanProcessorService.ProcesarEntregaOficinaDestino`
  pero no está integrado con la asignación automática al operario de oficina destino
  ni con la salida del CTA hacia esa oficina.

### 2.5 Reparto (última milla) — ✅ implementado

[microservicios/Nexopostal/Nexopostal.Reparto/Controllers/RepartoController.cs](microservicios/Nexopostal/Nexopostal.Reparto/Controllers/RepartoController.cs)
tiene endpoints completos:

- `GET/POST/PUT /api/reparto/repartidores`
- `GET/POST /api/reparto/rutas` — crear ruta, listarla por fecha o por repartidor.
- `GET /api/reparto/ruta` — ruta activa del repartidor autenticado (driver-app).
- Endpoints adicionales para iniciar/finalizar ruta, registrar entrega, posición GPS
  (verificados en `FLUJO_ENVIO_PAGO_A_ENTREGA.md`).

**Lo que falta:**

- Conectar el evento `PaqueteDisponibleParaReparto` (SignalR) con la UI del JefeReparto
  para que el paquete aparezca como "pendiente de asignar a ruta" sin pasos manuales
  intermedios. Esto puede ser principalmente cambio de frontend en `intranet-app`.
- Validar que la creación de ruta acepta múltiples paquetes y los marca como
  `AsignadoARuta` al añadirlos.

### 2.6 Apps Angular — parcialmente

- `clientes-app`: tiene flujo de crear envío y pago. **Falta**:
  - Selector de **oficina origen** (carga `oficinas.json` o un endpoint que filtre por CP).
  - Selector de **tipo de entrega** (domicilio / oficina destino).
  - Si tipo = oficina, selector de **oficina destino**.
- `intranet-app`:
  - Existe escáner integrado con buscador por código y modal "Paquete fuera de tus tareas".
  - **Falta**: pantalla para que el operario de oficina dé de alta envíos presenciales.
  - **Falta**: bandeja específica para el JefeReparto con paquetes en estado
    `DisponibleParaReparto` listos para arrastrar a una ruta.
- `driver-app`: ya tiene ruta activa, inicio/fin y entrega. Suficiente para el flujo objetivo.

### 2.7 Estados y enumeraciones — ✅ existen, requieren ampliación

- `EstadoEnvio` (público) y `EstadoInterno` (granular) en
  [Nexopostal.Ciudadano/Models/Envio.cs](microservicios/Nexopostal/Nexopostal.Ciudadano/Models/Envio.cs).
- `TipoTarea` y `EstadoTarea` en `Nexopostal.Intranet.Models` (usados por `AsignacionService`).
- `ModosEscaneo` en `Nexopostal.Intranet.Models` con los modos descritos en 2.4.

**Lo que falta:**

- Añadir estado interno `EnTransitoAOficinaDestino` (entre el CTA destino y la oficina
  destino) para envíos con `TipoEntrega = Oficina`.
- Añadir `TipoTarea.RecepcionOficinaDestino` (o equivalente) para asignar al operario
  de oficina destino.
- Posible nueva incidencia `OficinaIncorrecta` para el caso "este paquete no es de tu oficina".

---

## 3. Resumen de cambios necesarios (delta sobre lo que hay)

### 3.1 Modelo de datos (Ciudadano)

- [ ] Añadir a `Envio`:
  - `int? OficinaOrigenId`
  - `int? OficinaDestinoId`
  - `TipoEntrega TipoEntrega` (`Domicilio` | `Oficina`)
- [ ] Migración EF Core correspondiente.
- [ ] Actualizar `CrearEnvioDto`, `EnvioCreadoDto`, `EnvioTrackingDto` y `AdminEnvioDetalleDto`.

### 3.2 Propagación a Intranet

- [ ] El payload del endpoint interno `POST /api/admision/interno/paquete`
  (`AdmisionPaqueteDto`) debe transportar `OficinaOrigenId`, `OficinaDestinoId` y `TipoEntrega`.
- [ ] Persistir estos datos junto al paquete en Intranet (`AdmisionService` /
  `PaqueteRepository` o estructura equivalente que la cadena de escaneos pueda consultar).

### 3.3 ScanProcessorService

- [ ] `ProcesarRecepcionOficina`: comparar `req.OficinaJsonId` con
  `paquete.OficinaOrigenId`. Si no coinciden, devolver `Exito = false`,
  `Codigo = "OficinaIncorrecta"` y mensaje con datos de la oficina correcta
  (nombre, CP, ciudad). **No** crear historial ni cambiar estado.
- [ ] (Opcional) Tras `RecogidoEnOrigen`, autocrear `TipoTarea.SalidaOficinaACta`
  para el operario de oficina (encadenado, igual que las del CTA), o dejar que el
  operario simplemente lo escanee al meterlo en la valija.
- [ ] `ProcesarClasificacion` (cuando `esUltimaMilla = true`):
  - Leer `TipoEntrega` del paquete.
  - Si `Domicilio` → comportamiento actual (`DisponibleParaReparto`).
  - Si `Oficina` → cambiar estado a `EnTransitoAOficinaDestino`, crear tarea
    `RecepcionOficinaDestino` para el operario de la `OficinaDestinoId` y
    notificar por SignalR a esa oficina.
- [ ] `ProcesarEntregaOficinaDestino`: validar que el escaneo se hace en la oficina
  destino correcta (mismo patrón que 2.3), cerrar tarea y dejar el paquete en
  `DepositadoEnOficina`.

### 3.4 Alta presencial en oficina

- [ ] Nuevo endpoint en Intranet (o en Ciudadano, autorizado a `OperarioOficina`):
  `POST /api/envios/alta-en-oficina` que:
  - Crea el `Envio` ya en estado `RecogidoEnOrigen` (porque está físicamente en mano del operario).
  - Toma la oficina del operario autenticado como `OficinaOrigenId`.
  - Calcula tarifa, registra cobro (efectivo / TPV) y emite recibo PDF.
  - Genera etiqueta y la imprime (descarga PDF en respuesta).
  - Notifica a Intranet (`admision/interno/paquete`) y crea directamente la tarea
    `SalidaOficinaACta` para el propio operario.
- [ ] Pantalla en `intranet-app` para este alta (formulario remitente/destinatario,
  peso, dimensiones, CP destino, tipo de entrega, oficina destino opcional, cobro).

### 3.5 UI clientes-app

- [ ] En el flujo de crear envío, antes del pago:
  - Selector de **oficina origen** (filtrada por la provincia del CP del remitente).
  - Selector de **tipo de entrega**: domicilio (por defecto) u oficina.
  - Si oficina: selector de **oficina destino** (filtrada por CP destino).

### 3.6 UI intranet-app

- [ ] Bandeja del **JefeReparto** que consuma el evento SignalR
  `PaqueteDisponibleParaReparto` y muestre la lista en tiempo real.
- [ ] Acción "Asignar a ruta" que llame al endpoint de creación/edición de ruta
  en `Nexopostal.Reparto` y marque el paquete como `AsignadoARuta`.
- [ ] Mensaje y modal "Oficina incorrecta" en la pantalla de escaneo cuando el
  backend devuelva ese código.

### 3.7 Tipos / enums nuevos

- [ ] `EstadoInterno.EnTransitoAOficinaDestino`.
- [ ] `TipoEntrega` (`Domicilio = 0`, `Oficina = 1`).
- [ ] `TipoTarea.RecepcionOficinaDestino` (o `EntregaOficinaDestino` reutilizada en
  asignaciones, según convenga).
- [ ] (Opcional) `TipoIncidencia.OficinaIncorrecta` si se quiere registrar el caso.

---

## 4. Conclusiones

1. **Lo que ya está listo y se reutiliza tal cual**:
   - Pago Stripe + generación de etiqueta/factura + email.
   - Toda la red CTA: resolución de CTA por CP, movimientos troncales,
     cadena `RecepcionCta → Clasificacion → DespachoTroncal → RecepcionTroncal`.
   - Sistema de asignaciones a operarios con balanceo, SignalR en tiempo real,
     buscador de tarea por código, modal "fuera de mis tareas".
   - Microservicio de reparto completo: repartidores, rutas, registro de entrega,
     posiciones GPS.
   - Tracking público y privado (`NumeroSeguimiento` vs `NumeroExpedicion`).

2. **Lo que hay que añadir (gaps reales del flujo objetivo)**:
   - Persistir `OficinaOrigenId`, `OficinaDestinoId` y `TipoEntrega` en el envío
     y propagarlos a Intranet.
   - Validación "oficina correcta" en `ProcesarRecepcionOficina` y en
     `ProcesarEntregaOficinaDestino`.
   - Bifurcación domicilio/oficina al clasificar en el CTA destino.
   - Flujo de **alta presencial en oficina** (endpoint + UI).
   - Selectores de oficina origen / tipo de entrega / oficina destino en `clientes-app`.
   - Bandeja del JefeReparto en `intranet-app` enganchada a SignalR
     `PaqueteDisponibleParaReparto`.

3. **Lo que conviene revisar antes de tocar nada**:
   - Decidir si la asignación al operario CTA origen se genera ya con `RecogidoEnOrigen`
     o sigue esperando al `SalidaOficinaACta` (operativamente más realista).
   - Decidir si "alta en oficina" cobra siempre presencial o también permite generar un
     enlace de pago Stripe (afecta a si el envío arranca pagado o no).
   - Confirmar mapeo `Oficina → CTA` (se calcula por CP de la oficina, pero si una
     oficina cambia de CTA hay que centralizar la fuente de verdad).

4. **Impacto resumido por capa**:
   - **Backend Ciudadano**: modelo `Envio`, DTOs, controller, migración EF Core.
   - **Backend Intranet**: `AdmisionService`, `ScanProcessorService`, nuevas tareas,
     posiblemente nuevo endpoint de alta presencial.
   - **Backend Reparto**: sin cambios estructurales — solo posibles ajustes para
     consumir `DisponibleParaReparto` desde la UI del JefeReparto.
   - **clientes-app**: selectores y validaciones en el wizard de crear envío.
   - **intranet-app**: pantalla de alta presencial, bandeja JefeReparto,
     mensaje "oficina incorrecta".
   - **driver-app**: sin cambios.

Con estos cambios el flujo objetivo queda cubierto extremo a extremo y reutiliza la
mayor parte del trabajo ya hecho. El esfuerzo se concentra en el modelo de datos del
envío, la validación de oficina y los dos pequeños subflujos nuevos (alta presencial
y entrega en oficina destino).
