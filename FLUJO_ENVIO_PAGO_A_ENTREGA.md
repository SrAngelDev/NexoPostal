# Flujo programado de un envio: desde pago hasta entrega

## 1) Resumen ejecutivo

Este documento describe el flujo real implementado en NexoPostal desde que un cliente paga un envio hasta que:

- recibe etiqueta y factura por correo,
- el paquete entra en la red logistica,
- se mueve entre centros,
- y termina en entrega o incidencia.

Tambien incluye el siguiente paso operativo y que hace cada trabajador en un caso Madrid -> Barcelona.

## 2) Flujo tecnico real tras el pago

### Paso 1. Crear sesion de pago

El cliente inicia pago desde la app de clientes.

- Se crea el envio en estado interno `PendientePago`.
- Se crea sesion de Stripe Checkout.
- Se guarda `StripeSessionId` en el envio.

### Paso 2. Pago en Stripe

El cliente paga en Stripe y vuelve a la app por URL de exito.

### Paso 3. Verificacion del pago

El pago se confirma por dos vias:

1. Verificacion activa desde frontend (`/api/pagos/verificar/{sessionId}`).
2. Webhook de Stripe (`/api/pagos/webhook`).

En ambos casos, si el pago esta correcto, se ejecuta el procesamiento de pago exitoso.

### Paso 4. Procesar pago exitoso

El backend actualiza el envio:

- `Pagado = true`
- `FechaPago = now`
- Estado publico `Admitido`
- Estado interno `PendienteRecogida`

### Paso 5. Generar documentos y enviar correo

Se generan:

- Etiqueta PDF
- Factura PDF

Y se envia email al remitente con ambos adjuntos.

Importante:

- El envio de correo es best effort.
- Si falla email/PDF, el pago sigue confirmado y el flujo logistica NO se bloquea.

### Paso 6. Alta automatica en logistica (siguiente paso del sistema)

Despues de confirmar pago, Ciudadano notifica a Intranet por endpoint interno de servicio:

- `POST /api/admision/interno/paquete` con `X-Service-Key`.

Con esto empieza la operativa de red logistica.

## 3) Que pasa justo despues de recibir etiqueta y factura por correo

## Para el cliente

1. Imprimir y pegar la etiqueta en el paquete.
2. Entregar el paquete en oficina de origen o esperar recogida (segun operativa contratada).
3. Seguir el tracking publico con el numero de seguimiento.

## Para operaciones

1. El envio ya esta en `PendienteRecogida` y admitido para operar.
2. Intranet ya conoce la expedicion y el CTA de destino.
3. Se genera (si aplica) movimiento troncal y autoasignaciones.

## 4) Flujo logistico interno (red de trabajo)

### 4.1 Resolucion de CTA y movimiento troncal

Intranet resuelve CTA por prefijo de codigo postal:

- `28` -> CTA-MAD
- `08` -> CTA-BCN

Si CTA origen y CTA destino son distintos:

- se crea movimiento troncal automaticamente,
- y se decide tipo de transporte (terrestre, aereo, maritimo) segun reglas y urgencia.

### 4.2 Autoasignacion interna en Intranet

Tras admision:

- se crea tarea de clasificacion para personal de CTA (idempotente),
- se notifica en tiempo real por SignalR al CTA destino,
- se intenta orquestacion de ultima milla hacia microservicio Reparto.

### 4.3 Autoasignacion de reparto

Reparto recibe solicitud interna y:

1. valida datos minimos,
2. evita duplicados por `NumeroExpedicion` (idempotencia),
3. elige repartidor activo con menor carga,
4. reutiliza ruta planificada del dia o crea una,
5. agrega la entrega a la ruta.

## 5) Que hace cada trabajador en un envio Madrid -> Barcelona

Escenario base:

- origen Madrid (`28xxx`), destino Barcelona (`08xxx`).

### 1. Operario de oficina de origen (Madrid)

- Recibe fisicamente el paquete.
- Escanea en modo `RecepcionOficina`.
- Resultado esperado: estado interno `RecogidoEnOrigen`.

### 2. Operario CTA origen (CTA-MAD)

- Recibe saco/paquete en CTA.
- Escanea `RecepcionCta`.
- Clasifica para salida (`Clasificacion`).
- Despacha troncal (`DespachoTroncal`).

Estados tipicos:

- `RecibidoEnCentroOrigen`
- `ClasificadoParaExpedicion`
- `EnTransitoHaciaCentroDestino`

### 3. Equipo de transporte troncal

- Ejecuta el traslado CTA-MAD -> CTA-BCN.
- En sistema, el movimiento pasa por programado/en transito/recibido.

### 4. Operario CTA destino (CTA-BCN)

- Recibe el movimiento troncal (`RecepcionTroncal`).
- Clasifica para ultima milla (destino final de reparto).
- Deja listo para asignacion/ruta de reparto.

Estado esperado:

- `RecibidoEnCentroDestino` (y despues `AsignadoARuta`).

### 5. Jefe de reparto / asignacion automatica

- Supervisa rutas del dia.
- El sistema puede autoasignar la entrega a un repartidor y ruta.
- Si hace falta, ajusta carga o replanifica.

### 6. Repartidor (Barcelona)

- Inicia ruta (`rutas/{id}/iniciar`).
- Entrega o registra intento (`confirmar` o `entregas/{id}/registrar`).
- Reporta ubicacion en tiempo real (`ubicacion`).
- Finaliza ruta (`rutas/{id}/finalizar`).

Resultados posibles:

- Entregado en domicilio.
- Entregado en punto/oficina.
- Ausente (primer/segundo intento).
- Incidencia (direccion incorrecta, rechazo, etc.).

### 7. Operario de oficina de destino (si aplica)

- Si no hay entrega en domicilio, recibe paquete en oficina.
- Puede quedar en `DepositadoEnOficina` para recogida.

## 6) Como ve esto el cliente (estado publico)

El cliente NO ve el detalle operativo completo. Ve estado publico simplificado:

- `PendientePago`
- `Admitido`
- `EnTransito`
- `EnReparto`
- `EnOficina`
- `Entregado` o `Incidencia` o `Devuelto`

Detras de cada estado publico hay estados internos mas granulares que usan operarios y repartidores.

## 7) Secuencia cronologica recomendada para Madrid -> Barcelona

1. Cliente paga envio (Stripe).
2. Sistema confirma pago, genera etiqueta+factura, envia email.
3. Sistema notifica admision a Intranet.
4. Intranet resuelve CTAs: origen Madrid, destino Barcelona.
5. Se crea movimiento troncal CTA-MAD -> CTA-BCN.
6. Oficina Madrid recibe paquete y lo pasa a CTA-MAD.
7. CTA-MAD clasifica y despacha troncal.
8. CTA-BCN recibe troncal y clasifica destino.
9. Reparto asigna ruta/repartidor en Barcelona.
10. Repartidor sale a reparto, entrega o registra incidencia.
11. Ciudadano actualiza tracking publico y emite eventos realtime.

## 8) Nota tecnica importante (consistencia)

Existe un servicio en segundo plano que tambien puede marcar pagos pendientes como pagados.

- Ese camino marca estados de pago,
- pero no ejecuta todo el flujo rico de documentos + email + notificacion logistica en el mismo paso.

Para operativa completa, la ruta principal es verificacion/webhook que dispara el procesamiento completo.
