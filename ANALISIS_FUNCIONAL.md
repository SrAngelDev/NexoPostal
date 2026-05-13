# Analisis funcional integral de NexoPostal

Fecha: 2026-05-13

Este documento resume el estado funcional del sistema (frontend, backend y operativa), detalla brechas y propone mejoras, con foco especial en la app de repartidores.

## Alcance
- Apps Angular: clientes-app, intranet-app, driver-app.
- Microservicios .NET: Auth, Ciudadano, Intranet (Logistica), Reparto.
- Gateway y Nginx como capa de entrada.
- SignalR para tracking en tiempo real.

Nota: este documento no incluye credenciales ni secretos.

## Funcionalidad implementada

### Clientes (web)
- Envio: wizard con datos de paquete, remitente/destinatario, tarifas locales y pago.
- Tracking: consulta publica por numero de seguimiento + UI de estados.
- Panel usuario: perfil, direcciones favoritas, listado de envios.
- Oficinas: busqueda por CP o direccion, listado y detalles.

### Ciudadano (API)
- Envios: cotizar, crear, tracking publico, listado del usuario.
- Documentos: etiqueta y factura en PDF.
- Envios internos: detalle por expedicion, listado interno, actualizar estado interno.
- Perfil: get/guardar perfil y agenda de direcciones.
- Tarifas: consulta y calculo basico.
- Tracking realtime: Hub + servicio de notificaciones.

### Intranet (Logistica)
- Admision de paquetes (incluye endpoint interno para inter-servicios).
- Asignaciones de tareas por CTA y operario.
- Movimientos troncales (CTA origen/destino).
- Incidencias (alta y ciclo de vida).

### Reparto (API)
- Repartidores: crear, listar, mi perfil.
- Rutas: crear, listar, iniciar, finalizar, detalle.
- Entregas: agregar a ruta, listar, confirmar/registrar.
- Dashboard de reparto.
- Ubicacion: endpoint presente (pendiente de integracion en tiempo real).

### Driver app (repartidores)
- Login y dashboard basico.
- Escaneo de codigo de barras (camara y entrada manual).
- Consulta interna por numero de expedicion.
- Actualizacion de estado interno del envio.

## Faltantes y brechas

### Reparto / Driver app (principal gap)
- La app de repartidores no consume el microservicio Reparto (rutas/entregas). Solo actualiza estados internos de envios.
- No hay vista de ruta asignada, listado de paradas o secuenciacion.
- No hay flujo de confirmacion por entrega vinculado a una ruta.
- Sin GPS en tiempo real (el endpoint de ubicacion esta sin integrar).
- Sin prueba de entrega (firma/foto/ubicacion) asociada a la entrega.
- Sin modo offline ni colas de reintentos para repartidores.

### Tracking en tiempo real
- Existe Hub y servicio de notificaciones, pero no se ve orquestacion clara desde cambios de estado interno.
- Falta integrar eventos de reparto (entrega, ubicacion) con SignalR para el cliente.

### Tarifas y pricing
- La web calcula tarifas localmente con logica simulada.
- El backend tiene otra logica estatica.
- Falta motor unico de tarifas con misma fuente para web y API.

### Autenticacion
- Endpoint de refresh token esta como placeholder.

### Orquestacion operativa
- No hay generacion automatica de rutas/entregas a partir de la admision.
- Las piezas existen, pero el flujo end-to-end esta incompleto.

## Mejoras priorizadas (resumen)

### Prioridad 1: Repartidores
- Integrar driver-app con Reparto: mi perfil, ruta activa, entregas, confirmar entrega.
- Añadir vista de ruta (orden, mapa basico, checklist, tiempos).
- Implementar GPS en background con envio periodico al backend.
- Vincular confirmaciones de entrega con actualizacion de estados publicos.

### Prioridad 2: Tracking realtime
- Emitir eventos SignalR desde cambios de estado internos y reparto.
- Publicar ubicacion del repartidor para el seguimiento del cliente.

### Prioridad 3: Pricing unico
- Mover calculo de tarifas al backend (endpoint unico).
- Reemplazar logica simulada en la web.

### Prioridad 4: Robustez
- Refresh token real.
- Observabilidad basica (logs estructurados, metricas, trazas).
- Tests de integracion en endpoints criticos.

## Detalle funcional por dominio

### Clientes
- Implementado: altas de envio, pago, tracking, perfil y direcciones.
- Falta: tarifas reales, reintentos de pago guiados, estado de pagos mas robusto, notificaciones push reales.

### Intranet / Logistica
- Implementado: admision, asignaciones, movimientos troncales, incidencias.
- Falta: unificacion con reparto para cerrar el ciclo operativo (asignar rutas y entregas automaticamente).

### Reparto
- Implementado: API de rutas y entregas.
- Falta: consumo real desde driver-app, evidencia de entrega, ubicacion realtime.

### Driver app
- Implementado: login, dashboard y escaneo.
- Falta: flujo de ruta y entregas, GPS, offline y evidencia.

## Backlog propuesto (driver-app)

1) Ruta activa
- Obtener mi perfil de repartidor.
- Obtener ruta activa del dia.
- Listar entregas de la ruta.

2) Entregas
- Confirmar entrega con estados (entregado, ausente, rechazo, etc.).
- Adjuntar evidencia (firma/foto) y ubicacion.

3) Seguimiento
- Enviar ubicacion periodica al backend.
- Mostrar progreso de ruta (entregas completadas vs pendientes).

4) Offline
- Cola local de confirmaciones.
- Reintentos automaticos al recuperar conexion.

## Mapa de endpoints (referencia)

Ciudadano:
- POST /api/envios/cotizar
- POST /api/envios/crear
- GET  /api/envios/track/{numero}
- GET  /api/envios/mis-envios
- GET  /api/envios/factura/{numero}
- GET  /api/envios/etiqueta/{numero}
- GET  /api/envios/interno/{expedicion}
- GET  /api/envios/interno/listar
- PUT  /api/envios/interno/{expedicion}/estado
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
- GET  /api/reparto/entregas?rutaId=
- POST /api/reparto/confirmar?entregaId=
- POST /api/reparto/ubicacion   (pendiente de integrar con tracking)

## Proximos pasos recomendados

1) Definir el flujo end-to-end de reparto (admision -> ruta -> entrega -> tracking).
2) Implementar la integracion driver-app con Reparto y GPS.
3) Unificar tarifas en backend y actualizar clientes-app.
4) Activar SignalR en cambios de estado para tracking realtime.

---
Fin del documento.
