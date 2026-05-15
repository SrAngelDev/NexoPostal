# Analisis funcional integral de NexoPostal

Fecha: 2026-05-15

Este documento resume el estado funcional del sistema (frontend, backend y operativa), detalla brechas y propone mejoras. Esta version incorpora los avances recientes en pricing, reparto y tracking en tiempo real.

## Alcance
- Apps Angular: clientes-app, intranet-app, driver-app.
- Microservicios .NET: Auth, Ciudadano, Intranet (Logistica), Reparto.
- Gateway y Nginx como capa de entrada.
- SignalR para tracking en tiempo real.

Nota: este documento no incluye credenciales ni secretos.

## Funcionalidad implementada

### Clientes (web)
- Envio: wizard con datos de paquete, remitente/destinatario y pago.
- Tarifas: consumo del backend mediante endpoint unico (sin simulador local en cliente).
- Tracking: consulta publica por numero de seguimiento + UI de estados.
- Panel usuario: perfil, direcciones favoritas, listado de envios.
- Oficinas: busqueda por CP o direccion, listado y detalles.

### Ciudadano (API)
- Envios: cotizar, crear, tracking publico, listado del usuario.
- Documentos: etiqueta y factura en PDF.
- Envios internos: detalle por expedicion, listado interno, actualizar estado interno.
- Perfil: get/guardar perfil y agenda de direcciones.
- Tarifas: motor unico (TarifasService) reutilizado en tarifas, envios y pagos.
- Tracking realtime: Hub + servicio de notificaciones con soporte de latitud/longitud.
- Integracion interna de reparto: endpoint POST /api/envios/interno/tracking/ubicacion para publicar ubicacion de reparto al tracking del cliente.

### Intranet (Logistica)
- Admision de paquetes (incluye endpoint interno para inter-servicios).
- Asignaciones de tareas por CTA y operario.
- Movimientos troncales (CTA origen/destino).
- Incidencias (alta y ciclo de vida).

### Reparto (API)
- Repartidores: crear, listar, mi perfil.
- Rutas: crear, listar, detalle, iniciar y finalizar.
- Entregas: agregar a ruta, listar por ruta/seguimiento, confirmar y registrar.
- Confirmaciones con evidencia: receptor, observaciones, firma/foto y coordenadas de entrega.
- Dashboard de reparto.
- Ubicacion: endpoint operativo e integrado con Ciudadano para tracking realtime (por numero de seguimiento o por ruta).

### Driver app (repartidores)
- Login y dashboard operativo.
- Ruta activa: carga de ruta asignada, listado/secuenciacion de paradas y resumen de progreso.
- Ciclo de ruta: iniciar y finalizar ruta desde la app (incluye observaciones de cierre).
- Entregas: confirmacion con estados (entregado, ausente, rechazado, etc.) y evidencia de entrega.
- GPS: envio periodico de ubicacion al backend cuando la ruta esta en curso.
- Offline: cola local de confirmaciones y ubicaciones con reintento automatico al reconectar.
- Escaneo de codigo de barras (camara y entrada manual) + consulta interna por expedicion.

## Estado de prioridades (actualizacion)

### Prioridad 1: Repartidores
- Estado: implementada en gran parte.
- Hecho: integracion driver-app con Reparto, vista de ruta, confirmacion de entregas, inicio/fin de ruta, GPS y cola offline.
- Pendiente: mejorar experiencia de mapa/navegacion y endurecer el seguimiento en segundo plano.

### Prioridad 2: Tracking realtime
- Estado: parcialmente implementada.
- Hecho: publicacion de ubicacion de reparto hacia tracking del cliente via Ciudadano + SignalR.
- Pendiente: cerrar sincronizacion completa de eventos de entrega con el estado publico del envio.

### Prioridad 3: Pricing unico
- Estado: implementada.
- Hecho: motor unico de tarifas en backend y consumo de esa fuente desde la web.

### Prioridad 4: Robustez
- Estado: pendiente.
- Incluye: refresh token real, observabilidad basica, hardening de seguridad interna y mas tests de integracion.

## Faltantes y brechas actuales

### Reparto / Driver app
- Falta capa de mapa/navegacion real (actualmente hay secuencia y progreso, no guiado cartografico).
- El tracking depende de la sesion activa de la app; no hay estrategia robusta de background para movilidad prolongada.
- La evidencia de entrega requiere evolucion a almacenamiento/gestor documental centralizado.

### Tracking en tiempo real
- Ubicacion de reparto ya integrada en realtime para cliente.
- Falta consolidar una orquestacion unica para que todos los eventos operativos (entrega/fallo/devolucion) impacten de forma consistente en estado publico + realtime.

### Autenticacion y seguridad interna
- Endpoint de refresh token continua como placeholder.
- Pendiente reforzar autenticacion/autorizacion servicio a servicio en endpoints internos.

### Orquestacion operativa
- No hay generacion automatica de rutas/entregas a partir de la admision.
- El flujo end-to-end existe por piezas, pero no esta totalmente automatizado.

## Detalle funcional por dominio

### Clientes
- Implementado: altas de envio, pago, tracking, perfil, direcciones y tarifas consumidas desde backend.
- Falta: resiliencia de pagos (reintentos guiados), push reales y mejoras UX de seguimiento.

### Intranet / Logistica
- Implementado: admision, asignaciones, movimientos troncales, incidencias.
- Falta: automatizar el traspaso operativo hacia reparto para cerrar ciclo sin pasos manuales.

### Reparto
- Implementado: API de rutas y entregas, evidencia, inicio/fin de ruta y ubicacion integrada con tracking cliente.
- Falta: sincronizacion completa con estados publicos de Ciudadano en todos los resultados de entrega.

### Driver app
- Implementado: login, dashboard, escaneo, ruta activa, confirmacion de entregas, GPS y modo offline con reintentos.
- Falta: navegacion en mapa y robustez de tracking en segundo plano.

## Backlog propuesto (foco actual)

1) Sincronizacion estado publico
- Propagar automaticamente confirmaciones de Reparto a estado interno/publico en Ciudadano.
- Unificar eventos realtime para entrega correcta, intento fallido, devolucion e incidencias.

2) Experiencia de ruta avanzada
- Añadir mapa basico con paradas y progreso espacial.
- Incorporar estimaciones ETA por parada y alertas de desvio.

3) Tracking y offline de nivel produccion
- Estrategia de background tracking (movil/PWA) y politicas de bateria.
- Reintentos con backoff, deduplicacion y telemetria de colas.

4) Robustez transversal
- Implementar refresh token real.
- Añadir observabilidad (logs estructurados, metricas, trazas distribuidas).
- Extender tests de integracion E2E en flujos criticos.

## Mapa de endpoints (referencia actualizada)

Ciudadano:
- POST /api/envios/cotizar
- POST /api/envios/crear
- GET  /api/envios/track/{numero}
- GET  /api/envios/mis-envios
- GET  /api/envios/factura/{numero}
- GET  /api/envios/etiqueta/{numero}
- GET  /api/envios/interno/{expedicion}
- GET  /api/envios/interno/por-seguimiento/{numero}
- GET  /api/envios/interno/listar
- PUT  /api/envios/interno/{expedicion}/estado
- POST /api/envios/interno/tracking/ubicacion
- GET  /api/etiquetas/{numero}
- GET  /api/perfil
- POST /api/perfil
- GET  /api/perfil/direcciones

Intranet:
- POST /api/admision/paquete
- POST /api/admision/interno/paquete
- POST /api/asignaciones
- GET  /api/asignaciones/mis-pendientes
- PUT  /api/asignaciones/{id}/iniciar
- PUT  /api/asignaciones/{id}/completar
- POST /api/movimientos
- PUT  /api/movimientos/{id}/despachar
- PUT  /api/movimientos/{id}/recibir
- POST /api/incidencias
- PUT  /api/incidencias/{id}

Reparto:
- GET  /api/reparto/mi-perfil
- GET  /api/reparto/ruta
- GET  /api/reparto/rutas
- GET  /api/reparto/rutas/{id}
- POST /api/reparto/rutas/{id}/iniciar
- POST /api/reparto/rutas/{id}/finalizar
- GET  /api/reparto/entregas?rutaId=
- GET  /api/reparto/entregas?seguimiento=
- POST /api/reparto/confirmar?entregaId=
- PUT  /api/reparto/entregas/{entregaId}/registrar
- POST /api/reparto/ubicacion

Gateway (uso driver-app):
- POST /api/nexopostal/reparto/ruta-iniciar/{id}/iniciar
- POST /api/nexopostal/reparto/ruta-finalizar/{id}/finalizar

## Proximos pasos recomendados

1) Conectar confirmaciones de entrega de Reparto con actualizacion de estado interno/publico en Ciudadano de forma automatica.
2) Añadir mapa operativo en driver-app y mejorar estrategia de tracking en segundo plano.
3) Implementar refresh token y hardening de endpoints internos entre microservicios.
4) Introducir observabilidad y pruebas E2E para flujos de reparto/tracking/pagos.

---
Fin del documento.
