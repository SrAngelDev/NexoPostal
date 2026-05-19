# Memoria TFG - NexoPostal

Este documento es un borrador extenso de la memoria del TFG basado en la implementacion real del proyecto NexoPostal. El texto esta pensado para copiarse a Word y adaptarse con datos personales, capturas de pantalla y formato academico final. Sustituye los campos entre corchetes por tu informacion real.

---

## Portada

Proyecto de desarrollo de aplicaciones web  
Titulo del proyecto: NexoPostal: plataforma integral de paqueteria, trazabilidad y reparto en tiempo real  
Autor: [Nombre y apellidos]  
Tutor: [Nombre y apellidos]  
Ciclo formativo: Desarrollo de Aplicaciones Web  
Curso academico: [2025-2026 o el que corresponda]  
Centro: [Nombre del centro]  

---

# 1. Introduccion

## 1.1 Objetivo

El objetivo principal de este Trabajo Fin de Grado ha sido el analisis, diseño, desarrollo e implantacion de una plataforma software completa para la gestion de servicios de paqueteria. El proyecto recibe el nombre de NexoPostal y nace con la finalidad de reproducir, en un entorno academico pero con criterios tecnicos realistas, el funcionamiento de una empresa de transporte y distribucion postal moderna. La idea central del sistema es cubrir de forma integral todo el ciclo de vida de un envio, desde el momento en que un cliente calcula el precio y realiza el pago hasta la entrega final del paquete por parte de un repartidor, pasando por los procesos internos de admision, clasificacion, movimientos logísticos, control operativo y seguimiento en tiempo real.

El trabajo no se ha limitado a crear una unica aplicacion web, sino que se ha planteado como una solucion distribuida y modular, compuesta por varias interfaces de usuario y varios microservicios especializados. Este enfoque permite representar de manera mas fiel la complejidad de un sistema empresarial real, donde las necesidades de un cliente final no son las mismas que las de un operario de logistica o las de un repartidor de ultima milla. Por ello, NexoPostal se estructura en tres aplicaciones frontend diferenciadas, un gateway de acceso, varios microservicios backend, distintas bases de datos y diversas integraciones externas.

De manera mas concreta, los objetivos especificos del proyecto han sido los siguientes:

- Diseñar una aplicacion para clientes que permita registrarse, iniciar sesion, calcular tarifas, crear envios, pagar online, consultar el estado de los paquetes y gestionar su perfil.
- Desarrollar una intranet operativa orientada a personal interno para gestionar la admision de paquetes, la clasificacion por CTAs, las asignaciones de tareas, el seguimiento interno y el escaneo logistico.
- Implementar una aplicacion para repartidores enfocada a la gestion de rutas, visualizacion de entregas, confirmacion de entregas, seguimiento GPS, uso offline y escaneo de codigos.
- Construir una arquitectura backend basada en microservicios con separacion de responsabilidades entre autenticacion, ciudadano, logistica y reparto.
- Incorporar mecanismos de comunicacion en tiempo real para que el cliente pueda seguir su envio y para que los operarios reciban notificaciones operativas.
- Integrar un sistema de pago realista mediante Stripe Checkout, junto con la generacion automatica de factura y etiqueta en PDF.
- Orquestar la comunicacion interna entre servicios para sincronizar estados y automatizar procesos clave, como el traspaso entre admision y reparto.
- Preparar el sistema para su despliegue en contenedores Docker, con pipeline de integracion y despliegue continuo.

En definitiva, el objetivo no ha sido unicamente programar una aplicacion funcional, sino demostrar la capacidad de analizar un problema empresarial amplio, proponer una arquitectura adecuada, implementar una solucion mantenible y justificar tecnicamente cada una de las decisiones adoptadas.

## 1.2 Alcance

El alcance del proyecto abarca el desarrollo de una plataforma completa de paqueteria con cobertura funcional para tres perfiles de uso principales: cliente final, personal interno de operaciones y personal de reparto. Para responder a estas necesidades, el sistema se divide en distintos bloques funcionales y tecnicos.

En primer lugar, el alcance incluye tres aplicaciones frontend desarrolladas con Angular:

- Una aplicacion orientada al cliente final, accesible desde la web principal, donde el usuario puede registrarse, autenticarse, consultar tarifas, crear envios, realizar pagos, hacer seguimiento y gestionar sus datos personales.
- Una intranet operativa, destinada a la gestion interna de centros de tratamiento, tareas logisticas, movimientos entre nodos, incidencias y escaneos.
- Una aplicacion especifica para repartidores, preparada para ejecutarse en dispositivos moviles, con soporte para visualizar rutas, registrar entregas, enviar ubicacion y continuar trabajando incluso con conectividad limitada.

En segundo lugar, el alcance incluye la capa backend basada en ASP.NET Core y microservicios. Los modulos implementados son:

- Modulo de autenticacion y gestion de usuarios.
- Modulo ciudadano, responsable del dominio de envios, tarifas, pagos, perfil y tracking publico.
- Modulo intranet o logistica, centrado en CTAs, asignaciones, movimientos y operativa interna.
- Modulo de reparto, dedicado a repartidores, rutas, entregas y localizacion.
- API Gateway para centralizar el acceso desde los clientes y aplicar reglas de autorizacion sobre las rutas.

En tercer lugar, el alcance contempla la infraestructura de datos y despliegue:

- Una base de datos PostgreSQL separada para cada microservicio principal.
- Uso de Docker y Docker Compose tanto en desarrollo local como en produccion.
- Nginx como proxy inverso y punto de entrada para los distintos dominios.
- Publicacion de imagenes en GHCR y despliegue automatizado en un VPS.

Ademas, el proyecto integra servicios y tecnologias externas que amplian su realismo y valor tecnico:

- Stripe Checkout para pagos online.
- SignalR para eventos en tiempo real.
- SMTP para envio de correos de confirmacion y recuperacion de contraseña.
- Leaflet para la representacion cartografica de la ruta del repartidor.
- html5-qrcode para lectura de codigos mediante camara.

No obstante, tambien es importante delimitar lo que queda fuera del alcance actual del proyecto. Aunque la plataforma es plenamente funcional, no pretende cubrir todas las capacidades de una empresa de paqueteria de gran escala. Por ejemplo, no se ha implementado una app movil nativa, ni un motor propio de navegacion giro a giro, ni una plataforma de observabilidad distribuida completa, ni un bus de eventos empresarial con patrones outbox/inbox ya consolidados. Tampoco se ha desarrollado un algoritmo avanzado de optimizacion de rutas basado en trafico real, capacidad del vehiculo o ventanas temporales de entrega. Estas mejoras se plantean como evolucion natural del sistema y se recogen en el apartado de trabajo futuro.

## 1.3 Justificacion

La justificacion del proyecto se apoya en tres dimensiones: la necesidad funcional que resuelve, el valor formativo que aporta y la calidad tecnica del reto afrontado.

Desde el punto de vista funcional, el sector de la paqueteria y la logistica necesita soluciones que ofrezcan trazabilidad, rapidez operativa, automatizacion de procesos y una experiencia digital coherente para todos los actores implicados. El cliente final demanda conocer el estado de su envio, pagar con comodidad y disponer de informacion clara. El personal de operaciones necesita herramientas para clasificar, asignar, mover y controlar los paquetes sin depender de procesos manuales dispersos. Los repartidores, por su parte, requieren acceso a rutas, entregas, evidencias y posicionamiento, incluso en situaciones de movilidad o conectividad irregular. NexoPostal responde a esta necesidad proponiendo una unica plataforma modular que conecta todos estos puntos.

Desde el punto de vista academico, el proyecto tiene especial interes porque permite poner en practica conocimientos de programacion frontend y backend, bases de datos, arquitecturas distribuidas, integracion continua, seguridad, despliegue y documentacion tecnica. No se trata de una simple aplicacion CRUD, sino de un sistema con varios dominios, distintos tipos de usuarios, reglas de negocio reales, comunicacion entre servicios y procesos asincronos. Esto convierte el TFG en una muestra mucho mas completa de competencias profesionales.

Tambien esta justificado por el aprendizaje que aporta a nivel de arquitectura. En lugar de concentrar toda la logica en una aplicacion monolitica, se ha optado por separar la solucion en componentes especializados. Esta decision obliga a trabajar conceptos como autenticacion con JWT, paso de contexto entre servicios, coordinacion de eventos, seguridad interservicio, gateways, gestion de configuracion y despliegue por contenedores. En otras palabras, el proyecto no solo resuelve una necesidad funcional, sino que reproduce problemas y patrones habituales en entornos profesionales.

Por ultimo, NexoPostal esta justificado por su potencial de evolucion. La base construida permite seguir ampliando el sistema con nuevas capacidades, como notificaciones push reales, analitica operativa, inteligencia en la asignacion de rutas, dashboards avanzados, almacenamiento centralizado de evidencias o integracion con proveedores externos. Esto convierte al proyecto en una plataforma extensible y no en un ejercicio cerrado de corto recorrido.

---

# 2. Implementacion

## 2.1 Analisis de la aplicacion

### 2.1.1 Contexto del problema

Antes de definir la solucion, fue necesario analizar el problema de negocio que se queria resolver. En una empresa de paqueteria conviven varios procesos que, aunque para el cliente parezcan una sola operacion, en realidad implican diferentes sistemas y responsables: alta del envio, calculo del precio, pago, generacion de documentacion, admision en oficina o sistema, clasificacion en centros logísticos, transporte troncal entre zonas, asignacion a reparto, entrega final y consulta de trazabilidad. Si cualquiera de estos pasos falla o queda desconectado del resto, la experiencia del usuario se resiente y la eficiencia operativa disminuye.

En muchos entornos tradicionales, la informacion del cliente, la logistica interna y el reparto final se manejan con herramientas poco integradas o con procesos manuales. Esto provoca problemas como falta de visibilidad del estado real del paquete, duplicidad de datos, demoras en la actualizacion de estados, escasa automatizacion y baja capacidad de reaccion ante incidencias. El analisis inicial del proyecto planteo que una solucion adecuada debia ser capaz de unificar la vision del cliente y la vision interna, manteniendo a la vez una separacion clara de responsabilidades entre los distintos modulos.

### 2.1.2 Actores y perfiles del sistema

El sistema identifica varios perfiles de usuario, cada uno con objetivos y permisos diferentes.

- Cliente: es el usuario final que crea envios, paga, consulta tarifas, hace seguimiento del pedido y gestiona su cuenta.
- Administrador: perfil interno con capacidad de gestion global del sistema y acceso ampliado a la intranet.
- Operario de oficina: usuario vinculado al tratamiento de paquetes en oficina, especialmente en procesos de recepcion y entrega en punto fisico.
- Operario logistico: perfil orientado a los CTAs y a tareas de clasificacion, asignaciones y movimientos logísticos.
- Operario jefe: perfil con funciones de coordinacion y mayor capacidad de gestion sobre tareas internas.
- Repartidor: usuario de ultima milla encargado de ejecutar rutas, registrar entregas, reportar incidencias y enviar ubicacion.

La existencia de varios actores con necesidades distintas justifica la separacion de interfaces y servicios. No tiene sentido que un cliente vea los mismos estados o acciones que un repartidor, del mismo modo que un operario de CTA necesita datos internos y no solo informacion publica de tracking.

### 2.1.3 Requisitos funcionales

Del analisis del problema se derivaron los principales requisitos funcionales del sistema.

En el area de cliente, la plataforma debia permitir:

- Registro de nuevos usuarios.
- Inicio de sesion y recuperacion de contraseña.
- Consulta publica de tarifas.
- Creacion de envios con datos completos de remitente, destinatario y paquete.
- Eleccion entre entrega o recogida en direccion u oficina, segun el caso.
- Pago online mediante una pasarela externa.
- Generacion de etiqueta y factura.
- Seguimiento publico por numero de envio.
- Gestion del perfil y de las direcciones favoritas.
- Consulta del historial de envios del usuario.

En el area interna de logistica, la solucion debia ofrecer:

- Resolucion automatica del CTA de destino a partir del codigo postal.
- Admision de paquetes en la red interna.
- Creacion automatica de movimientos troncales cuando el origen y el destino corresponden a nodos distintos.
- Notificaciones en tiempo real a operarios de los CTAs.
- Creacion y seguimiento de asignaciones de tareas.
- Registro de incidencias.
- Seguimiento interno del paquete por numero de expedicion o seguimiento.
- Cambios de estado interno por parte de operarios y repartidores.
- Escaneo individual y por lotes de codigos internos.

En el area de reparto, el sistema debia incluir:

- Gestion del perfil del repartidor.
- Consulta de la ruta del dia y sus entregas.
- Inicio y finalizacion de la ruta.
- Confirmacion de entrega con distintos resultados posibles.
- Registro de evidencias como nombre del receptor, DNI, firma o foto.
- Envio periodico de ubicacion GPS.
- Persistencia temporal offline y reintentos automáticos.
- Escaneo de expediciones desde la propia app de reparto.

### 2.1.4 Requisitos no funcionales

Ademas de los requisitos funcionales, el proyecto debia cumplir una serie de condiciones tecnicas que garantizaran su calidad.

- Modularidad: separar dominios para reducir el acoplamiento y facilitar mantenimiento.
- Escalabilidad: permitir que los componentes evolucionen de forma relativamente independiente.
- Seguridad: usar autenticacion JWT, control de roles y proteccion basica entre microservicios.
- Trazabilidad: registrar y exponer estados publicos e internos de forma coherente.
- Usabilidad: interfaces diferenciadas y adaptadas a cada tipo de usuario.
- Portabilidad: facilitar ejecucion local y despliegue remoto mediante contenedores.
- Resiliencia: soportar reconexiones en tiempo real y cierta tolerancia a conectividad limitada en reparto.
- Mantenibilidad: organizar el codigo por capas, servicios, DTOs, repositorios y modelos.

Estos requisitos no funcionales no se definieron como una lista teorica separada del desarrollo, sino que condicionaron directamente la forma de construir el sistema. La modularidad explica la division en microservicios y en tres frontends distintos; la portabilidad justifica el uso de Docker y Docker Compose; la seguridad se materializa en JWT, roles y claves internas entre servicios; la resiliencia se refleja en la reconexion automatica de SignalR y en la cola offline del modulo de reparto; y la mantenibilidad se consigue gracias a una estructura separada por controladores, servicios, repositorios, DTOs y modelos. En otras palabras, buena parte de la arquitectura tecnica de NexoPostal es una consecuencia directa de estos requisitos no funcionales.

### 2.1.5 Reglas de negocio clave

Mas alla de los requisitos generales, durante el analisis fue necesario identificar un conjunto de reglas de negocio concretas que determinan el comportamiento real de la plataforma. Estas reglas son especialmente importantes porque convierten el sistema en una solucion coherente y no en una simple suma de pantallas y endpoints.

- El calculo de tarifas se resuelve siempre en backend. La web no utiliza un simulador local distinto, sino que consulta un motor unico de tarifas. Esto garantiza consistencia entre el precio estimado, el precio pagado y el coste finalmente asociado al envio.
- El peso facturable del paquete no depende solo del peso real. El sistema compara el peso real con el peso volumetrico, calculado a partir de las dimensiones, y utiliza el mayor de ambos para la tarificacion.
- Existen restricciones fisicas y comerciales sobre el paquete. El formulario y la logica de negocio limitan el peso maximo a 30 kg, imponen dimensiones minimas para poder etiquetar el bulto y aplican recargo cuando la suma de dimensiones supera 210 cm o el lado mayor excede 170 cm.
- La operativa contempla casuisticas territoriales reales. Si el codigo postal de origen o destino corresponde a Canarias, el sistema exige la identificacion fiscal del remitente o del destinatario segun el caso, introduciendo una regla especial que no afecta al resto del territorio.
- Un envio puede tener origen y destino en direccion particular o en oficina. Esto afecta a la experiencia de usuario, al texto de las direcciones construidas, a la selecccion de oficinas y a la informacion asociada a la expedicion.
- Cada envio dispone de dos identificadores distintos. El numero de seguimiento publico se utiliza en la web de clientes y en el tracking externo, mientras que el numero de expedicion interno se utiliza en intranet, escaneo y reparto. Esta dualidad separa la visibilidad publica de la operativa interna.
- El envio no entra en la red logistica por el mero hecho de rellenar el formulario. Primero se crea en estado pendiente de pago; solo cuando el pago se confirma se marcan estados operativos, se generan documentos y se notifica la admision a logistica.
- El estado publico del envio no es una variable independiente y arbitraria, sino una proyeccion simplificada del estado interno. Esto permite que el cliente vea una trazabilidad comprensible sin exponer toda la complejidad operativa del circuito logístico.
- La admision del paquete resuelve automaticamente el CTA de destino a partir del codigo postal. Si origen y destino pertenecen a nodos distintos, se crea ademas un movimiento troncal con el tipo de transporte adecuado.
- La autoasignacion a reparto no se ejecuta siempre. Solo se intenta cuando la admision dispone de datos minimos de ultima milla, como numero de seguimiento, direccion de entrega y codigo postal de destino.
- El seguimiento GPS del repartidor no debe estar activo en cualquier contexto. La app movil solo intensifica el tracking cuando la ruta esta realmente en curso, evitando gasto innecesario de bateria y eventos irrelevantes.
- La falta de conectividad no debe bloquear la operativa de reparto. Por ello, confirmaciones de entrega y ubicaciones se almacenan temporalmente en una cola local para reenviarse cuando el dispositivo recupera conexion.

Estas reglas son fundamentales para entender el valor del proyecto. No se limitan a ser detalles tecnicos de implementacion, sino que explican por que el comportamiento observado en cliente, intranet y reparto es coherente entre si.

### 2.1.6 Procesos de negocio principales

Una vez identificados los requisitos, se definieron los procesos de negocio que articulan la plataforma.

Proceso 1. Alta y autenticacion de cliente  
El usuario se registra en la aplicacion de clientes mediante email, contraseña y nombre completo. Estos datos se almacenan en el microservicio de autenticacion, que emite el token JWT necesario para el resto de operaciones privadas. Una vez autenticado, el cliente puede acceder a su panel, consultar sus envios, mantener su agenda de direcciones y operar sobre el resto de servicios de forma segura. Este proceso es transversal, ya que actua como puerta de entrada al resto de funcionalidades de valor.

Proceso 2. Cotizacion y creacion del envio  
El cliente introduce peso, dimensiones, origen y destino del paquete. A partir de esos datos, el backend calcula el precio considerando zona, peso real, peso volumetrico, posibles recargos y tipo de servicio. Una vez completados remitente y destinatario, el sistema no crea directamente un envio pagado, sino que prepara un envio pendiente de pago y genera la sesion de Stripe Checkout asociada. Este matiz es importante porque separa la intencion de compra de la confirmacion real del cobro.

Proceso 3. Pago y admision logistica  
Cuando Stripe devuelve un resultado satisfactorio, el sistema verifica la sesion y actualiza el envio. En ese momento se marca como pagado, se registran la fecha de pago y los estados iniciales operativos, se genera la etiqueta PDF, se genera la factura PDF y se envian ambos documentos por correo electronico. A continuacion, el microservicio ciudadano notifica a logistica para que el paquete entre en la red interna. En caso de cancelacion o fallo, el envio puede permanecer pendiente de pago y volver a intentarse sin perder el contexto de la expedicion.

Proceso 4. Clasificacion y movimientos internos  
El modulo de intranet analiza el codigo postal de destino y determina el CTA que debe gestionar el paquete. Si el codigo postal de origen remite a un nodo diferente, se crea automaticamente un movimiento troncal entre ambos centros. El sistema decide ademas el tipo de transporte en funcion del contexto y de la urgencia del envio. Una vez registrado el paquete en la red, se envian notificaciones en tiempo real a los operarios logísticos del CTA correspondiente para iniciar la clasificacion y las tareas asociadas.

Proceso 5. Seguimiento interno del paquete  
La operativa interna trabaja principalmente con el numero de expedicion. Este identificador se usa para escaneo, seguimiento detallado, asignaciones de tareas y actualizacion de estados. A diferencia del tracking publico, el seguimiento interno refleja pasos mas finos del circuito logístico, como clasificacion, movimientos entre centros, intentos fallidos de entrega, incidencias especificas o devoluciones. Gracias a ello, la plataforma distingue entre lo que necesita ver un cliente y lo que necesita gestionar un operario.

Proceso 6. Autoasignacion a reparto  
Cuando la admision incluye los datos suficientes de ultima milla, la intranet invoca un endpoint interno del modulo de reparto. Este modulo intenta localizar un repartidor adecuado, reutilizar una ruta planificada del dia o crear una nueva si es necesario, y añadir la entrega asociada a la expedicion. El proceso incorpora una idempotencia basica por numero de expedicion, evitando la duplicacion de entregas ante reintentos. Esta automatizacion reduce trabajo manual y conecta la logistica interna con el reparto final.

Proceso 7. Ejecucion de ruta y entrega  
El repartidor inicia sesion en su aplicacion y consulta la ruta asignada del dia. Cuando comienza la jornada, marca la ruta como iniciada y la app activa el seguimiento GPS, el mapa operativo y la sincronizacion de eventos. Durante la ruta, el repartidor puede seleccionar entregas, navegar hacia la siguiente parada, registrar el resultado de cada intento y adjuntar evidencias como receptor, DNI, firma, foto u observaciones. Al finalizar, cierra la ruta con observaciones de jornada y detiene el seguimiento activo.

Proceso 8. Tracking publico en tiempo real  
Mientras el paquete avanza por la red, el modulo ciudadano proyecta la operativa interna en una traza publica comprensible. El cliente consulta inicialmente el estado por HTTP y, a continuacion, queda suscrito mediante SignalR a futuras actualizaciones del numero de seguimiento. Cuando reparto informa de una nueva ubicacion o de un evento de entrega, ciudadano transforma esa informacion en cambios de estado, incidencias o confirmaciones visibles para el cliente. De este modo, la trazabilidad publica se alimenta de procesos operativos reales y no de mensajes simulados.

Conviene destacar que estos procesos no funcionan de manera aislada. El valor del sistema reside precisamente en que un proceso alimenta al siguiente: la autenticacion permite operar; la cotizacion conduce al pago; el pago dispara la admision; la admision puede generar movimiento y autoasignacion; el reparto actualiza el tracking; y el tracking devuelve informacion al cliente en tiempo real. Esta continuidad operativa es una de las claves de NexoPostal como proyecto.

### 2.1.7 Entidades principales del dominio

Del analisis funcional surgieron las entidades que estructuran el sistema.

En el dominio de autenticacion destaca la entidad `ApplicationUser`, que representa al usuario del sistema. Esta entidad no solo almacena las credenciales basicas, sino tambien el nombre completo, el codigo de empleado cuando procede, la fecha de registro y el rol asignado. Gracias a ella, el mismo sistema de autenticacion puede dar soporte tanto a clientes como a perfiles internos.

En el dominio ciudadano destacan:

- ClientePerfil, con datos ampliados del cliente y direccion predeterminada.
- DireccionFavorita, para almacenar agenda personal de direcciones.
- Envio, entidad central del dominio, con doble identificador: numero de seguimiento publico y numero de expedicion interno.

La entidad `ClientePerfil` amplía la informacion del usuario autenticado con datos pensados para la operativa de la aplicacion de clientes, como el telefono, el documento identificativo o la direccion predeterminada. La entidad `DireccionFavorita` permite persistir una agenda reutilizable que simplifica la contratacion de nuevos envios. La entidad `Envio`, por su parte, concentra la mayor parte del valor de negocio: origen, destino, datos de remitente y destinatario, codigos postales, peso, dimensiones, coste, tarifa, estado publico, estado interno, estado de pago y referencias a documentos y sesiones de Stripe.

En el dominio logístico destacan:

- CentroTratamiento, que modela los CTAs de la red.
- RutaCta, para definir relaciones entre zonas y centros.
- OperarioCta y OperarioOficina, para vincular personal interno a nodos operativos.
- AsignacionPaquete, que representa una tarea concreta sobre un envio dentro de un CTA.
- MovimientoPaquete, que representa un desplazamiento troncal entre dos centros.
- Incidencia, para registrar excepciones del proceso.

`CentroTratamiento` actua como nodo logístico principal y se complementa con reglas de clasificacion por prefijos de codigo postal. `OperarioCta` y `OperarioOficina` permiten modelar quien trabaja en cada ambito y con que rol. `AsignacionPaquete` representa trabajo fisico concreto sobre un bulto, con responsable, creador, estado, urgencia y fechas operativas. `MovimientoPaquete` modela el trayecto troncal entre dos CTAs, incluyendo origen, destino, estado y tipo de transporte. Finalmente, `Incidencia` recoge las excepciones del flujo para que el sistema no pierda trazabilidad cuando algo se desvía de la operativa normal.

En el dominio de reparto destacan:

- Repartidor, perfil especializado asociado a una oficina de referencia.
- RutaReparto, agrupacion de entregas para una jornada y un repartidor concretos.
- EntregaPaquete, unidad operativa de ultima milla sobre un paquete de una ruta.

La entidad `Repartidor` conecta la identidad del usuario con su rol de ultima milla, su oficina de referencia y su contexto operativo. `RutaReparto` representa la unidad de trabajo diaria del repartidor y contiene informacion sobre fecha, estado, horario y agrupacion de entregas. `EntregaPaquete` es la entidad que mas se acerca a la realidad de la ultima milla, ya que registra direccion, destinatario, orden de parada, estado, intento, evidencias y coordenadas de entrega.

La decision de utilizar dos identificadores distintos para un envio es especialmente relevante. El numero de seguimiento publico se expone al cliente y se usa para el tracking web. El numero de expedicion interno se reserva a la operativa de intranet y reparto. Esta separacion reduce exposicion innecesaria de informacion interna y permite trabajar con dos niveles de granularidad funcional.

Tambien resulta importante la distribucion de la propiedad de los datos. Auth es dueño de la identidad y de las credenciales; Ciudadano es dueño de la contratacion, del perfil de cliente, del pago y del tracking publico; Intranet es dueña de la gestion de CTAs, tareas y movimientos internos; y Reparto es dueño de rutas, entregas y posicion del repartidor. Esta separacion de responsabilidades es una de las claves arquitectonicas del proyecto y conviene mantenerla visible desde el propio analisis funcional.

## 2.2 Diseño

### 2.2.1 Diseño arquitectonico general

La arquitectura de NexoPostal se ha diseñado siguiendo un enfoque de microservicios con separacion clara de dominios. Esta decision no se adopto por moda tecnologica, sino porque encaja con el problema a resolver. La autenticacion, la contratacion de envios, la operativa logistica interna y el reparto de ultima milla tienen reglas de negocio diferentes, ritmos de cambio distintos y necesidades de seguridad particulares. Si toda la logica se hubiese concentrado en un unico backend monolitico, el resultado habria sido una base de codigo mas acoplada, menos legible y mas dificil de justificar academica y tecnicamente.

Desde el punto de vista logico, la solucion puede dividirse en cuatro planos:

- Plano de presentacion, compuesto por tres clientes Angular independientes: clientes, intranet y driver app.
- Plano de acceso, compuesto por Nginx y el API Gateway, que concentran entrada, enrutado, dominios y politicas de autorizacion.
- Plano de negocio, formado por los microservicios Auth, Ciudadano, Intranet y Reparto.
- Plano de persistencia e integracion, formado por PostgreSQL por servicio, archivos JSON de apoyo, correo SMTP, Stripe y SignalR.

La responsabilidad de cada bloque es la siguiente:

- Nginx actua como puerta de entrada externa y publica las tres aplicaciones bajo dominios diferenciados.
- El API Gateway centraliza el acceso HTTP desde los clientes y decide que rutas son publicas y cuales requieren autenticacion.
- El microservicio Auth gestiona identidades, login, registro, refresh token, recuperacion de contraseña y datos base del usuario.
- El microservicio Ciudadano concentra la logica visible para el cliente: envios, tarifas, pagos, documentos, perfil, direcciones y tracking publico.
- El microservicio Intranet modela la red logistica interna: CTAs, operarios, asignaciones, movimientos, incidencias, admision y notificaciones operativas.
- El microservicio Reparto se ocupa de la ultima milla: repartidores, rutas, entregas, posicion del repartidor y sincronizacion de eventos de entrega.

Esta arquitectura presenta varias ventajas. En primer lugar, cada modulo concentra la logica de su dominio y puede evolucionar con menor impacto sobre el resto. En segundo lugar, permite una division mas limpia de tablas, endpoints y reglas de negocio. En tercer lugar, facilita el despliegue por contenedores y el aislamiento de fallos. Finalmente, tiene gran valor academico porque obliga a resolver integracion, seguridad, orquestacion y consistencia de datos entre modulos, cuestiones mucho mas cercanas a un sistema real que a una aplicacion CRUD convencional.

### 2.2.2 Diseño de la arquitectura fisica

La arquitectura fisica del proyecto se apoya en contenedores Docker tanto en local como en produccion. Esto permite que el entorno de desarrollo y el entorno desplegado compartan una filosofia comun de ejecucion, algo muy valioso para reducir diferencias de comportamiento y simplificar las pruebas.

En desarrollo local, la plataforma se levanta con `docker-compose.yml` mediante `docker compose up -d --build`. Todos los servicios se conectan a una red comun y el proxy Nginx expone la web de clientes en el puerto 80, la intranet en el puerto 4202 y la app de reparto en el puerto 4201. Dentro de esa misma red conviven el gateway, los cuatro microservicios principales y las cuatro bases de datos PostgreSQL. Ademas, las bases de datos exponen puertos locales separados para facilitar depuracion y acceso en desarrollo. Este modelo permite reproducir el comportamiento completo de la plataforma sin instalaciones manuales adicionales de bases de datos o backends.

En produccion, el despliegue se realiza con `docker-compose.production.yml`, pero en lugar de construir contenedores en el propio servidor se consumen imagenes previamente publicadas en GHCR. Esta decision reduce el trabajo del VPS, acorta el tiempo de despliegue y separa claramente la fase de compilacion de la fase de ejecucion. En este escenario solo se exponen externamente los puertos 80 y 443, mientras que las bases de datos quedan aisladas en la red interna. Esta organizacion es mas segura y mas cercana a una operativa profesional.

Otro elemento importante de la arquitectura fisica es la diferenciacion por dominios. Nginx sirve la web de clientes bajo el dominio principal, la intranet bajo un subdominio propio y la app de reparto bajo otro distinto. Ademas, el proxy esta configurado para manejar correctamente las conexiones WebSocket necesarias para SignalR, tanto en el tracking publico como en las notificaciones internas de la intranet. Esto demuestra que el sistema no solo esta diseñado a nivel logico, sino tambien preparado para funcionar como un conjunto de aplicaciones publicadas de forma coherente.

La configuracion por variables de entorno tambien forma parte del diseño fisico. Los contenedores consumen archivos `.env` para resolver cadenas de conexion, claves JWT, configuracion de Stripe, credenciales SMTP y URLs internas de los servicios. Gracias a ello, el mismo codigo puede ejecutarse en distintos entornos sin necesidad de modificar ficheros fuente.

### 2.2.3 Diseño de datos

El diseño de datos sigue el principio de base de datos por servicio. Cada microservicio mantiene su propio esquema y sus propias tablas, evitando dependencias fuertes entre bases de datos y reduciendo el acoplamiento. Esta estrategia obliga a pensar cuidadosamente que modulo es dueño de cada dato y como se relacionan las piezas del sistema sin compartir directamente tablas entre dominios.

La distribucion principal de la persistencia es la siguiente:

- Auth mantiene las tablas de identidad, usuarios, credenciales y tokens asociados a la sesion.
- Ciudadano mantiene perfiles de cliente, direcciones favoritas y envios contratados, incluyendo sus estados, coste, datos del remitente y del destinatario, y referencias de pago.
- Intranet mantiene CTAs, rutas de clasificacion, relaciones con operarios, asignaciones, movimientos e incidencias.
- Reparto mantiene repartidores, rutas diarias y entregas de ultima milla.

Para enlazar informacion entre modulos se utilizan identificadores compartidos o referencias cruzadas de tipo logico, no relaciones fisicas entre motores distintos. Algunos ejemplos especialmente relevantes son los siguientes:

- `IdentityUserId`, que conecta los datos de Auth con perfiles en Ciudadano y perfiles de repartidor en Reparto.
- `NumeroSeguimiento`, que identifica publicamente un envio y se utiliza en tracking y comunicacion con el cliente.
- `NumeroExpedicion`, que sirve de referencia operativa transversal entre Ciudadano, Intranet y Reparto.
- Identificadores internos como `RutaId`, `EntregaId` o `CtaId`, que organizan la operativa propia de cada dominio.

Este diseño obliga a asumir una consecuencia importante: la consistencia entre servicios no se obtiene mediante relaciones SQL directas, sino mediante contratos de integracion y actualizaciones coordinadas. Precisamente por eso la memoria debe explicar no solo las entidades aisladas, sino tambien como se sincronizan los estados entre modulos.

Ademas, no toda la informacion del sistema se apoya en tablas relacionales. Existen datos que deliberadamente se mantienen fuera de la base de datos principal. El caso mas claro es el de las oficinas postales, que se obtienen desde un archivo JSON estatico cargado en memoria y utilizado tanto para busquedas publicas como para resolucion de oficinas en procesos internos. Esta decision simplifica la demostracion, evita construir una administracion completa de oficinas y permite mostrar como combinar persistencia relacional con fuentes estructuradas externas.

Tambien existen mecanismos de persistencia ligera en cliente. Por ejemplo, la autenticacion en frontend utiliza almacenamiento local para conservar token y contexto de usuario, mientras que la app de reparto utiliza `localStorage` para mantener la cola offline de ubicaciones y confirmaciones pendientes. Esto introduce una segunda capa de gestion del dato, mas cercana a la experiencia de usuario y a la tolerancia a fallos de conectividad.

### 2.2.4 Diseño de seguridad

La seguridad se ha diseñado en varias capas, combinando autenticacion, autorizacion, separacion por roles, aislamiento de entornos y proteccion basica entre servicios internos.

A nivel de usuario final, el acceso a rutas protegidas se gestiona mediante JWT. El frontend conserva el token y lo envía en las peticiones autenticadas, mientras que los microservicios validan firma, emisor, audiencia y expiracion. Un detalle relevante del proyecto es que la validacion se configura sin margen de cinco minutos, utilizando `ClockSkew = TimeSpan.Zero`, lo que endurece el comportamiento temporal de la sesion y evita aceptar tokens expirados durante un intervalo extra.

A nivel de entrada al sistema, el gateway añade una segunda capa de control porque distingue entre rutas publicas y privadas. Login, registro, refresh, reseteo de contraseña, tracking publico, calculo de tarifas, busqueda de oficinas y webhook de Stripe son rutas abiertas por necesidad funcional. El resto exige contexto autenticado. Este punto es importante porque la frontera de acceso no depende solo del frontend, sino tambien del backend central de entrada.

A nivel de roles, cada aplicacion cliente restringe lo que puede hacer el usuario segun su perfil. La web de clientes admite unicamente usuarios con rol `Cliente`. La intranet presenta opciones diferenciadas para cuatro perfiles: `Admin`, `OperarioOficina` (atiende ventanilla y escanea en oficinas postales), `OperarioCTA` (trabaja en la nave de clasificacion y gestiona movimientos troncales) y `Supervisor` (gestiona incidencias, altas de personal y metricas, pero no opera paquetes directamente). La app de reparto distingue entre `Repartidor` (ejecuta la ruta y confirma entregas) y `JefeReparto` (planifica rutas, da de alta repartidores y consulta metricas del equipo). Esta separacion evita mezclar experiencias de usuario y refuerza la idea de aplicacion especializada por contexto operativo.

La seguridad del dominio de autenticacion tambien se refuerza con funcionalidades adicionales, como refresh tokens reales con rotacion, revocacion y recuperacion de contraseña por correo. Estas capacidades mejoran la experiencia de usuario, pero tambien elevan el nivel del proyecto frente a un modelo minimo de login con token simple.

En la comunicacion interservicio, determinados endpoints internos se protegen mediante una `X-Service-Key`. Esta clave se usa, por ejemplo, para admitir paquetes desde Ciudadano hacia Intranet o para publicar tracking desde Reparto hacia Ciudadano. En varios puntos del sistema la comparacion se realiza en tiempo constante, reduciendo problemas asociados a comparaciones inseguras de cadenas. Aunque este enfoque es suficiente para un MVP academico y para una arquitectura pequena, la propia memoria debe dejar claro que en un escenario empresarial de mayor madurez seria recomendable evolucionar hacia esquemas mas robustos, como JWT de servicio a servicio o mTLS.

Finalmente, la seguridad se extiende tambien a la capa de publicacion. En produccion se utiliza HTTPS con certificados en Nginx y se aplican cabeceras de seguridad propias de un entorno web real, como HSTS y proteccion frente a mezcla de contenido. Esto refuerza el sistema no solo desde el codigo, sino tambien desde la infraestructura de entrega.

### 2.2.5 Diseño de la comunicación entre servicios y de los eventos en tiempo real

Uno de los aspectos mas interesantes del diseño de NexoPostal es que la integracion entre modulos no se basa en una unica tecnica, sino en una combinacion de llamadas HTTP entre microservicios y eventos push mediante SignalR. Este enfoque hibrido permite separar adecuadamente la operativa sin perder continuidad de negocio.

La comunicacion síncrona entre servicios se utiliza cuando un modulo necesita provocar de forma inmediata una accion concreta en otro. Algunos ejemplos clave son:

- Ciudadano notifica a Intranet que un envio pagado debe ser admitido en la red.
- Intranet solicita a Reparto la autoasignacion de una entrega cuando dispone de los datos minimos de ultima milla.
- Reparto comunica a Ciudadano ubicaciones o eventos de entrega para consolidar el tracking publico.

Este modelo tiene la ventaja de ser sencillo de seguir y muy apropiado para un TFG, porque deja visibles las dependencias funcionales entre dominios. Sin embargo, tambien introduce una limitacion conocida: al depender de peticiones directas, la robustez frente a fallos temporales de red es menor que en un sistema basado en mensajeria duradera. Precisamente por eso el proyecto recoge como trabajo futuro la evolucion hacia patrones como outbox e inbox.

En paralelo, el sistema utiliza SignalR en dos ambitos claramente diferenciados. El primero es el tracking publico del cliente, donde el modulo Ciudadano expone un hub al que cualquier usuario puede suscribirse con un numero de seguimiento. El segundo es la intranet, donde existe un hub interno autenticado que organiza a los usuarios por grupos de CTA y rol. Esta separacion es muy importante porque el realtime publico y el realtime operativo responden a necesidades distintas y no deben mezclarse.

Gracias a esta estrategia, el flujo completo puede describirse de forma coherente. Un cliente contrata y paga un envio; Ciudadano dispara la admision interna; Intranet resuelve CTA, crea movimientos y notifica a operarios; si procede, Intranet coordina con Reparto la autoasignacion; Reparto ejecuta ruta, registra entregas y reporta ubicacion; y Ciudadano proyecta esa operativa al tracking visible para el cliente. El sistema, por tanto, no solo integra modulos, sino que mantiene un hilo narrativo único del dato a través de todos ellos.

### 2.2.6 Diseño de las interfaces de usuario

Uno de los aspectos mas importantes del diseño ha sido separar claramente las interfaces segun el tipo de usuario. El sistema no utiliza una unica web para todo, sino tres clientes especializados.

#### A) Aplicacion de clientes

La aplicacion de clientes se ha diseñado para ofrecer una experiencia cercana a la de una empresa de paqueteria real. Sus pantallas principales son:

- Inicio: punto de acceso general a la plataforma y a las funcionalidades publicas.
- Registro y login: acceso del cliente a su espacio personal.
- Calculadora de tarifas: permite estimar precios antes de contratar.
- Nuevo envio: asistente de varias etapas para preparar el paquete y realizar el pago.
- Buscador de oficinas: localiza oficinas por codigo postal o texto libre.
- Tracking: muestra el estado del envio y sus actualizaciones en tiempo real.
- Panel de usuario: concentra perfil, direcciones favoritas y listado de envios.
- Pago exitoso y pago cancelado: gestion del retorno tras Stripe Checkout.
- Recuperacion de contraseña: flujo de reseteo por correo.

La pantalla de nuevo envio merece especial atencion porque concentra una gran parte del valor funcional del proyecto. Su diseno sigue un modelo por pasos. Primero se recogen los datos del remitente. Despues, los del destinatario. Por ultimo, se introducen las caracteristicas fisicas del paquete y se calculan las tarifas disponibles. El usuario puede elegir si el origen o el destino corresponde a una direccion particular o a una oficina postal, reutilizar direcciones guardadas y seleccionar la tarifa mas adecuada. Esta estructura por pasos reduce la complejidad percibida y mejora la usabilidad.

Ademas, el formulario incorpora validaciones de negocio que acercan la aplicacion a un caso real. Se comprueba el peso maximo, las dimensiones minimas necesarias para etiquetado, la longitud maxima permitida y la aplicacion de recargos por exceso de tamaño. Tambien se exige DNI/NIF en envios con Canarias, lo que introduce una casuistica interesante en la experiencia de usuario.

#### B) Intranet operativa

La intranet tiene un enfoque completamente distinto. No prima la contratacion o la simplicidad comercial, sino la operativa, la rapidez y la visibilidad del estado interno. Sus pantallas principales son:

- Login interno.
- Dashboard inicial con acceso segun rol.
- Gestion de CTA, donde el operario consulta los centros a los que esta asignado y visualiza metricas operativas.
- Asignaciones, donde se crean, inician, completan o cancelan tareas sobre expediciones.
- Seguimiento interno, donde se busca un envio por numero de expedicion o seguimiento y se puede modificar su estado interno.
- Escaneo logistico, pensado para procesar codigos individuales o lotes con distintos modos de operacion.
- Panel de administracion, reservado a perfiles de mayor privilegio.
- Gestion de usuarios, exclusiva del rol Admin: listado con filtros, alta de empleados, cambio de rol, bloqueo/desbloqueo de acceso y restablecimiento de contrasena. Esta funcionalidad evita tener que acceder directamente a la base de datos para operaciones habituales de soporte y alta de personal.

La pantalla de escaneo es especialmente representativa del enfoque operativo del sistema. Se han definido varios modos de escaneo, como recepcion en CTA, clasificacion, despacho troncal, recepcion troncal, recepcion en oficina, entrega a oficina destino y salida a reparto. Esto permite que la misma herramienta se adapte a momentos distintos del circuito logístico. Ademas, se ha incluido modo batch para procesar varios codigos de una sola vez y se mantiene un historial de escaneos durante la sesion.

#### C) Aplicacion de repartidores

La app de reparto se ha diseñado con orientacion movil y foco en la accion. Las pantallas principales son:

- Login del repartidor.
- Dashboard operativo.
- Ruta activa, con resumen de la jornada, entregas, progreso y mapa.
- Escaneo de expediciones para apoyo en reparto.

La vista de ruta es la pantalla mas importante del modulo de reparto. En ella se muestra la ruta asignada, el estado de la jornada, las paradas pendientes, las entregas completadas y las fallidas, asi como un mapa con la posicion del repartidor y los destinos disponibles. Desde esta misma pantalla el usuario puede iniciar o finalizar la ruta, centrar el mapa, abrir navegacion externa y confirmar una entrega con toda la informacion asociada.

Este diseño responde a una realidad operativa clara: el repartidor no puede navegar entre multiples pantallas complejas mientras conduce o realiza entregas. Por ello, se ha concentrado la mayor parte de la accion en una vista unica y se ha dotado a esa vista de soporte GPS, reintentos, cola offline y evidencias de entrega.

### 2.2.7 Manual de usuario resumido por procesos

Para reforzar el diseño funcional, puede describirse el uso de la plataforma mediante una secuencia de procesos.

Proceso de envio por parte del cliente:

1. El usuario accede a la web de NexoPostal y se autentica.
2. Accede a la opcion de nuevo envio.
3. Introduce los datos del remitente, pudiendo seleccionar direccion u oficina.
4. Introduce los datos del destinatario, igualmente con posibilidad de direccion u oficina.
5. Informa peso y dimensiones del paquete.
6. El sistema calcula las tarifas disponibles y el usuario selecciona una.
7. Se crea la sesion de pago y el usuario es redirigido a Stripe.
8. Tras el pago, vuelve a la aplicacion, donde se verifica la sesion y se confirma el envio.

Proceso de seguimiento por parte del cliente:

1. El usuario accede al apartado de tracking.
2. Introduce el numero de seguimiento.
3. El sistema recupera por HTTP el estado actual y el historial disponible.
4. A continuacion, el cliente queda suscrito por SignalR a las actualizaciones del envio.
5. Si el estado cambia mientras la vista esta abierta, la informacion se actualiza en tiempo real.

Proceso de trabajo en intranet:

1. El operario inicia sesion en la intranet.
2. Consulta su CTA o conjunto de CTAs asignados.
3. Revisa metricas, notificaciones o tareas pendientes.
4. Crea o gestiona asignaciones sobre expediciones concretas.
5. Puede buscar envios por numero interno y cambiar estados segun la operativa realizada.
6. En tareas mas intensivas, utiliza el modulo de escaneo para registrar entradas, clasificaciones o salidas.

Proceso de reparto:

1. El repartidor inicia sesion en la app movil.
2. Consulta su ruta del dia o la ruta en curso.
3. Inicia la ruta cuando sale a reparto.
4. El sistema activa el seguimiento GPS y comienza a reportar ubicacion.
5. En cada parada, el repartidor selecciona la entrega y confirma su resultado.
6. Si no hay conectividad, la accion se guarda en cola y se reintenta automaticamente.
7. Al finalizar la jornada, el repartidor cierra la ruta con observaciones.

## 2.3 Implementacion

### 2.3.1 Implementacion del frontend con Angular

Las tres aplicaciones frontend se han desarrollado con Angular 21 utilizando componentes standalone y una organizacion basada en paginas, servicios, guards e interceptores. Esta eleccion encaja bien con la arquitectura general del proyecto porque evita dependencias innecesarias, reduce el peso del arranque y facilita que cada cliente evolucione de forma aislada.

Cada una de las aplicaciones sigue una misma idea estructural, aunque aplicada a su dominio:

- `app.routes.ts` define la navegacion y las pantallas principales.
- `app.config.ts` centraliza proveedores globales, como cliente HTTP, interceptores y configuraciones compartidas.
- Las paginas implementan la logica de presentacion y coordinan formularios, estados visuales y acciones de usuario.
- Los servicios encapsulan el contrato HTTP con el backend y evitan que los componentes conozcan detalles de rutas o payloads.
- Los guards protegen el acceso a rutas privadas y ayudan a restringir el uso por sesion y por rol.
- Los interceptores añaden el token JWT y unifican ciertos comportamientos de comunicacion con API.

Una decision importante del proyecto ha sido no introducir una capa de estado global pesada como NgRx. En su lugar, se ha optado por una combinacion de estado local de componente, servicios especializados y señales reactivas en los puntos donde la interfaz necesita reflejar cambios inmediatos. Para el alcance del TFG, esta decision resulta razonable: mantiene la solucion comprensible, evita sobreingenieria y ofrece suficiente capacidad para modelar formularios multietapa, estados de carga, errores y actualizaciones en tiempo real.

En el caso de la aplicacion de clientes, se ha trabajado con servicios especificos para autenticacion, envios, tarifas, pagos, oficinas, perfil y tracking. Esto facilita que cada pagina consuma exactamente la funcionalidad necesaria y evita duplicar logica de acceso a API. La experiencia resultante esta muy orientada a tarea: contratar un envio, consultar su evolucion y mantener la informacion personal del cliente.

La pantalla de tracking incorpora una combinacion de carga inicial por HTTP y suscripcion posterior por SignalR. Este diseño evita depender por completo de los eventos en tiempo real y garantiza que el usuario vea de inmediato la informacion disponible, incluso aunque la conexion WebSocket tarde unos instantes en establecerse. En la intranet y en la app de reparto se repite la misma filosofia: la informacion critica se puede obtener por consulta directa, pero se complementa con eventos en vivo cuando estos aportan valor operativo.

La intranet y la app de reparto tambien siguen un patron de servicios, aunque adaptado a sus necesidades. En la intranet se concentra la logica de consultas operativas, dashboards, asignaciones, seguimiento interno, escaneo y notificaciones. En la app de reparto, la logica gira alrededor de la ruta, las entregas, la geolocalizacion y la tolerancia a conectividad irregular. Esta consistencia estructural entre frontends facilita el mantenimiento y refuerza la idea de ecosistema de aplicaciones, no de una unica interfaz con permisos mezclados.

### 2.3.2 Aplicacion de clientes: funcionalidades implementadas

#### Registro, login y recuperacion de contraseña

El cliente puede registrarse con email, contraseña y nombre completo. Una vez autenticado, el token JWT se almacena localmente y se utiliza para consumir las rutas privadas. El servicio de autenticacion del frontend comprueba el rol del usuario y limita el acceso al flujo de cliente, evitando que perfiles internos utilicen por error esta interfaz. El sistema tambien soporta solicitud de reseteo de contraseña y establecimiento de una nueva contraseña a traves de un token enviado por correo, lo que añade una capa de madurez funcional poco habitual en prototipos academicos basicos.

#### Contratacion del envio mediante asistente por pasos

La pagina de contratacion del envio es uno de los elementos mas trabajados del proyecto. No se trata de un formulario lineal simple, sino de un asistente por pasos que organiza la informacion para reducir errores y guiar al usuario durante una operacion relativamente compleja.

El flujo se divide en tres fases principales: datos de origen y remitente, datos de destino y destinatario, y configuracion del paquete con servicio y pago. Esta separacion permite validar progresivamente la informacion y mostrar advertencias en el momento oportuno. Entre las reglas implementadas destacan las siguientes:

- Validacion de campos de contacto obligatorios para remitente y destinatario.
- Comprobacion de codigos postales y estructura minima de direccion.
- Seleccion explicita entre entrega o recogida en domicilio y entrega o recogida en oficina.
- Integracion con la agenda de direcciones favoritas para autocompletar remitente o destinatario.
- Busqueda de oficinas para los escenarios en los que el paquete entra o sale de un punto fisico.
- Obligacion de DNI en operaciones que implican Canarias, reflejando una casuistica territorial real.
- Control de peso maximo de 30 kg y verificacion de dimensiones compatibles con la operativa.
- Advertencia de recargo cuando la suma de dimensiones supera 210 cm o aparece exceso dimensional relevante.

El valor de esta implementacion no reside solo en la interfaz, sino en la coherencia entre lo que se valida en frontend y lo que finalmente consume backend. El formulario construye un payload estructurado que recoge origen, destino, servicio, medidas, datos personales y preferencias operativas, y lo envía al flujo de pago sin introducir reglas comerciales alternativas. De este modo, la experiencia de contratacion mantiene consistencia con el motor real de negocio.

#### Calculadora de tarifas

La calculadora consume directamente el backend y evita realizar estimaciones locales independientes. Esto es importante porque garantiza que la logica comercial sea unica y que el precio mostrado al usuario coincida con el precio que se utilizara en la creacion real del envio. El frontend actua aqui como presentador de resultados, no como segunda fuente de verdad.

El motor de tarifas tiene en cuenta:

- Peso real del paquete.
- Peso volumetrico, calculado a partir de largo, ancho y alto.
- Peso facturable, que es el maximo entre peso real y volumetrico.
- Zona del envio segun codigos postales: local, peninsula, Baleares, Ceuta/Melilla o Canarias.
- Tipo de tarifa: estandar o premium.
- Recargo por exceso dimensional cuando la suma de dimensiones supera 210 cm o el lado mayor excede 170 cm.
- IVA del 21 por ciento.

Ademas de devolver importe, el calculo tambien informa de tiempo estimado y condiciones del servicio, lo que ayuda al usuario a comparar opciones antes de pagar. Esto convierte la tarificacion en uno de los componentes mas realistas del sistema, ya que va mas alla de una simple tabla estatica y reproduce criterios habituales del sector logístico.

#### Creacion del envio y pasarela de pago

El flujo real implementado no crea el envio como pagado desde el principio. En lugar de eso, al confirmar el formulario se genera en backend un envio en estado `PendientePago`, junto con una sesion de Stripe Checkout. El usuario es redirigido a la pasarela externa y, una vez completado el pago, el sistema verifica la sesion, marca el envio como pagado, genera documentacion y dispara la admision logistica.

La app de clientes contempla ademas el retorno desde Stripe hacia pantallas de pago exitoso o cancelado. En el escenario de exito, el frontend consulta al backend para consolidar el resultado real de la sesion; en el escenario de cancelacion, el envio puede seguir existiendo como pendiente y ser reintentado mas adelante. Este diseño es importante porque separa con claridad la intencion de compra de la confirmacion efectiva del pago y evita perder la operacion si el usuario abandona temporalmente la pasarela.

#### Perfil y agenda de direcciones

El cliente dispone de un panel donde puede:

- Consultar y editar sus datos de perfil ampliado.
- Actualizar los datos de cuenta del sistema de autenticacion.
- Cambiar la contraseña desde una vista controlada.
- Crear, editar y eliminar direcciones favoritas.
- Establecer una direccion predeterminada.
- Consultar el listado de envios asociados a su cuenta.
- Descargar etiquetas y facturas en PDF.

La implementacion de este panel es relevante porque unifica varias responsabilidades que, en muchas aplicaciones, aparecen dispersas. Aqui el usuario no solo contrata, sino que mantiene su identidad operativa dentro del sistema, reutiliza datos en futuras contrataciones y accede a la documentacion de los envios ya procesados. Esta parte del proyecto aporta mucha solidez al modulo de cliente, ya que convierte la plataforma en algo mas que un simple formulario de contratacion puntual.

#### Tracking publico en tiempo real

El tracking publico combina la consulta de trazabilidad existente con la recepcion de eventos en vivo. El cliente visualiza una barra de progreso con estados como recogido, oficina de origen, clasificacion, transito, CTA destino, oficina destino, reparto y entregado. Ademas, cuando se producen cambios, estos pueden llegar por SignalR con informacion de estado, entrega, incidencia o ubicacion.

Desde el punto de vista de implementacion, esta pantalla no se limita a pintar un texto de estado. Incluye una traduccion de la realidad interna a una narrativa comprensible para el cliente, gestiona la conexion y reconexion al hub, controla suscripciones por numero de seguimiento y actualiza visualmente el progreso del envio. Esta funcionalidad es especialmente valiosa porque aporta una sensacion de sistema vivo y elimina la necesidad de recargar continuamente la pagina para ver novedades.

### 2.3.3 Microservicio de autenticacion

El microservicio Auth se ha implementado con ASP.NET Core Identity, utilizando un modelo de usuario propio que extiende `IdentityUser` con nombre completo, codigo de empleado, fecha de registro y rol. Sobre esta base se construyen los procesos de login, registro, consulta del usuario autenticado, actualizacion del perfil de cuenta, cambio de contraseña, refresh token y reseteo de contraseña por correo.

Desde el punto de vista de arranque, el servicio configura conexion a PostgreSQL, autenticacion JWT, Identity y politicas de autorizacion, y ademas aplica migraciones y prepara datos base necesarios para que el sistema pueda arrancar de forma consistente. Esto es importante porque evita depender de configuraciones manuales posteriores al despliegue.

En la capa de API, el `AuthController` concentra endpoints de alto valor funcional. No solo permite autenticar y registrar usuarios, sino tambien recuperar el contexto del usuario autenticado, modificar informacion de cuenta, cambiar la contraseña, emitir nuevos tokens mediante refresh y lanzar el flujo de recuperacion de acceso. Esta amplitud funcional convierte a Auth en un servicio transversal real y no en un simple endpoint de login.

Desde el punto de vista tecnico, la autenticacion utiliza JWT con validacion de firma, emisor, audiencia y expiracion sin margen adicional de cinco minutos, lo cual endurece el comportamiento de la sesion. Asimismo, el sistema soporta refresh tokens reales con rotacion, expiracion y revocacion, lo que mejora la experiencia de usuario y acerca la aplicacion a una implementacion profesional.

El modulo Auth es compartido por todas las aplicaciones del sistema, pero cada frontend aplica restricciones propias sobre el rol del usuario que puede entrar. Por ejemplo, la app de clientes limita el acceso a usuarios con rol Cliente, mientras que la intranet y la app de reparto esperan perfiles internos. De este modo, la identidad es comun, pero el contexto de uso no lo es.

### 2.3.4 Microservicio ciudadano

El modulo Ciudadano es el nucleo del dominio funcional visible para el cliente. En el se concentra la gestion de envios, el calculo de tarifas, los pagos, la generacion de documentos, el perfil del cliente, el directorio de oficinas y el tracking. Es, por tanto, el servicio con mayor densidad de reglas de negocio orientadas a contratacion y seguimiento.

Su implementacion presenta varios puntos de interes.

#### A) Gestion de envios

El controlador de envios expone endpoints para cotizar, crear envios, consultar tracking, recuperar envios del usuario y obtener documentos asociados. Cuando se crea un envio, se generan dos identificadores: un numero de seguimiento publico y un numero de expedicion interno. Tambien se fija un estado publico y un estado interno inicial, lo que permite mantener dos niveles de visibilidad diferentes sobre el mismo objeto de negocio.

El mismo controlador incluye endpoints internos para que otros servicios publiquen cambios de ubicacion o eventos de entrega. Este detalle es importante porque situa a Ciudadano como dueño del tracking visible para el cliente, incluso cuando la informacion original procede de Reparto. En otras palabras, Reparto informa de la operativa, pero Ciudadano decide como esa operativa se persiste y se expone publicamente.

#### B) Oficinas postales

La busqueda de oficinas no depende de una tabla relacional sino de un JSON estatico que se carga en memoria y se consulta por codigo postal o texto libre. Esta aproximacion ha sido util para disponer de un catalogo rapido y realista de oficinas sin necesidad de crear una administracion completa sobre ese dato. Ademas, este mismo origen de datos sirve tanto para la experiencia de cliente como para algunos procesos internos, reduciendo duplicidades.

#### C) Motor de tarifas

El calculo de precios se concentra en un servicio dedicado de tarifas. Su responsabilidad va mas alla de devolver un importe: resuelve la zona del envio, calcula el peso volumetrico, determina el peso facturable, aplica recargos dimensionales, diferencia entre servicio estandar y premium, añade IVA y devuelve una estimacion temporal del servicio. Esta centralizacion evita incoherencias entre pantallas y asegura que cualquier proceso que necesite precio utilice la misma logica comercial.

#### D) Pagos y documentos

La integracion con Stripe se articula en un flujo completo:

1. El usuario autenticado solicita crear una sesion de pago.
2. El backend crea un envio en estado pendiente de pago.
3. Se genera una sesion de Stripe Checkout con URL de exito y cancelacion.
4. Tras el pago, el sistema verifica la sesion, por retorno del cliente o por webhook.
5. Si el pago es correcto, actualiza el envio, genera la etiqueta y la factura, y envia un correo de confirmacion.

Este proceso representa muy bien una integracion empresarial real. No se trata solo de "cobrar", sino de coordinar un conjunto de acciones dependientes del resultado del pago. El `PagosController` soporta creacion de sesion, verificacion posterior y reintento de pagos pendientes, mientras que el `StripeService` encapsula la comunicacion con la API externa. Cuando el pago se consolida, el sistema ejecuta un procesamiento adicional que genera documentos PDF, actualiza estados, registra fechas y notifica a logistica que el paquete debe ser admitido.

Ademas, el sistema incorpora un servicio en segundo plano que revisa periodicamente pagos pendientes para detectar confirmaciones que no hayan quedado correctamente reflejadas solo con el retorno del navegador. Esta capa aporta robustez y reduce el riesgo de inconsistencias en escenarios reales de navegacion o red.

#### E) Tracking y SignalR

El modulo ciudadano incorpora un `TrackingHub` publico al que los clientes se suscriben por numero de seguimiento. Los eventos principales emitidos son:

- EstadoActualizado.
- UbicacionActualizada.
- EntregaCompletada.
- IncidenciaDetectada.

La aplicacion cliente escucha estos eventos para refrescar visualmente la trazabilidad. El hub organiza suscripciones por grupos del tipo `tracking-{numero}`, lo que permite aislar los eventos de cada envio. Este mecanismo sirve de puente entre la operativa interna y la experiencia publica del usuario, y se complementa con un servicio de notificacion que abstrae la emision de eventos para no acoplarla a un unico controlador.

#### F) Sincronizacion con reparto

Ciudadano expone endpoints internos para que el modulo de reparto publique ubicaciones o notifique eventos de entrega. Cuando reparto comunica que un envio ha sido entregado, rechazado, intentado o devuelto, el modulo ciudadano transforma ese evento en un estado interno y un estado publico coherentes, actualiza la base de datos y emite las notificaciones SignalR correspondientes.

Este punto es de gran relevancia porque demuestra una integracion real entre dominios. La aplicacion no se limita a tener una pantalla de tracking decorativa, sino que enlaza el resultado operativo del repartidor con la informacion que ve el cliente. De hecho, una de las claves del diseño es que el estado publico nunca se actualiza de forma arbitraria desde el frontend: siempre procede de procesos reales de negocio consolidados en backend.

### 2.3.5 Microservicio de intranet y logistica

El modulo intranet modela la red logistica interna. Sus procesos principales son la admision, la resolucion de CTAs, la creacion de movimientos, la asignacion de tareas, la gestion de incidencias y el soporte al escaneo operativo. Desde el punto de vista arquitectonico, este servicio es el puente entre la contratacion del envio y la realidad fisica de la red logística.

#### A) CTAs y clasificacion

Los CTAs se representan como nodos principales de la red. Cada uno tiene codigo, nombre, area zonal, provincia, ciudad y capacidades como nodo aereo o maritimo. Este diseño permite representar una logistica basada en centros de tratamiento y rutas de transporte entre nodos.

La resolucion del CTA de destino se apoya en el codigo postal. Cuando se admite un paquete, el sistema determina a que centro debe dirigirse y, si el origen y el destino corresponden a CTAs distintos, crea automaticamente un movimiento troncal. En otras palabras, la admision no se limita a registrar que el paquete existe: decide en que punto de la red debe entrar y como debe comenzar a desplazarse.

#### B) Admision de paquetes

La admision es uno de los procesos mejor definidos del proyecto. Su flujo es el siguiente:

1. Se recibe una solicitud con los datos del paquete y del destino.
2. Se resuelve el CTA de destino segun el codigo postal.
3. Si el paquete debe viajar a otro nodo, se calcula el tipo de transporte mas adecuado y se crea un movimiento troncal.
4. Se notifica en tiempo real a los operarios del CTA destino.
5. Si existen datos minimos de ultima milla, se orquesta con el modulo de reparto la autoasignacion a una ruta.

Este flujo se implementa principalmente en el `AdmisionService`, que concentra la resolucion del CTA, la creacion del movimiento, la notificacion y la posible coordinacion con reparto. El `AdmisionController` expone tanto rutas de uso interno como endpoints protegidos mediante `X-Service-Key`, lo que permite que Ciudadano inicie el proceso sin abrir innecesariamente la operativa al exterior. Esto permite pasar de un pago realizado por el cliente a una accion operativa interna sin intervencion manual entre medias, lo cual representa un salto cualitativo importante en automatizacion.

#### C) Asignaciones y tareas

Las asignaciones representan tareas atomicas sobre expediciones concretas dentro de un CTA. Cada tarea tiene un tipo, un estado, un responsable, un creador, una fecha de asignacion y, en su caso, fechas de inicio y finalizacion. Con esta estructura, la intranet no solo conoce que un paquete existe, sino tambien que trabajo fisico debe realizarse sobre el y quien debe hacerlo.

La aplicacion interna permite crear asignaciones, filtrarlas por estado y ejecutar transiciones de inicio, finalizacion o cancelacion. Esto convierte la trazabilidad interna en algo accionable, porque cada estado se vincula con trabajo pendiente o completado por parte de operarios reales del sistema.

#### D) Escaneo logistico

La intranet incorpora un modulo de escaneo con lectura por camara y procesamiento individual o por lotes. Este modulo es relevante por dos motivos. El primero es que aporta rapidez operativa y reduce errores de introduccion manual. El segundo es que cada escaneo puede activar una accion de negocio distinta dependiendo del modo elegido, lo que lo convierte en una interfaz de alto valor practico.

El servicio de escaneo no es un lector aislado, sino una capa que valida codigos, consulta modos disponibles, procesa lotes y conserva historial de operaciones. Gracias a ello, el escaneo puede utilizarse para distintas etapas del circuito logístico, no solo como mecanismo de captura de datos.

#### E) Notificaciones internas y seguimiento operativo

El modulo intranet incorpora SignalR para notificaciones en tiempo real dirigidas a CTAs y perfiles concretos. De esta manera, eventos como la recepcion de un paquete, la asignacion de una tarea o la creacion de un movimiento pueden llegar directamente a los usuarios que deben actuar sobre ellos. Esta estrategia reduce la dependencia de refrescos manuales y refuerza la sensacion de sistema operativo vivo.

Ademas, la pantalla de seguimiento interno permite buscar expediciones por numero de seguimiento o por numero de expedicion y actualizar estados internos con mayor granularidad que en el tracking publico. Este punto es crucial porque demuestra que el sistema distingue claramente entre visibilidad para cliente y control de la operativa interna.

### 2.3.6 Microservicio de reparto

El modulo de reparto se encarga de la ultima milla, es decir, del tramo final del envio entre la oficina de destino y el destinatario. Incluye el modelo de repartidor, la definicion de rutas diarias y las entregas asociadas a cada ruta. Su importancia dentro del proyecto es alta porque conecta el mundo de la planificacion interna con la ejecucion en movilidad.

#### A) Repartidores

Cada repartidor se asocia a un usuario del sistema y a una oficina de referencia. Esto permite enlazar sus credenciales de acceso con su informacion operativa y con el contexto fisico desde el que trabaja. El microservicio expone endpoints para recuperar el perfil del repartidor autenticado y para cargar su contexto de trabajo sin depender de datos introducidos manualmente en cada sesion.

#### B) Rutas de reparto

Las rutas se identifican con codigos del tipo `REP-YYYYMMDD-XXX`, se asignan a una fecha y a un repartidor concreto, y agrupan varias entregas. Una ruta puede estar planificada, en curso o completada, y conserva informacion como hora de salida, hora de regreso y observaciones generales.

La propia ruta actua como unidad de trabajo diaria, permitiendo resumir progreso, volumen de entregas y resultados de la jornada. Desde el backend, el servicio de reparto soporta carga de la ruta activa, inicio de jornada, finalizacion y consulta de entregas asociadas, de forma que el frontend movil puede reconstruir el contexto completo de la operativa del dia.

#### C) Entregas

Cada entrega modela una parada real de la ruta y contiene informacion detallada sobre direccion, codigo postal, ciudad, destinatario, telefono, intento, orden en la ruta, estado, fecha, receptor, coordenadas, firma y foto. Esta riqueza de datos hace que el sistema sea apto tanto para el seguimiento operativo como para la aportacion de evidencias.

La confirmacion de entrega no se reduce a cambiar un estado booleano. El servicio admite distintos resultados operativos, observaciones y pruebas asociadas, lo que acerca la solucion a una operativa real de paqueteria y deja preparado el sistema para auditoria o consulta posterior.

#### D) Ubicacion y tracking

El modulo de reparto no se limita a gestionar estados. Tambien expone un endpoint para registrar ubicacion y utiliza un servicio de notificacion a Ciudadano para sincronizar el tracking en tiempo real. Gracias a esto, la posicion del repartidor puede traducirse en informacion visible para el cliente.

Esta relacion entre servicios es especialmente relevante porque demuestra que Reparto no es un modulo aislado de movilidad, sino una fuente activa de informacion para el resto del sistema. El backend de reparto consolida los datos de la ejecucion de ruta y los transforma en eventos consumibles por el dominio ciudadano.

#### E) Autoasignacion desde admision

Una funcionalidad especialmente interesante del proyecto es el endpoint interno para autoasignar una entrega desde la admision. Cuando logistica recibe un envio con los datos minimos necesarios, puede solicitar a reparto que seleccione un repartidor, cree o reutilice una ruta del dia y anada la entrega. Este proceso incorpora idempotencia basica por numero de expedicion para evitar duplicados en reintentos.

Desde el punto de vista funcional, esta capacidad es una de las que mejor representan el valor del sistema distribuido: un evento originado por el cliente, consolidado por el pago y admitido por logistica puede terminar, sin pasos manuales intermedios, convertido en una entrega planificada para un repartidor concreto.

### 2.3.7 Aplicacion de reparto: GPS, offline y mapa

La aplicacion de repartidores es probablemente una de las piezas mas avanzadas del proyecto desde el punto de vista tecnico, porque combina operativa de negocio con problemas propios de movilidad.

#### A) Seguimiento GPS

Cuando una ruta pasa a estado `EnCurso`, la app activa el seguimiento GPS. Para ello combina varias estrategias:

- `watchPosition` para recibir actualizaciones continuas del navegador.
- Envio inicial al empezar la ruta.
- Heartbeat periodico para mantener actividad aunque no haya cambios frecuentes.
- Ajuste del comportamiento cuando la app entra en segundo plano.
- Reintentos progresivos si fallan las comunicaciones.

La implementacion incorpora ademas control del ciclo de vida de la pagina, reactivacion del seguimiento al volver a primer plano y gestion de temporizadores auxiliares. Esta logica demuestra que no se ha tratado la geolocalizacion como una simple llamada puntual, sino como un proceso continuo con consideraciones de rendimiento, bateria y conectividad.

#### B) Cola offline

La app mantiene una cola en `localStorage` para dos tipos de eventos: confirmaciones de entrega y ubicaciones pendientes. Si el dispositivo pierde conexion, los datos no se descartan. En su lugar, quedan encolados y se reintentan cuando vuelve la conectividad. Esta decision es clave en un contexto de reparto, donde el uso en movilidad hace que la cobertura no siempre sea estable.

La existencia de este servicio offline convierte a la app en una herramienta utilizable en condiciones reales de campo. Incluso aunque no exista sincronizacion completa bidireccional ni una base local avanzada, la solucion logra preservar las acciones mas criticas para que la jornada no quede bloqueada por una perdida temporal de red.

#### C) Mapa y navegacion

La vista de ruta integra Leaflet y OpenStreetMap para representar:

- La posicion actual del repartidor.
- El historial reciente de la traza GPS.
- Los puntos de entrega con coordenadas disponibles.
- La entrega seleccionada o la siguiente parada.

Ademas, se ofrecen enlaces rapidos a Google Maps y Waze para facilitar la navegacion externa. Aunque esto no equivale a un navegador embebido completo, proporciona una solucion util y realista dentro del alcance del proyecto. La decision de apoyarse en Leaflet y OpenStreetMap permite disponer de cartografia funcional sin depender de servicios comerciales cerrados para la visualizacion principal.

#### D) Confirmacion de entrega con evidencias

La app permite confirmar distintas situaciones: entrega correcta, entrega en punto alternativo, ausente, direccion incorrecta, rechazo o devolucion a oficina. Junto a ese estado, el repartidor puede registrar el nombre del receptor, su DNI, observaciones, firma digital, foto y coordenadas. Todo ello convierte la confirmacion de entrega en un registro mucho mas rico que un simple cambio de estado.

La aplicacion incluye ademas una pantalla de escaneo operativo, capaz de consultar expediciones y sugerir la siguiente accion interna. Esto conecta la actividad del repartidor con la logica de estados del sistema y evita tratar la app movil como una simple lista de paradas.

### 2.3.8 API Gateway y control de acceso

El API Gateway centraliza el acceso desde los distintos frontends y sirve como punto unico de entrada a la capa de microservicios. Entre sus funciones destacan:

- Aplicar CORS de forma centralizada.
- Integrar autenticacion JWT.
- Determinar que rutas son publicas y cuales protegidas.
- Orquestar el enrutamiento hacia el backend correspondiente.

Una decision destacable es la definicion explicita de rutas publicas para login, registro, refresh, recuperacion de contraseña, consulta de tracking, consulta de tarifas, webhook de Stripe y busqueda de oficinas. El resto de rutas queda protegido por defecto. De este modo, el gateway refleja claramente la frontera entre lo que debe estar abierto al exterior y lo que requiere contexto autenticado.

Esta pieza tiene un valor especial dentro de la arquitectura porque evita que cada microservicio tenga que replicar exactamente la misma logica de entrada externa. Aunque los servicios mantienen su propia seguridad interna, el gateway concentra la politica de exposicion publica y simplifica a los frontends el consumo de APIs. Tambien facilita una futura evolucion de la plataforma, por ejemplo si se desea introducir limitacion de peticiones, trazas centralizadas o politicas mas avanzadas de observabilidad y seguridad en el punto de entrada.

## 2.4 Implantacion, despliegue e instalacion

### 2.4.1 Entorno de desarrollo local

Para desarrollo local, NexoPostal se apoya en Docker Compose. Esto permite que cualquier entorno con Docker pueda levantar toda la plataforma sin necesidad de instalar manualmente cada dependencia por separado. La decision es especialmente util en un proyecto con tres frontends, varios backends y varias bases de datos, donde la puesta en marcha manual seria mucho mas propensa a errores.

La configuracion local incluye:

- Proxy Nginx sin SSL.
- API Gateway.
- Tres aplicaciones Angular.
- Cuatro microservicios principales.
- Cuatro bases de datos PostgreSQL.

En la practica, `docker-compose.yml` ofrece un entorno completo de extremo a extremo para desarrollo local. La web de clientes, la intranet y la app de reparto se sirven a traves del proxy local, mientras que gateway, microservicios y bases de datos conviven en una misma red Docker. Esto permite probar flujos completos como registro, contratacion, pago, admision, autoasignacion, tracking y entrega sin necesidad de cambiar manualmente URLs ni levantar componentes uno a uno.

El hecho de levantar la plataforma completa desde un unico archivo simplifica la puesta en marcha y reduce el coste de incorporacion de nuevos desarrolladores o de nuevas pruebas en otras maquinas. Ademas, mejora la paridad con produccion, ya que la topologia general de servicios se mantiene.

### 2.4.2 Configuracion en contenedores

Cada frontend dispone de su propio Dockerfile, al igual que cada microservicio .NET. Esta decision permite construir imagenes independientes y desplegarlas de forma desacoplada. No todos los componentes tienen el mismo ciclo de cambio ni las mismas necesidades de recursos, por lo que empaquetarlos por separado resulta coherente con la arquitectura general.

Las variables de entorno se inyectan mediante archivos `.env` y configuracion jerarquica por servicio, lo que facilita adaptar el comportamiento entre desarrollo y produccion sin modificar el codigo fuente. Gracias a ello, la misma solucion puede conectarse a bases de datos distintas, cambiar secretos JWT, activar configuraciones de Stripe o variar URLs internas sin recompilar toda la plataforma.

El uso de contenedores aporta varias ventajas:

- Reproducibilidad del entorno.
- Separacion clara entre servicios.
- Facil integracion con pipelines CI/CD.
- Mayor control sobre dependencias y puertos.
- Posibilidad de actualizar o reiniciar modulos concretos sin afectar a toda la plataforma.

### 2.4.3 Despliegue en produccion

En produccion, el sistema se despliega en un VPS, utilizando imagenes publicadas en GHCR. El despliegue contempla Nginx con SSL, sin exposicion directa de las bases de datos, y con todos los servicios conectados a una red interna. Esta separacion entre compilacion y despliegue es importante: el servidor no necesita construir el proyecto, sino solo descargar imagenes ya validadas y ejecutar la nueva version.

La publicacion se realiza bajo varios dominios y subdominios, con una separacion clara entre clientes, intranet y reparto. Los certificados del proxy deben provisionarse previamente en el servidor para que la entrada HTTPS funcione correctamente, lo cual refleja una necesidad operativa real de cualquier despliegue web serio.

Este modelo es adecuado para un proyecto TFG porque combina realismo tecnico con una infraestructura asumible. No requiere una plataforma cloud compleja, pero al mismo tiempo obliga a trabajar con dominios, certificados, registros, variables seguras y coordinacion de servicios.

### 2.4.4 Integracion y despliegue continuo

El proyecto incluye un workflow de GitHub Actions que automatiza buena parte del ciclo de entrega. Cuando se produce un push sobre ramas relevantes, el pipeline:

1. Compila los microservicios .NET.
2. Compila las aplicaciones Angular.
3. Construye las imagenes Docker de todos los servicios.
4. Publica dichas imagenes en GitHub Container Registry.
5. En rama master, conecta con el VPS y ejecuta el despliegue por `pull` y `compose up`.

El valor de este pipeline no es solo tecnico, sino metodologico. El proyecto no termina cuando el codigo compila en local, sino cuando existe un proceso repetible para construir, publicar y desplegar una version. Ademas, el uso de GHCR reduce el tiempo de despliegue en el VPS, ya que este solo necesita descargar imagenes y relanzar los contenedores necesarios.

Esta automatizacion tiene un valor muy importante dentro del TFG porque demuestra una vision completa del ciclo de vida del software. No se entrega solo codigo fuente, sino una solucion preparada para integracion continua y despliegue automatizado.

### 2.4.5 Configuracion de red y proxy

Nginx actua como reverse proxy, resolviendo que dominio o subdominio debe servir cada aplicacion. Esto permite exponer, por ejemplo, una URL para clientes, otra para intranet y otra para driver. A nivel conceptual, esta separacion mejora la organizacion del sistema y se alinea con la segmentacion funcional ya presente en el resto de la arquitectura.

La configuracion del proxy es relevante no solo por el enrutado HTTP tradicional, sino tambien por el soporte a conexiones WebSocket, necesarias para los hubs de SignalR. Ademas, el despliegue incorpora una mitigacion frente al cacheo de DNS interno de Docker mediante el uso de un `resolver` explicito, evitando problemas de cruce entre dominios o backends tras recrear contenedores. Este tipo de ajuste demuestra que la implantacion no se ha quedado en un ejemplo teorico, sino que ha tenido en cuenta incidencias reales de operacion.

### 2.4.6 Monitorizacion y operacion

Aunque la monitorizacion no constituye el eje principal del TFG, el proyecto contempla un `docker-compose.monitoring.yml` con componentes como Watchtower, Prometheus, Grafana, Loki y Promtail. Esto muestra una preocupacion por la operacion real del sistema y por la posibilidad de evolucionarlo hacia un entorno con metricas, logs agregados y actualizaciones automatizadas.

La presencia de este stack complementario es importante porque abre la puerta a observar la plataforma desde dos perspectivas distintas: salud tecnica de servicios y comportamiento funcional del negocio. Aunque no toda esta observabilidad este explotada al maximo en la version actual, la base para hacerlo ya forma parte del proyecto.

## 2.5 Documentacion

### 2.5.1 Documentacion tecnica del codigo

La documentacion tecnica del proyecto se apoya en varios niveles:

- Un README general del repositorio que resume arquitectura, modulos, credenciales de desarrollo y formas de despliegue.
- Comentarios XML en controladores y servicios del backend.
- Documentacion funcional adicional que recoge el estado de prioridades, backlog y mapa de endpoints.
- Estructura del repositorio organizada por aplicaciones y microservicios.

En los backends .NET se ha trabajado con comentarios que describen el objetivo de controladores, endpoints y servicios, lo cual facilita la comprension del sistema y permite generar documentacion complementaria mediante Swagger u otras herramientas.

### 2.5.2 Documentacion de API

Los microservicios principales cuentan con configuracion de OpenAPI o Swagger, lo que permite inspeccionar contratos, endpoints y mecanismos de autenticacion. Este aspecto es especialmente util para depuracion, pruebas e integracion entre equipos.

Los endpoints mas relevantes del sistema son:

En Ciudadano:

- Cotizacion y creacion de envios.
- Consulta de tracking publico.
- Consulta de envios del usuario.
- Descarga de etiqueta y factura.
- Consulta y actualizacion de perfil.
- Endpoints internos de tracking.

En Auth:

- Login, registro y refresh.
- Consulta del usuario autenticado.
- Recuperacion y reseteo de contraseña.

En Intranet:

- Admision de paquetes.
- Asignaciones.
- Movimientos.
- Incidencias.

En Reparto:

- Perfil del repartidor.
- Ruta del dia.
- Rutas por identificador.
- Entregas.
- Confirmacion de entrega.
- Registro de ubicacion.
- Autoasignacion interna.

### 2.5.3 Documentacion de usuario

La propia estructura de las tres aplicaciones funciona como base para un manual de usuario. De cara a la memoria, conviene incluir capturas de pantalla en esta seccion o en anexos. Algunas capturas recomendadas son:

- Pantalla de inicio de clientes.
- Wizard de nuevo envio en sus tres pasos.
- Tracking publico con barra de progreso.
- Panel de usuario con agenda de direcciones.
- Dashboard de intranet.
- Gestion de CTA.
- Pantalla de asignaciones.
- Pantalla de escaneo logistico.
- Dashboard de driver app.
- Vista de ruta con mapa y lista de entregas.

### 2.5.4 Organizacion del repositorio

El repositorio esta organizado por areas funcionales, lo cual facilita localizar el codigo y comprender la arquitectura.

- `clientes-app`: frontend de clientes.
- `intranet-app`: frontend de operativa interna.
- `driver-app`: frontend de reparto.
- `microservicios/Nexopostal/Nexopostal.Auth`: autenticacion.
- `microservicios/Nexopostal/Nexopostal.Ciudadano`: dominio ciudadano.
- `microservicios/Nexopostal/Nexopostal.Intranet`: dominio logístico.
- `microservicios/Nexopostal/Nexopostal.Reparto`: dominio de reparto.
- `microservicios/Nexopostal/Nexopostal.Gateway`: gateway.
- `nginx`: configuraciones del proxy.
- `docker-compose*.yml`: escenarios de ejecucion y despliegue.

Esta organizacion refuerza la mantenibilidad del proyecto y hace visible, desde la propia estructura de carpetas, la modularidad buscada durante el desarrollo.

---

# 3. Resultados y discusion

El resultado final del proyecto puede considerarse claramente satisfactorio en relacion con los objetivos planteados al inicio. Se ha conseguido construir una plataforma funcional, compuesta por varias aplicaciones y varios servicios, que reproduce de forma realista los procesos esenciales de una empresa de paqueteria: contratacion del envio, pago, admision logistica, reparto y seguimiento publico.

La evaluacion de los resultados puede hacerse en varios planos.

En el plano funcional, el sistema cubre un ciclo de negocio completo. El cliente puede registrarse, autenticarse, calcular tarifas, contratar un envio, pagar mediante Stripe, consultar su historial, descargar documentos y seguir la evolucion del paquete. Paralelamente, la intranet permite admitir el paquete en la red, resolver CTAs, crear tareas, trabajar con escaneo y gestionar seguimiento interno. Finalmente, el modulo de reparto permite planificar o reutilizar rutas, registrar entregas, emitir ubicaciones y alimentar el tracking visible para el cliente.

En el plano arquitectonico, el proyecto demuestra que la separacion por dominios no se ha quedado en una decision superficial. Cada microservicio tiene responsabilidades razonablemente bien delimitadas, cada frontend responde a un perfil de usuario distinto y la integracion entre modulos se apoya en contratos claros. La existencia de dos identificadores por envio, la separacion entre estado interno y estado publico, y la combinacion de HTTP con SignalR son ejemplos concretos de decisiones de diseño que aportan coherencia y valor.

En el plano tecnico, el trabajo alcanza un nivel superior al de una aplicacion academica elemental. No solo se han implementado CRUDs o pantallas simples, sino integraciones con Stripe, emision de documentos, autenticacion JWT con refresh tokens, colas offline en movilidad, notificaciones en tiempo real, despliegue con Docker y Nginx, y automatizacion CI/CD mediante GitHub Actions. Este conjunto de piezas hace que el proyecto tenga un grado apreciable de realismo.

En el plano operativo, tambien se han obtenido resultados relevantes. La plataforma puede levantarse en local con todos sus componentes, puede desplegarse en un VPS con imagenes en GHCR y cuenta con una base de monitorizacion ampliable. Esto demuestra que el trabajo no se ha quedado en el desarrollo aislado del codigo, sino que ha considerado el ciclo de vida completo de la solucion.

Uno de los logros mas importantes del proyecto es la coherencia entre capas. La interfaz del cliente no esta desconectada del backend, el backend ciudadano no esta aislado de la intranet, y el reparto no funciona como una demo separada, sino como una pieza que participa activamente en el ciclo completo del envio. Esta continuidad funcional es probablemente el mejor indicador de madurez del sistema.

No obstante, tambien es importante discutir las limitaciones y puntos de mejora. Aunque la arquitectura es adecuada para el alcance del TFG, la comunicacion entre microservicios todavia puede robustecerse. Actualmente se ha implementado seguridad interservicio mediante `X-Service-Key` y sincronizacion por llamadas HTTP, pero en un escenario de mayor volumen seria recomendable introducir patrones de mensajeria mas resistentes a fallos transitorios, como outbox/inbox, deduplicacion por identificador de evento y trazabilidad distribuida.

Otra limitacion esta en la parte de reparto. La aplicacion movil web incorpora un seguimiento GPS trabajado y una cola offline util, pero sigue dependiendo de las capacidades del navegador y del sistema operativo. Esto significa que la experiencia en segundo plano y la estabilidad de la geolocalizacion no alcanzan todavia el nivel de una aplicacion nativa especializada.

Tambien puede mejorarse la parte de navegacion de rutas. Actualmente se ofrecen enlaces a aplicaciones externas como Google Maps o Waze, lo cual es practico y suficiente para el alcance actual, pero no equivale a disponer de un motor embebido con ETA, recalculo o asistente de conduccion integrado.

Desde la perspectiva operativa, existe ademas una dependencia clara de la infraestructura del servidor en aspectos como el correo SMTP o la provision de certificados. Esto no invalida el proyecto, pero si recuerda que una solucion completa requiere no solo buen codigo, sino tambien condiciones de operacion adecuadas.

En resumen, el proyecto ofrece resultados claramente positivos. No solo cumple su funcion principal, sino que ademas demuestra madurez de diseño, coherencia entre modulos y un nivel tecnico superior al de una aplicacion academica elemental. Al mismo tiempo, conserva un margen de mejora realista y bien identificado, lo cual refuerza la credibilidad de la memoria.

---

# 4. Trabajo futuro (opcional)

El desarrollo de NexoPostal deja abierta una linea muy interesante de evolucion. Lejos de considerarse una aplicacion cerrada, la plataforma puede seguir creciendo en varias direcciones, y muchas de ellas no responden a carencias graves, sino a una progresion natural de madurez del producto.

Una primera linea de trabajo futuro consistiria en reforzar la robustez de la arquitectura distribuida. Para ello seria recomendable incorporar un sistema de mensajeria con garantias de entrega, colas persistentes o patrones outbox/inbox. Esto permitiria reducir el acoplamiento temporal entre servicios, mejorar la fiabilidad ante caidas parciales o problemas de red y ofrecer mejor trazabilidad de los eventos de negocio.

Una segunda linea estaria orientada a la seguridad y a la operacion. Aunque el sistema ya utiliza JWT, refresh tokens y proteccion basica entre servicios, seria deseable evolucionar hacia autenticacion fuerte servicio a servicio mediante JWT internos firmados o mTLS. Tambien podria anadirse auditoria avanzada de acciones sensibles, politicas de rotacion de secretos, gestion mas formal de certificados y alternativas al correo SMTP tradicional mediante proveedores con API HTTPS.

Una tercera linea se centraria en la experiencia del repartidor. Las mejoras mas relevantes en este area serian la navegacion embebida en el mapa, el calculo de ETA por parada, la reordenacion inteligente de rutas segun trafico o prioridad y un mejor soporte para segundo plano mediante tecnologia nativa o hibrida. Con ello, la app de reparto podria pasar de ser una herramienta web avanzada a una solucion de movilidad aun mas robusta.

Una cuarta linea de trabajo estaria relacionada con la observabilidad y el control operativo. Seria interesante desplegar dashboards reales con metricas de negocio y de sistema, como volumen de envios por dia, tiempo medio de entrega, entregas fallidas, ocupacion por CTA, salud de servicios, latencia de APIs o tasa de errores en sincronizacion interservicio. Esta informacion seria valiosa no solo para operacion tecnica, sino tambien para toma de decisiones de negocio.

Una quinta linea de evolucion tendria un enfoque mas funcional y comercial: notificaciones push al cliente, integracion con almacenes o ERPs, gestion de reembolsos o seguros, programacion de recogidas, soporte para multipaquete y apertura a envios internacionales. Todas estas opciones amplian el alcance del producto sin romper la base ya construida.

Finalmente, seria muy valioso ampliar la cobertura de pruebas, incorporando mas tests de integracion y escenarios end to end que recorran los flujos criticos completos: registro, envio, pago, admision, autoasignacion a reparto, tracking y entrega. Esta linea de mejora tendria un impacto directo en la mantenibilidad futura de la plataforma.

---

# 5. Conclusiones

La realizacion del proyecto NexoPostal ha permitido demostrar que es posible diseñar y desarrollar una plataforma de paqueteria completa dentro del marco de un TFG, siempre que se adopte una metodologia ordenada, se delimite bien el alcance y se mantenga una arquitectura coherente. A lo largo del trabajo se ha pasado por todas las fases fundamentales del desarrollo de software: analisis del problema, definicion de requisitos, diseño de la arquitectura, implementacion de la solucion, integracion de modulos, despliegue y documentacion.

Una de las principales conclusiones es que la separacion por dominios ha sido una decision acertada. Dividir el sistema entre cliente, logistica y reparto, y respaldar esa division con microservicios especificos, ha permitido construir una solucion mas clara, mantenible y realista. Al mismo tiempo, esta decision ha obligado a afrontar retos autenticos de sincronizacion, seguridad, orquestacion y despliegue que enriquecen mucho el valor academico del proyecto.

Tambien puede concluirse que la incorporacion de procesos reales, como pagos con Stripe, generacion de documentos, tracking con SignalR, escaneo logístico, autoasignacion a reparto y soporte offline en movilidad, ha elevado significativamente la calidad del trabajo. El resultado final no es una demostracion aislada de funcionalidades inconexas, sino un sistema donde las piezas colaboran para sostener un flujo completo de negocio desde la contratacion hasta la entrega.

Desde una perspectiva formativa, el proyecto ha servido para consolidar conocimientos de Angular, ASP.NET Core, Entity Framework, PostgreSQL, Docker, Nginx, GitHub Actions, integracion de APIs externas, seguridad con JWT y comunicacion en tiempo real. Pero, mas alla de las tecnologias concretas, tambien ha permitido desarrollar competencias de analisis, toma de decisiones arquitectonicas, organizacion del codigo y razonamiento sobre requisitos funcionales y no funcionales.

La conclusion mas importante es que NexoPostal no solo cumple el objetivo de funcionar, sino el de justificar tecnicamente sus decisiones. El sistema presenta una arquitectura defendible, unos flujos de negocio coherentes, una implantacion realista y un margen de evolucion claramente identificado. Por todo ello, puede afirmarse que NexoPostal cumple el objetivo del TFG no solo por su nivel de funcionalidad, sino porque representa una solucion tecnicamente consistente, bien estructurada y con capacidad real de evolucion.

---

# 6. Bibliografia

La siguiente bibliografia puede utilizarse como base para la memoria. Conviene revisar el formato que exija tu centro y normalizarlo, por ejemplo en APA o IEEE.

1. Angular Team. Angular Documentation. Disponible en: https://angular.dev/  Consultado el 16 de mayo de 2026.
2. Microsoft. ASP.NET Core documentation. Disponible en: https://learn.microsoft.com/aspnet/core/  Consultado el 16 de mayo de 2026.
3. Microsoft. SignalR for ASP.NET Core. Disponible en: https://learn.microsoft.com/aspnet/core/signalr/introduction  Consultado el 16 de mayo de 2026.
4. Microsoft. Entity Framework Core documentation. Disponible en: https://learn.microsoft.com/ef/core/  Consultado el 16 de mayo de 2026.
5. PostgreSQL Global Development Group. PostgreSQL Documentation. Disponible en: https://www.postgresql.org/docs/  Consultado el 16 de mayo de 2026.
6. Docker, Inc. Docker Documentation. Disponible en: https://docs.docker.com/  Consultado el 16 de mayo de 2026.
7. NGINX. NGINX Documentation. Disponible en: https://nginx.org/en/docs/  Consultado el 16 de mayo de 2026.
8. Stripe, Inc. Stripe Checkout Documentation. Disponible en: https://docs.stripe.com/payments/checkout  Consultado el 16 de mayo de 2026.
9. GitHub. GitHub Actions Documentation. Disponible en: https://docs.github.com/actions  Consultado el 16 de mayo de 2026.
10. Leaflet contributors. Leaflet Documentation. Disponible en: https://leafletjs.com/reference.html  Consultado el 16 de mayo de 2026.
11. scanapp.org. html5-qrcode Documentation. Disponible en: https://scanapp.org/html5-qrcode-docs/  Consultado el 16 de mayo de 2026.
12. MDN Web Docs. Geolocation API. Disponible en: https://developer.mozilla.org/  Consultado el 16 de mayo de 2026.

---

# Anexos

## Anexo I. Estructura general de la solucion

La solucion esta formada por los siguientes bloques:

- Frontend clientes.
- Frontend intranet.
- Frontend driver.
- API Gateway.
- Microservicio Auth.
- Microservicio Ciudadano.
- Microservicio Intranet.
- Microservicio Reparto.
- Nginx.
- PostgreSQL por microservicio.
- Integraciones externas: Stripe, SMTP y SignalR.

## Anexo II. Resumen de entidades principales

### Dominio Auth

- ApplicationUser.

### Dominio Ciudadano

- ClientePerfil.
- DireccionFavorita.
- Envio.
- Estados publicos e internos.

### Dominio Intranet

- CentroTratamiento.
- OperarioCta.
- OperarioOficina.
- RutaCta.
- AsignacionPaquete.
- MovimientoPaquete.
- Incidencia.

### Dominio Reparto

- Repartidor.
- RutaReparto.
- EntregaPaquete.

## Anexo III. Endpoints relevantes del sistema

### Auth

- POST /api/auth/login
- POST /api/auth/register
- POST /api/auth/refresh
- GET /api/auth/me
- POST /api/auth/solicitar-reset
- POST /api/auth/reset-password

### Ciudadano

- POST /api/envios/cotizar
- POST /api/envios/crear
- GET /api/envios/track/{numero}
- GET /api/envios/mis-envios
- GET /api/envios/etiqueta/{numero}
- GET /api/envios/factura/{numero}
- GET /api/perfil
- POST /api/perfil
- GET /api/perfil/direcciones
- POST /api/pagos/crear-sesion
- GET /api/pagos/verificar/{sessionId}
- POST /api/pagos/reintentar/{numero}

### Intranet

- POST /api/admision/paquete
- POST /api/admision/interno/paquete
- POST /api/asignaciones
- PUT /api/asignaciones/{id}/iniciar
- PUT /api/asignaciones/{id}/completar
- POST /api/movimientos
- PUT /api/movimientos/{id}/despachar
- PUT /api/movimientos/{id}/recibir
- POST /api/incidencias

### Reparto

- GET /api/reparto/mi-perfil
- GET /api/reparto/ruta
- GET /api/reparto/rutas
- GET /api/reparto/rutas/{id}
- POST /api/reparto/rutas/{id}/iniciar
- POST /api/reparto/rutas/{id}/finalizar
- GET /api/reparto/entregas
- POST /api/reparto/confirmar
- POST /api/reparto/ubicacion
- POST /api/reparto/interno/admision/auto-asignar

## Anexo IV. Capturas recomendadas para insertar en Word

1. Portada visual del proyecto o logotipo.
2. Pantalla principal de clientes.
3. Pantalla de calculadora de tarifas.
4. Paso 1 del wizard de envio.
5. Paso 2 del wizard de envio.
6. Paso 3 del wizard con tarifas.
7. Tracking publico con barra de progreso.
8. Panel de usuario con envios.
9. Dashboard de intranet.
10. Gestion de CTA con metricas.
11. Asignaciones de tareas.
12. Escaneo logistico.
13. Dashboard de repartidor.
14. Ruta con mapa, entregas y progreso.
15. Confirmacion de entrega.

## Anexo V. Posibles diagramas para enriquecer la memoria

- Diagrama de arquitectura general del sistema.
- Diagrama de despliegue con Docker y Nginx.
- Diagrama de secuencia del flujo cliente -> pago -> admision -> reparto.
- Diagrama de estados del envio.
- Diagrama entidad-relacion simplificado por microservicio.

---

Fin del borrador.