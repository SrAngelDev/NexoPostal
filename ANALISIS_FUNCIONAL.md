# Analisis funcional integral de NexoPostal

Fecha: 2026-05-15

Este documento resume el estado funcional del sistema (frontend, backend y operativa), detalla brechas y propone mejoras. Esta version incorpora los avances recientes en pricing, reparto, tracking en tiempo real y orquestacion automatica admision -> ultima milla.

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
- Orquestacion de eventos de reparto: endpoint interno de evento de entrega para sincronizar estado interno/publico y emitir realtime consistente (estado, entrega, incidencia/devolucion).
- Seguridad interna: validacion de X-Service-Key en endpoints internos de tracking.

### Intranet (Logistica)
- Admision de paquetes (incluye endpoint interno para inter-servicios). La admision NO orquesta automaticamente Reparto: registra el paquete y emite SignalR; la ultima milla se materializa en el handler de escaneo `DisponibleParaReparto` (notifica al JefeReparto).
- Asignaciones de tareas por CTA y operario, con flujo encadenado por escaneo (cada handler de `ScanProcessor` cierra la tarea actual y crea la siguiente).
- Buscador de tarea por codigo (`GET /api/asignaciones/buscar?codigo=`) con `modoSugerido` derivado del `TipoTarea`; si el codigo no esta en las tareas del operario, la UI abre un modal bloqueante para reportar incidencia `PaqueteFueraDeTareas`.
- Endpoints de cambio manual de estado (`iniciar`/`completar`/`cancelar`) reservados a `Admin,Supervisor`; la operativa normal de operario cambia estado solo via escaneo.
- Movimientos troncales (CTA origen/destino).
- Incidencias (alta y ciclo de vida) con tipo nuevo `PaqueteFueraDeTareas`.
- Orquestacion automatica hacia Reparto tras admision (cuando llega payload con datos de ultima milla), con respuesta operacional embebida en la admision.

### Auth (Gestion de usuarios Admin)
- Panel de gestion de empleados reservado al rol Admin.
- Listado con filtros por rol, estado de bloqueo y busqueda libre por nombre/email/codigo.
- Bloqueo/desbloqueo de acceso mediante Identity lockout (sin migracion de esquema).
- Cambio de rol inline desde la tabla; el admin no puede modificar su propio rol.
- Restablecimiento de contrasena directo (flujo admin, sin email).
- Alta de nuevos empleados con nombre, email, codigo, rol y contrasena inicial; rol Cliente no admitido.
- Login bloquea acceso a usuarios con lockout activo antes de emitir tokens.

### Reparto (API)
- Repartidores: crear, listar, mi perfil.
- Rutas: crear, listar, detalle, iniciar y finalizar.
- Entregas: agregar a ruta, listar por ruta/seguimiento, confirmar y registrar.
- Confirmaciones con evidencia: receptor, observaciones, firma/foto y coordenadas de entrega.
- Dashboard de reparto.
- Ubicacion: endpoint operativo e integrado con Ciudadano para tracking realtime (por numero de seguimiento o por ruta).
- Sincronizacion operacional: confirmaciones de entrega/fallo/devolucion notifican a Ciudadano para consolidar estado publico + realtime.
- Endpoint interno para auto-asignacion desde admision: crea/reutiliza ruta planificada diaria y agrega entrega con idempotencia por expedicion.

### Driver app (repartidores)
- Login y dashboard operativo.
- Ruta activa: carga de ruta asignada, listado/secuenciacion de paradas y resumen de progreso.
- Ciclo de ruta: iniciar y finalizar ruta desde la app (incluye observaciones de cierre).
- Entregas: confirmacion con estados (entregado, ausente, rechazado, etc.) y evidencia de entrega.
- Mapa operativo en la vista de ruta (Leaflet) con posicion del repartidor, historial de traza y puntos de entrega con coordenadas disponibles.
- Navegacion rapida por parada mediante deep-links a Google Maps y Waze (siguiente parada y parada seleccionada).
- GPS reforzado: watchPosition + heartbeat + ajuste por visibilidad (segundo plano) + reintentos exponenciales con cola offline.
- Offline: cola local de confirmaciones y ubicaciones con reintento automatico al reconectar.
- Escaneo de codigo de barras (camara y entrada manual) + consulta interna por expedicion.

## Estado de prioridades (actualizacion)

### Prioridad 1: Repartidores
- Estado: implementada.
- Hecho: integracion driver-app con Reparto, vista de ruta, confirmacion de entregas, inicio/fin de ruta, mapa operativo, navegacion por parada, GPS endurecido en segundo plano y cola offline.
- Pendiente: evolucionar a capacidades de background de nivel nativo (si el SO suspende el navegador/PWA).

### Prioridad 2: Tracking realtime
- Estado: implementada en gran parte.
- Hecho: publicacion de ubicacion de reparto y sincronizacion de eventos operativos de entrega/fallo/devolucion hacia estado interno/publico + SignalR.
- Pendiente: reforzar resiliencia e idempotencia de la comunicacion entre microservicios para escenarios de fallo transitorio.

### Prioridad 3: Pricing unico
- Estado: implementada.
- Hecho: motor unico de tarifas en backend y consumo de esa fuente desde la web.

### Prioridad 4: Robustez
- Estado: parcialmente implementada.
- Hecho: endpoint refresh token real con rotacion y expiracion; autorizacion inter-servicio mediante X-Service-Key en endpoints internos de tracking.
- Pendiente: hardening adicional (mTLS o JWT entre servicios), observabilidad basica y mas tests de integracion.

### Prioridad 5: Orquestacion operativa admision -> reparto
- Estado: implementada (MVP operativo).
- Hecho: admision en Intranet invoca automaticamente a Reparto para auto-asignar entrega de ultima milla; Reparto selecciona repartidor activo, reutiliza/crea ruta del dia y agrega la entrega.
- Hecho: flujo con idempotencia basica por numero de expedicion para evitar duplicados en reintentos.
- Pendiente: evolucionar a outbox/inbox y reglas avanzadas de balanceo/asignacion por capacidad y geografia.

## Faltantes y brechas actuales

### Reparto / Driver app
- El guiado se apoya en deep-links externos (Google Maps/Waze); no hay motor de navegacion turn-by-turn propio embebido.
- El tracking en segundo plano esta endurecido en contexto web (visibilidad + heartbeat + reintentos), pero sigue sujeto a limitaciones del navegador/SO en segundo plano estricto.
- La evidencia de entrega requiere evolucion a almacenamiento/gestor documental centralizado.

### Tracking en tiempo real
- Ubicacion de reparto ya integrada en realtime para cliente.
- Ya existe orquestacion unica para eventos operativos de reparto (entrega/fallo/devolucion) con impacto consistente en estado publico + realtime.
- Pendiente robustecer reintentos/idempotencia y trazabilidad de eventos entre servicios.

### Autenticacion y seguridad interna
- Refresh token implementado en Auth (rotacion, expiracion y revocacion en cambio de contraseña).
- Endpoints internos de tracking en Ciudadano protegidos con X-Service-Key.
- Pendiente ampliar el modelo de seguridad entre servicios a un esquema mas fuerte (mTLS/JWT de servicio).

### Orquestacion operativa
- Implementada la generacion automatica de entrega/ruta desde admision (Intranet -> Reparto) cuando existen datos minimos de ultima milla.
- Pendiente robustecer la fiabilidad con patrones de mensajeria idempotente (outbox/inbox) y telemetria operacional de reintentos.

## Detalle funcional por dominio

### Clientes
- Implementado: altas de envio, pago, tracking, perfil, direcciones y tarifas consumidas desde backend.
- Falta: resiliencia de pagos (reintentos guiados), push reales y mejoras UX de seguimiento.

### Intranet / Logistica
- Implementado: admision, asignaciones, movimientos troncales, incidencias.
- Implementado: automatizacion de traspaso operativo hacia reparto al admitir paquetes con datos de entrega (seguimiento, direccion, destinatario, telefono).
- Implementado: gestion de usuarios empleados reservada al rol Admin (listado, bloqueo/desbloqueo, cambio de rol, reset de contrasena, alta de empleados).
- Falta: reglas de asignacion mas avanzadas (SLA, carga, zona) y politicas de reintento transaccional.

### Reparto
- Implementado: API de rutas y entregas, evidencia, inicio/fin de ruta y ubicacion integrada con tracking cliente.
- Implementado: sincronizacion de resultados de entrega con estado interno/publico y realtime de Ciudadano.
- Implementado: endpoint interno de auto-asignacion de admision con reutilizacion/creacion de ruta diaria e idempotencia basica por expedicion.
- Falta: robustecer mensajeria inter-servicio ante fallos de red (outbox/inbox, deduplicacion por event-id y observabilidad de colas).

### Driver app
- Implementado: login, dashboard, escaneo, ruta activa, confirmacion de entregas, mapa operativo, navegacion por parada, GPS reforzado y modo offline con reintentos.
- Falta: capacidades de tracking persistente de nivel nativo y navegacion turn-by-turn embebida.

## Backlog propuesto (foco actual)

1) Sincronizacion estado publico
- Añadir estrategia de reintentos idempotentes (outbox/inbox) para eventos Reparto -> Ciudadano.
- Incorporar auditoria completa de eventos de tracking para depuracion operativa.

2) Experiencia de ruta avanzada
- Implementar navegacion turn-by-turn embebida (no solo deep-links externos).
- Incorporar estimaciones ETA por parada y alertas de desvio.

3) Tracking y offline de nivel produccion
- Estrategia de background tracking de nivel nativo/PWA (cuando el sistema suspende pestañas) y politicas de bateria.
- Reintentos con backoff, deduplicacion y telemetria de colas.

4) Robustez transversal
- Endurecer seguridad inter-servicio con autenticacion fuerte (mTLS/JWT entre servicios).
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
- POST /api/envios/interno/tracking/evento-entrega
- GET  /api/etiquetas/{numero}
- GET  /api/perfil
- POST /api/perfil
- GET  /api/perfil/direcciones

Auth:
- POST /api/auth/login
- POST /api/auth/register
- POST /api/auth/refresh
- GET  /api/auth/me
- GET  /api/admin-usuarios
- GET  /api/admin-usuarios/{id}
- PUT  /api/admin-usuarios/{id}/rol
- PUT  /api/admin-usuarios/{id}/bloquear
- PUT  /api/admin-usuarios/{id}/desbloquear
- POST /api/admin-usuarios/{id}/reset-password
- POST /api/admin-usuarios

Intranet:
- POST /api/admision/paquete
- POST /api/admision/interno/paquete
- POST /api/asignaciones
- GET  /api/asignaciones/cta/{ctaId}
- GET  /api/asignaciones/mis-pendientes
- GET  /api/asignaciones/mis-en-progreso
- GET  /api/asignaciones/buscar?codigo=  (operario; 404 si fuera de sus tareas)
- PUT  /api/asignaciones/{id}/iniciar     (Admin/Supervisor)
- PUT  /api/asignaciones/{id}/completar   (Admin/Supervisor)
- PUT  /api/asignaciones/{id}/cancelar    (Admin/Supervisor)
- POST /api/movimientos
- PUT  /api/movimientos/{id}/despachar
- PUT  /api/movimientos/{id}/recibir
- POST /api/incidencias                   (Admin/Supervisor)
- POST /api/incidencias/reportar-fuera-tareas (Admin/Supervisor/OperarioCTA/OperarioOficina)
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
- POST /api/reparto/interno/admision/auto-asignar (interno)

Gateway (uso driver-app):
- POST /api/nexopostal/reparto/ruta-iniciar/{id}/iniciar
- POST /api/nexopostal/reparto/ruta-finalizar/{id}/finalizar

## Proximos pasos recomendados

1) Implantar patron idempotente de eventos (outbox/inbox) para robustecer la sincronizacion Reparto -> Ciudadano.
2) Evolucionar de deep-links a navegacion embebida y reforzar tracking de fondo de nivel nativo/PWA.
3) Evolucionar la seguridad inter-servicio desde service key a mTLS/JWT de servicio.
4) Introducir observabilidad y pruebas E2E para flujos de reparto/tracking/pagos.

---
Fin del documento.
