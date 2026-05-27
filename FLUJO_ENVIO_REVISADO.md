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

> Actualizado a 2026-05-27 tras los cambios de mayo 2026 en `clientes-app`,
> `intranet-app`, `driver-app`, `Nexopostal.Ciudadano`, `Nexopostal.Intranet`
> y `Nexopostal.Reparto`.

y entrega en oficina destino).
### 2.1 Alta online (cliente paga) — ✅ implementada

- [microservicios/Nexopostal/Nexopostal.Ciudadano/Controllers/EnviosController.cs](microservicios/Nexopostal/Nexopostal.Ciudadano/Controllers/EnviosController.cs)
  y [microservicios/Nexopostal/Nexopostal.Ciudadano/Controllers/PagosController.cs](microservicios/Nexopostal/Nexopostal.Ciudadano/Controllers/PagosController.cs)
  validan `OficinaOrigenId`, `TipoEntrega` y `OficinaDestinoId`.
- El modelo `Envio` en
  [microservicios/Nexopostal/Nexopostal.Ciudadano/Models/Envio.cs](microservicios/Nexopostal/Nexopostal.Ciudadano/Models/Envio.cs)
  **sí** persiste `OficinaOrigenId`, `OficinaDestinoId` y `TipoEntrega`.
- `CrearEnvioDto`, respuestas internas y payloads de pago ya transportan esos campos.
- `clientes-app` ya ofrece selección de oficina origen para el remitente y
  entrega a domicilio u oficina para el destinatario en
  [clientes-app/src/app/pages/envio-paquete/envio-paquete.component.ts](clientes-app/src/app/pages/envio-paquete/envio-paquete.component.ts).
- La admisión interna sigue entrando por
  [microservicios/Nexopostal/Nexopostal.Intranet/Controllers/AdmisionController.cs](microservicios/Nexopostal/Nexopostal.Intranet/Controllers/AdmisionController.cs)
  con `X-Service-Key`.

**Gap real pendiente:** ninguno estructural en datos; lo que queda es endurecer validaciones
de oficina en el escáner y afinar mensajes operativos.

### 2.2 Alta en oficina (operario de oficina) — ✅ implementada

- [microservicios/Nexopostal/Nexopostal.Intranet/Controllers/AdmisionController.cs](microservicios/Nexopostal/Nexopostal.Intranet/Controllers/AdmisionController.cs)
  expone `POST /api/admision/oficina/alta` para `Admin,OperarioOficina`.
- `intranet-app` ya tiene la ruta `/alta-en-oficina`, formulario específico y acceso
  desde dashboard en
  [intranet-app/src/app/app.routes.ts](intranet-app/src/app/app.routes.ts) y
  [intranet-app/src/app/pages/alta-en-oficina/alta-en-oficina.component.ts](intranet-app/src/app/pages/alta-en-oficina/alta-en-oficina.component.ts).
- El flujo presencial crea el envío en Ciudadano, lo admite en Intranet con
  `YaRecogidoEnOrigen=true` y deja autoasignada la tarea `SalidaOficinaACta`
  para el operario de oficina.

### 2.3 Validación "oficina correcta" al escanear — ⚠️ parcial

- En
  [microservicios/Nexopostal/Nexopostal.Intranet/Services/ScanProcessorService.cs](microservicios/Nexopostal/Nexopostal.Intranet/Services/ScanProcessorService.cs),
  `ProcesarRecepcionOficina` **todavía no** compara `req.OficinaJsonId` con
  `OficinaOrigenId`; registra `RecogidoEnOrigen`, cierra `RecepcionOficina` y crea
  `SalidaOficinaACta`.
- `ProcesarEntregaOficinaDestino` ya resuelve `TipoEntrega` y `OficinaDestinoId`,
  pero si la oficina no coincide hoy solo deja un aviso/log y continúa el flujo.

**Gap real pendiente:** convertir ambos casos en validación dura de negocio
(`OficinaIncorrecta` o equivalente), con mensaje claro y sin avanzar estado ni tareas.

### 2.4 Cadena de escaneos y autoasignación — ✅ implementada

El `ScanProcessorService` ya encadena tareas y cierre de trabajo de forma consistente:

| Escaneo                           | Cierra tarea                          | Crea siguiente tarea / efecto |
| --------------------------------- | ------------------------------------- | ----------------------------- |
| `RecepcionOficina`                | `RecepcionOficina`                    | `SalidaOficinaACta` en oficina origen |
| `SalidaOficinaACta`               | `SalidaOficinaACta`                   | `Recepcion` en CTA origen |
| `RecepcionCta`                    | `Recepcion`                           | `Clasificacion` en CTA origen |
| `Clasificacion` (CTA origen)      | `Clasificacion`                       | `DespachoTroncal` si hay troncal |
| `DespachoTroncal`                 | `DespachoTroncal`                     | — |
| `RecepcionTroncal`                | `RecepcionTroncal`                    | `Clasificacion` en CTA destino |
| `Clasificacion` (CTA destino, domicilio) | `Clasificacion`                 | `DisponibleParaReparto` |
| `Clasificacion` (CTA destino, oficina)   | `Clasificacion`                 | `EntregaCtaAOficinaDestino` en oficina destino |
| `DisponibleParaReparto`           | `DisponibleParaReparto`               | Registra el paquete en la bandeja persistente de Reparto y notifica al `JefeReparto` |
| `EntregaOficinaDestino`           | `EntregaCtaAOficinaDestino`           | Si `TipoEntrega=Oficina`, crea `EntregaAlClienteEnOficina` |

La bifurcación domicilio vs oficina **sí** usa `TipoEntrega` en la última milla.
Además, `DisponibleParaReparto` rechaza explícitamente envíos de recogida en oficina
para que no entren por error en reparto.

**Gap real pendiente:** la nomenclatura del flujo ideal y la implementación real no es
idéntica. El documento objetivo habla de `EnTransitoAOficinaDestino`; el código actual
usa `PreparadoParaOficinaDestino` y después `DepositadoEnOficina`.

### 2.5 Reparto (última milla) — ✅ implementado

[microservicios/Nexopostal/Nexopostal.Reparto/Controllers/RepartoController.cs](microservicios/Nexopostal/Nexopostal.Reparto/Controllers/RepartoController.cs)
mantiene endpoints completos de rutas, repartidores, ruta activa, entregas, GPS y,
además, la **bandeja persistente** del `JefeReparto`:

- `GET /api/reparto/bandeja`
- `POST /api/reparto/bandeja/{id}/asignar-a-ruta`
- `GET /api/reparto/rutas`
- `GET /api/reparto/ruta`

El flujo actual del jefe **no vive en `intranet-app`**, sino en `driver-app`:
el paquete entra en la bandeja, el jefe crea o reutiliza una ruta planificada y lo
asigna manualmente.

**Gap real pendiente:** como mejora opcional, falta push directo a la bandeja de Reparto
sin refresco manual (fase 2), aunque el flujo operativo ya es funcional.

### 2.6 Apps Angular — ✅ alineadas con el flujo actual

- `clientes-app`:
  - tiene wizard de creación de envío y pago;
  - ya selecciona oficina origen del remitente;
  - ya permite entrega a domicilio u oficina destino para el destinatario.
- `intranet-app`:
  - mantiene escáner con buscador por código y modal "Paquete fuera de tus tareas";
  - ya incorpora la pantalla de alta presencial en oficina.
- `driver-app`:
  - sigue cubriendo ruta activa, inicio/fin y entrega;
  - ahora también concentra las superficies del `JefeReparto`: `dashboard-jefe`,
    `bandeja-jefe`, `gestion-rutas`, `mapa-tiempo-real`, `asignar-paradas`
    y `mis-repartidores`.

### 2.7 Estados y enumeraciones — ✅ existen y cubren el flujo actual

- `Envio` ya incorpora `OficinaOrigenId`, `OficinaDestinoId` y `TipoEntrega`.
- `ModosEscaneo` ya incluye `DisponibleParaReparto`, `EntregaOficinaDestino`
  y `SalidaAReparto`.
- `TipoTarea` en Intranet ya cubre el flujo real con tareas como
  `RecepcionOficina`, `SalidaOficinaACta`, `Recepcion`, `Clasificacion`,
  `DespachoTroncal`, `RecepcionTroncal`, `DisponibleParaReparto`,
  `EntregaCtaAOficinaDestino` y `EntregaAlClienteEnOficina`.

**Gap real pendiente:** alinear los nombres documentales del flujo ideal con los estados
y tareas realmente expuestos por el código.

---

## 3. Resumen de gaps reales (delta sobre el estado actual)

### 3.1 Validación estricta de oficina

- [ ] Bloquear `RecepcionOficina` si `req.OficinaJsonId` no coincide con `OficinaOrigenId`.
- [ ] Bloquear `EntregaOficinaDestino` si `req.OficinaJsonId` no coincide con `OficinaDestinoId`.
- [ ] Devolver un código de negocio estable (`OficinaIncorrecta` o equivalente) con
  mensaje de la oficina correcta.

### 3.2 Nomenclatura y trazabilidad

- [ ] Decidir si se mantienen los estados actuales (`PreparadoParaOficinaDestino`,
  `DepositadoEnOficina`) o se renombran para parecerse más al flujo ideal del cliente.
- [ ] Mantener sincronizados este documento,
  [FLUJO_ENVIO_PAGO_A_ENTREGA.md](FLUJO_ENVIO_PAGO_A_ENTREGA.md) y
  [microservicios/Nexopostal/Nexopostal.Intranet/SISTEMA_LOGISTICO.md](microservicios/Nexopostal/Nexopostal.Intranet/SISTEMA_LOGISTICO.md)
  si cambia la nomenclatura.

### 3.3 Mejoras opcionales de operación

- [ ] Push en tiempo real de la bandeja del `JefeReparto` desde Reparto para no depender
  de refresco manual.
- [ ] Endurecer la UX del escáner cuando llegue el futuro error `OficinaIncorrecta`.

---

## 4. Conclusiones

1. **Lo que ya está listo y se reutiliza tal cual**:
   - pago, etiqueta, factura, email y admisión interna;
   - persistencia de `OficinaOrigenId`, `OficinaDestinoId` y `TipoEntrega`;
   - alta presencial en oficina;
   - cadena de escaneos con cierre y autoasignación de tareas;
   - bifurcación de última milla por `TipoEntrega`;
   - bandeja persistente del `JefeReparto` en `driver-app`.

2. **Lo que sigue siendo gap real**:
   - validación dura de oficina correcta al escanear;
   - alinear nomenclatura del flujo ideal con estados reales;
   - mejoras opcionales de push/UX en la bandeja del jefe.

3. **Impacto resumido por capa**:
   - **Backend Ciudadano/Intranet**: no faltan modelos ni endpoints base; faltan
     validaciones de negocio y pulido de trazabilidad.
   - **Backend Reparto**: la bandeja y la asignación a ruta ya son operativas.
   - **clientes-app / intranet-app / driver-app**: las pantallas clave ya existen;
     solo quedarían ajustes UX si se endurecen errores y notificaciones.

Con el estado actual, el flujo objetivo está cubierto de extremo a extremo salvo por la
validación estricta de oficina y algunos ajustes de nomenclatura/ergonomía. El documento
ya no debe usarse como lista de grandes desarrollos pendientes, sino como referencia de
estado actual y de esos gaps finales.
