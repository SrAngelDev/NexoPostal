# Implementación de CQRS con MediatR en NexoPostal

Fecha: 24/09/2026.

## Decisión y arquitectura

Se aplica la opción elegida: **CQRS con MediatR sobre PostgreSQL y consistencia inmediata**. El documento de referencia del usuario sirve como base conceptual; no se introduce su ejemplo de MongoDB ni replicación asíncrona. Cada microservicio conserva su PostgreSQL, sus repositorios, sus tablas y sus transacciones actuales.

```text
HTTP → controlador (identidad, permisos, validación, respuesta)
     → ISender.Send(Command / Query)
     → pipeline de ejecución
     → manejador tipado
     → servicio de dominio o repositorio existente → PostgreSQL
```

Las consultas posteriores leen la escritura confirmada desde la misma base de datos. Esto no crea transacciones distribuidas entre microservicios ni cambia las garantías de las llamadas externas. CQRS organiza las responsabilidades; no implica por sí solo mayor rendimiento ni obliga a tener dos bases de datos.

## Cambios realizados

- Dependencia **MediatR 12.5.0** fijada en `Nexopostal.Shared.csproj`.
- Contratos `ICommand<T>` e `IQuery<T>`, registro de manejadores y `CqrsExecutionBehavior` en `Nexopostal.Shared/Cqrs`.
- Registro `AddAuthCqrs`, `AddCiudadanoCqrs`, `AddIntranetCqrs` y `AddRepartoCqrs` en los cuatro puntos de arranque.
- **136 solicitudes tipadas: 75 Commands y 61 Queries**, con sus manejadores agrupados por funcionalidad en `Application/` de cada módulo.
- Separación de las interfaces de los 17 servicios existentes en contratos de lectura y escritura. Los manejadores dependen del contrato correspondiente. Las interfaces originales heredan los contratos separados, manteniendo compatibles sus consumidores internos.
- Los contratos separados resuelven la misma instancia del servicio dentro del ámbito de la petición. Se conserva la lógica de negocio existente, sin abrir un segundo contexto de datos ni ejecutar operaciones en paralelo.
- Extracción de la lógica de perfil, seguimiento/creación de envíos y pagos desde los controladores de Ciudadano hacia manejadores que devuelven DTO o resultados tipados, sin depender de `HttpContext` o `IActionResult`.
- Adaptación de las pruebas de controladores para atravesar MediatR real, conservando sus simulaciones de los servicios y sus comprobaciones.

### Alcance por módulo

| Módulo | Funcionalidades incorporadas |
| --- | --- |
| Auth | Registro, login, refresh, contraseña, recuperación, perfil de identidad y administración de usuarios |
| Ciudadano | Administración de envíos, creación online, tracking público, mis envíos, perfil, agenda de direcciones y pagos Stripe |
| Intranet | Admisión, asignaciones/tareas, movimientos, incidencias, historial, clasificación/CTA, operarios, consultas de oficinas, escaneo y difusión de notificaciones |
| Reparto | Repartidores, rutas, entregas, ubicaciones, paneles, vehículos y bandeja de pendientes |

Se modifican **19 controladores**. La adopción es por casos de uso: los cálculos síncronos de tarifas, catálogos JSON, generación/descarga de documentos, tareas en segundo plano y determinadas operaciones internas de Ciudadano conservan sus adaptadores actuales. No se afirma que toda llamada interna del proyecto atraviese MediatR. Los servicios siguen siendo reutilizables por otros servicios y procesos.

### Compatibilidad preservada

- Mismas URLs, verbos, parámetros de acciones, roles y atributos de autorización.
- Mismos DTO, mensajes, respuestas HTTP y reglas de pertenencia al usuario/oficina/CTA.
- Validación MVC/FluentValidation y comprobaciones de identidad antes del despacho.
- Mismos repositorios y orden de persistencia, notificaciones, PDFs y correo.
- YARP, los tres frontends, configuración de infraestructura y esquema de base de datos no necesitan cambios.
- Sin migraciones, nuevas variables de entorno ni servicios adicionales.

En pagos, **`VerificarPagoCommand` es un Command aunque la URL existente sea GET**, porque puede confirmar el pago, actualizar el envío y emitir notificaciones. El webhook conserva la comprobación de firma en el controlador y despacha una confirmación tipada; se conserva también su política actual de respuesta y la omisión de confirmaciones ya procesadas. Se mantiene la persistencia del pago antes de PDFs/correo y el comportamiento de mejor esfuerzo de estos últimos. No se añaden reintentos automáticos que pudieran repetir efectos externos.

El pipeline registra únicamente tipo de solicitud, clasificación y duración, sin serializar payloads, contraseñas, tokens o datos personales. Esto se refiere al nuevo pipeline; no elimina los registros de negocio preexistentes. Propaga los errores y comprueba la cancelación antes de ejecutar. Los servicios antiguos no aceptan todos un token de cancelación: no se promete interrumpir transacciones u operaciones externas ya iniciadas.

## Verificación

La revisión de sintaxis compara los atributos HTTP y parámetros de las acciones de los 19 controladores con la versión anterior, y los tokens de las implementaciones de los 17 servicios de dominio: se conservan.

Se añaden seis pruebas:

1. Ejecución única y ausencia de payload sensible en los registros del pipeline.
2. Propagación del error original sin reintentos ni registro del mensaje sensible de la excepción.
3. Cancelación previa sin invocar el manejador.
4. Clasificación exclusiva Command/Query y un único manejador registrado para cada solicitud de aplicación.
5. Creación/actualización parcial de perfil visible desde otro ámbito de petición, contra PostgreSQL real.
6. Aislamiento entre propietarios de direcciones y visibilidad inmediata del borrado, contra PostgreSQL real.

El flujo de integración existente de Ciudadano comprueba además crear un envío y consultarlo inmediatamente mediante tracking y mis envíos; ahora atraviesa los nuevos manejadores.

Comandos reproducibles desde la raíz:

```powershell
dotnet test microservicios/Nexopostal/Nexopostal.Tests/Nexopostal.Tests.csproj --filter "FullyQualifiedName!~IntegrationTests"
dotnet test microservicios/Nexopostal/Nexopostal.Tests/Nexopostal.Tests.csproj --filter "FullyQualifiedName~IntegrationTests"
```

Resultado local: **935 pruebas correctas, 0 fallidas, 0 omitidas**, sin base de datos. Docker Desktop no pudo iniciar por un error de acceso a su socket `sailor-ingest.sock`; no se restableció Docker ni se eliminaron sus datos. La ejecución de integración con PostgreSQL y las pruebas E2E quedan a cargo de los runners del pipeline existente y son obligatorias antes del despliegue.

Persisten los avisos de dependencias anteriores en MailKit/MimeKit, Microsoft.OpenApi y dependencias de pruebas. No se han silenciado ni se han deshabilitado pruebas.

## Publicación y producción

La publicación utiliza el flujo existente: pruebas unitarias, integración PostgreSQL, compilación de las tres aplicaciones Angular, construcción de ocho imágenes, 46 pruebas E2E contra esas imágenes y despliegue de infraestructura por SHA. No se modifica ni se salta ninguna puerta de validación.

Estado: pendiente de registrar el resultado del pipeline y la comprobación de producción.

Los cambios se preparan en una copia aislada basada en `origin/master`, separada de los cambios locales del panel administrativo. El resultado se trasladará a la carpeta original mediante un parche comprobado, preservando esos cambios.

## Reversión

La imagen anterior verificada es `9320e0d698127fd57737dbf610e02dee1fcfacb4` (YARP). Para restaurarla mediante el mecanismo de infraestructura:

```powershell
gh workflow run deploy.yml --repo SrAngelDev/GasanzTech-CI-CD --ref master -f nexopostal_image_tag=9320e0d698127fd57737dbf610e02dee1fcfacb4
```

Es una instrucción de recuperación; no se ejecuta durante esta entrega. No es necesario modificar ni borrar datos. Después se debe comprobar el workflow y las APIs públicas/protegidas.

## Añadir un caso de uso

1. Definir un record `ICommand<T>` si produce efectos, o `IQuery<T>` si solo lee.
2. Implementar su manejador en el módulo propietario. Mantener el ámbito por petición y evitar reintentos de efectos externos.
3. En el controlador, conservar permisos/validación y enviar la solicitud con `RequestAborted`; convertir el resultado de aplicación a la respuesta HTTP.
4. Probar la conducta observable, incluyendo pertenencia, errores y visibilidad de escrituras cuando corresponda.

Referencias: [CQRS de Microsoft](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs), [MediatR 12.5.0](https://www.nuget.org/packages/MediatR/12.5.0).
