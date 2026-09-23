# Migración del API Gateway a YARP

Fecha: 23 de septiembre de 2026.

## Objetivo y punto de partida

Sustituir `AspNetCore.ApiGateway 6.0.0` por `Yarp.ReverseProxy 2.3.0` manteniendo las URLs que ya consumen clientes-app, intranet-app y driver-app, sus permisos y el proceso de publicación a producción.

La migración parte de `origin/master`, commit `3f29080f84788515e4da97fa1a16529bb242093c`, que ya contiene las correcciones de concurrencia de los tests de Reparto, el locator del footer en E2E y el despliegue delegado en `SrAngelDev/GasanzTech-CI-CD`. Se ha trabajado en una copia Git aislada (`codex/migrate-gateway-yarp`) para preservar los cambios locales preexistentes, informes y documentos del usuario.

## Análisis realizado

- Configuración y arranque del gateway; reescrituras, route keys, propagación de errores, JWT, consulta de cuentas bloqueadas, CORS y controladores proxy.
- Rutas, verbos y autorización de Auth, Ciudadano, Intranet y Reparto, contrastados con los servicios HTTP de las tres aplicaciones Angular.
- Alias de perfil, direcciones, estado de envíos, rutas de reparto y APIs operativas; descargas de etiquetas/facturas; webhook de Stripe y consultas Nominatim.
- Nginx, Compose, Dockerfile, pruebas del backend y de navegador, workflow de publicación y workflow de infraestructura que recibe la versión validada.

## Implementación

### Transporte y routing

`Program.cs` registra `AddReverseProxy` y publica `MapReverseProxy`. `YarpGatewayConfig` configura destinos a partir de las mismas claves `Microservices:Auth`, `Ciudadano`, `Logistica` y `Reparto`. Se mantienen los nombres de servicio, puertos y variables `Microservices__...` del despliegue. Se resuelven también placeholders `${VARIABLE}`. No se necesita cambiar secretos ni bases de datos.

Las rutas REST se agrupan por prefijo y admiten todos los verbos; cada microservicio sigue validando los métodos y permisos de sus endpoints:

| Prefijos (con `/api/` o `/api/nexopostal/`) | Destino |
| --- | --- |
| `auth` | Auth |
| `envios`, `pagos`, `perfil`, `tarifas`, `oficinas` | Ciudadano |
| `operativa`, `admision`, `scan`, `asignaciones`, `movimientos`, `incidencias`, `historial`, `ctas`, `operarios`, `oficinaspostales` | Logística |
| `reparto` | Reparto |

YARP transforma exclusivamente el path de salida; no convierte los IDs en query strings ni reescribe `Request.Path`. Preserva método, cuerpo, query string, Authorization y las respuestas HTTP del destino, incluyendo binarios, tipos de contenido, cabeceras y errores. La selección de rutas no depende de IDs numéricos ni de una route key global.

### Compatibilidad de URLs

`LegacyGatewayRoutes` conserva el inventario anterior como una tabla de compatibilidad. Las nuevas rutas REST dentro de un prefijo existente no requieren añadir route keys a esa tabla.

| URL existente | Ruta del microservicio |
| --- | --- |
| `POST /api/nexopostal/reparto/crear-ruta` | `POST /api/reparto/rutas` |
| `GET /api/nexopostal/perfil/get` | `GET /api/perfil` |
| `POST /api/nexopostal/perfil/guardar` | `POST /api/perfil` |
| `POST /api/nexopostal/perfil/agregar-direccion` | `POST /api/perfil/direcciones` |
| `PUT /api/nexopostal/perfil/editar-direccion/{id}` | `PUT /api/perfil/direcciones/{id}` |
| `DELETE /api/nexopostal/perfil/eliminar-direccion/{id}` | `DELETE /api/perfil/direcciones/{id}` |
| `PUT /api/nexopostal/envios/interno-estado/{exp}/estado` | `PUT /api/envios/interno/{exp}/estado` |
| `GET /api/Gateway/envios/track?parameters=NXP-123` | `GET /api/envios/track/NXP-123` |

Se preservan también los aliases de operaciones y las URLs `/api/Gateway/{api}/{routeKey}`. Solo en este último contrato se interpreta y retira `parameters`; las rutas REST normales mantienen la query original. La URL canónica `POST /api/reparto/rutas` funciona a la vez que `GET /api/reparto/rutas`, y las acciones sobre IDs pasan directamente al destino.

### Seguridad y adaptadores conservados

- Las rutas YARP requieren JWT por defecto. Las excepciones públicas se limitan a los recursos públicos ya existentes y a su verbo HTTP.
- Continúan la validación de firma, emisor, audiencia, caducidad y consulta de cuentas bloqueadas a Auth. Se conservan los códigos JSON `UNAUTHORIZED` y `USER_BLOCKED`; los challenges incluyen `WWW-Authenticate: Bearer`.
- Se conserva la lista de orígenes CORS y el preflight se procesa antes de exigir autenticación.
- Los controladores proxy específicos tienen prioridad sobre los prefijos YARP. Se conservan sus contratos y restricciones por roles, incluyendo administradores, administración de repartidores, búsqueda de asignaciones, consultas públicas, PDFs y Nominatim.
- `AdminUsersProxyController` y `ClientesAdminProxyController` contienen encaminamiento específico entre varios microservicios; no se sustituyen por un wildcard que perdería ese comportamiento. Los demás adaptadores existentes también se conservan para limitar cambios de contrato en esta entrega.
- Las conexiones SignalR siguen las rutas directas de Nginx ya existentes. No se cambia su transporte.

La migración elimina la dependencia antigua por completo; conservar estos adaptadores no depende de `AspNetCore.ApiGateway`.

### Código retirado

- `ApiOrchestrationConfig` y sus registros `.AddRoute`.
- `UrlRewriteMiddleware`, sus heurísticas de IDs y la conversión general a `parameters`.
- `ErrorPropagationHandler`, que sustituía errores por falsos HTTP 200.
- `GatewayErrorMiddleware`, que almacenaba la respuesta completa para reconstruir el error real.
- `GatewayApiException`, sin consumidores productivos.
- El registro global del handler sobre todos los HttpClient.

Los tests que verificaban esos mecanismos retirados se sustituyen por pruebas del contrato HTTP real de YARP. Se conservan los tests de los adaptadores y de validación de sesiones.

## Verificación local

El proyecto compila con .NET 10. Se han ejecutado satisfactoriamente:

- **93 pruebas de gateway**, incluidas pruebas nuevas que arrancan el `Program.cs` real, firman JWT de prueba y envían tráfico a un servidor HTTP local real.
- **961 pruebas de backend en total: 961 correctas, 0 fallidas, 0 omitidas**, con las pruebas de integración sobre PostgreSQL efímero mediante Testcontainers.

Comandos reproducibles desde la raíz:

```powershell
dotnet test microservicios/Nexopostal/Nexopostal.Tests/Nexopostal.Tests.csproj --filter "FullyQualifiedName~Gateway"
dotnet test microservicios/Nexopostal/Nexopostal.Tests/Nexopostal.Tests.csproj
```

Las comprobaciones HTTP cubren verbos compartiendo ruta, IDs y acciones, alias del frontend, caracteres Unicode, parámetros repetidos/codificados, reenvío JWT, acceso público, cuentas bloqueadas, roles administrativos, CORS, cuerpo y firma de webhook sin modificaciones, PDFs binarios y propagación de HTTP 201/204/400/401/403/404/409/422/500.

La restauración muestra avisos de vulnerabilidades preexistentes en dependencias de otros módulos (entre ellas MailKit/MimeKit y Microsoft.OpenApi) y herramientas de pruebas. Esta entrega no actualiza esas dependencias ajenas a la migración. No se han silenciado los avisos ni deshabilitado pruebas.

## Publicación y producción

Se utiliza el pipeline existente, sin omitir puertas de validación:

1. Pruebas unitarias y de integración del backend.
2. Compilación de producción de las tres aplicaciones Angular.
3. Construcción y publicación de las ocho imágenes con etiqueta del SHA del commit.
4. Pruebas E2E contra el stack completo de esas imágenes.
5. Solicitud `deploy-nexopostal` al repositorio `SrAngelDev/GasanzTech-CI-CD` con el SHA validado.
6. Actualización de contenedores por el runner de infraestructura y comprobaciones posteriores.

Estado de la publicación y evidencias de producción: se completarán tras finalizar la ejecución del pipeline.

## Reversión

No hay migraciones de datos asociadas a este cambio. La versión anterior es `3f29080f84788515e4da97fa1a16529bb242093c`.

Para volver a ella mediante el mecanismo oficial de infraestructura:

```powershell
gh workflow run deploy.yml --repo SrAngelDev/GasanzTech-CI-CD --ref master -f nexopostal_image_tag=3f29080f84788515e4da97fa1a16529bb242093c
```

Ese comando es una instrucción de recuperación; no se ejecuta como parte de la migración. Después debe comprobarse el resultado del workflow y las APIs públicas/protegidas. No se deben eliminar volúmenes ni recrear las bases de datos para revertir el gateway.

## Referencias

- [Configuración de YARP](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/config-files)
- [Autenticación y autorización](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/authn-authz)
- [Transformaciones de solicitudes y respuestas](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/transforms)
