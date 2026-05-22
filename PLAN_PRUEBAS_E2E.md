# Plan de pruebas E2E — Flujo `TipoEntrega` (F10)

Pruebas manuales para validar el flujo revisado de envíos (F1–F9). Todas se
ejecutan contra el entorno local con `docker-compose up` y los tres frontends
(`clientes-app`, `intranet-app`, `driver-app`) en `ng serve`.

## Pre-requisitos

- Migraciones aplicadas: `AddOperarioOficinaTareasYAlta`,
  `AddTipoEntregaYOficinas` (Ciudadano e Intranet).
- Datos seed: oficinas postales en Madrid, Barcelona, Bilbao, Sevilla; al menos
  un CTA por ciudad; un `OperarioOficina` por oficina; un `OperarioLogistico`
  por CTA; un repartidor (driver) vinculado al CTA de destino.
- Stripe en modo test con webhook apuntando al gateway local.

## Escenario 1 — Madrid → Barcelona, **Domicilio**, pago online

1. Cliente entra en `clientes-app/envio-paquete`, rellena datos, marca
   destinatario "A domicilio".
2. Pulsa "Pagar" → Stripe Checkout → tarjeta test → vuelve a la app.
3. **Verificar (Ciudadano DB)**: `Envios` con `TipoEntrega=0` (Domicilio),
   `OficinaOrigenId=null`, `OficinaDestinoId=null`, `EstadoPago=Pagado`.
4. **Verificar (Intranet DB)**: `Envios` creado con mismos campos. Estado
   inicial `PendienteAdmision` → `Admitido`. Existe parada de reparto en
   microservicio Reparto.
5. **Verificar (driver-app)**: el repartidor del CTA Barcelona ve la parada.

## Escenario 2 — Madrid → Madrid, **Oficina**, pago online

1. Cliente en `envio-paquete` selecciona destinatario "Recogida en oficina",
   busca CP destino, selecciona oficina.
2. Paga.
3. **Verificar (Intranet DB)**: `Envios.TipoEntrega=1` (Oficina),
   `OficinaDestinoId=<id seleccionado>`. **NO** existe parada de reparto.
4. Operario CTA destino escanea el paquete (`intranet-app/escaneo`).
5. **Verificar**: estado pasa a `ListoParaRecogidaEnOficina`. Se crea tarea
   `EntregaEnOficinaPendiente` para `OperarioOficina` de la oficina destino.
6. **Verificar (driver-app)**: NO aparece este envío.

## Escenario 3 — Bilbao → Sevilla, **alta presencial Oficina**

1. `OperarioOficina` de Bilbao entra en `intranet-app`, dashboard → "Alta
   presencial".
2. Rellena remitente + destinatario, marca TipoEntrega=Oficina, busca oficinas
   Sevilla por CP, selecciona una.
3. Submit → respuesta con `numeroSeguimiento`, `numeroExpedicion`,
   `costeCalculado`, `tipoEntrega=Oficina`, `ctaDestinoCodigo`.
4. **Verificar (Intranet DB)**: Envio con `OficinaOrigenId=<Bilbao>`,
   `OficinaDestinoId=<Sevilla>`, `TipoEntrega=Oficina`.
5. **Verificar (Ciudadano DB)**: existe registro reverso vía
   `ICiudadanoEnvioLookupService`, consultable desde la app de cliente con el
   código de seguimiento.
6. Flujo normal de escaneos hasta CTA Sevilla → tarea para oficina destino.

## Escenario 4 — Error `OFICINA_INCORRECTA`

1. Envío Oficina con destino oficina Sevilla, pero el repartidor lo lleva al
   CTA equivocado y se escanea allí.
2. **Verificar**: respuesta del escaneo contiene mensaje `OFICINA_INCORRECTA`
   y el estado del envío NO avanza. Operario es informado en
   `escaneo.component`.

## Resultado esperado

Todos los escenarios deben pasar sin intervención manual en la base de datos.
Cualquier desviación se documenta como bug en `dev` antes del merge a `master`.
