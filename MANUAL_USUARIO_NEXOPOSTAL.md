# Manual de Usuario de NexoPostal

## 1. Objetivo del manual

El presente documento constituye el anexo de uso funcional de NexoPostal. Su finalidad es explicar el manejo de la plataforma desde la perspectiva de cada tipo de usuario, sin descender al detalle de codigo o arquitectura interna.

El manual se divide por perfiles para que cada persona consulte solo la parte que necesita:

- Cliente.
- Operario de oficina.
- Operario CTA.
- Supervisor.
- Administrador.
- Repartidor.
- Jefe de reparto.

---

## 2. Conceptos basicos previos

Antes de usar la plataforma, es util distinguir cuatro conceptos:

- `Numero de seguimiento`: identificador publico que ve el cliente.
- `Numero de expedicion`: identificador interno que usa operativa.
- `CTA`: Centro de Tratamiento Automatizado donde se clasifica y mueve el paquete.
- `DisponibleParaReparto`: estado interno que indica que el paquete ya puede incorporarse a una ruta de ultima milla.

Tambien es importante recordar que la plataforma no es una sola aplicacion, sino tres:

- Web de clientes.
- Intranet interna.
- App de reparto.

---

## 3. Acceso segun el perfil

### 3.1 Cliente

Accede a la web publica de clientes. Desde ella puede:

- Registrarse.
- Iniciar sesion.
- Calcular tarifas.
- Contratar envios.
- Consultar tracking.
- Gestionar perfil y direcciones.

### 3.2 Operarios y administracion interna

Acceden a la intranet. Segun su rol, veran opciones distintas:

- `OperarioOficina`.
- `OperarioCTA`.
- `Supervisor`.
- `Admin`.

### 3.3 Reparto

Accede a la app de reparto. Segun el rol:

- `Repartidor`: ruta y entregas.
- `JefeReparto`: planificacion y supervision.

---

## 4. Manual de cliente

### 4.1 Registro de cuenta

Pasos recomendados:

1. Abrir la web publica.
2. Ir a la opcion de registro.
3. Introducir nombre completo, email y contrasena.
4. Confirmar el formulario.
5. Iniciar sesion con la nueva cuenta.

Consejo:

- Utiliza un email real si vas a probar reseteo de contrasena o recepcion de documentos por correo.

### 4.2 Inicio de sesion

Pasos:

1. Acceder a la opcion de login.
2. Introducir email y contrasena.
3. Entrar al panel del usuario si la autenticacion es correcta.

Si el acceso falla:

- Comprueba si el email esta bien escrito.
- Verifica que estas entrando en la aplicacion correcta.
- Usa la opcion de reseteo si no recuerdas la contrasena.

### 4.3 Recuperar contrasena

Pasos:

1. Elegir la opcion de "He olvidado mi contrasena".
2. Introducir el correo electronico.
3. Revisar el email recibido.
4. Abrir el enlace de reseteo.
5. Establecer una nueva contrasena.

### 4.4 Calcular tarifa de un envio

La calculadora permite estimar el coste antes de contratar.

Pasos:

1. Ir a la calculadora de tarifas.
2. Introducir codigo postal de origen y de destino.
3. Introducir peso, largo, ancho y alto.
4. Ejecutar el calculo.

La pantalla devolvera:

- Zona logistica.
- Peso facturable.
- Tarifa estandar.
- Tarifa premium.
- Tiempo estimado.
- Aviso de recargo si las dimensiones lo requieren.

Importante:

- Esta calculadora consulta el backend. No es una simulacion local separada.

### 4.5 Buscar oficinas

La seccion de oficinas sirve para localizar puntos fisicos de entrega o recogida.

Formas de busqueda:

- Por codigo postal.
- Por direccion o ciudad.
- Por ubicacion actual, si el navegador lo permite.

Pasos:

1. Abrir el buscador de oficinas.
2. Elegir el tipo de busqueda.
3. Escribir la consulta.
4. Seleccionar una oficina del listado o del mapa.

La ficha mostrara, cuando exista informacion disponible:

- Nombre de la oficina.
- Direccion.
- Ciudad y codigo postal.
- Horario.
- Servicios.

### 4.6 Crear un envio online

La contratacion del envio se realiza con un asistente por pasos.

### Paso 1: remitente y oficina de admision

Debes introducir:

- Nombre y apellidos.
- Telefono.
- Email.
- DNI, cuando el envio lo requiera.
- Oficina donde vas a entregar fisicamente el paquete.

Importante:

- En la version actual, los envios contratados online no incluyen recogida a domicilio en origen.
- Debes llevar el paquete a una oficina postal.

### Paso 2: destinatario y modalidad de entrega

Debes introducir:

- Nombre y apellidos del destinatario.
- Telefono.
- Email opcional.
- DNI, si aplica.
- Modalidad de entrega:
  - domicilio
  - oficina

Si eliges domicilio, deberas completar direccion, ciudad, provincia y codigo postal.

Si eliges oficina, deberas buscar y seleccionar la oficina destino.

### Paso 3: paquete, tarifa y pago

Debes introducir:

- Peso.
- Largo.
- Ancho.
- Alto.

Despues podras:

- Calcular las tarifas disponibles.
- Comparar estandar y premium.
- Seleccionar el servicio.
- Confirmar el importe final.
- Ir a Stripe para pagar.

### Validaciones importantes

El sistema puede mostrar avisos por:

- Peso superior al maximo permitido.
- Dimensiones insuficientes para etiquetado.
- Lado mayor excesivo.
- Recargo por exceso dimensional.
- DNI obligatorio en envios que impliquen Canarias.

### 4.7 Pago del envio

Una vez confirmado el precio final:

1. El sistema te redirige a Stripe Checkout.
2. Realizas el pago fuera de NexoPostal.
3. Stripe te devuelve a la web.

### Si el pago se completa

Veras una pantalla de exito y el sistema verificara la sesion.

Resultado esperado:

- El envio quedara pagado.
- Se generaran los documentos.
- El paquete entrara en el circuito logístico interno.

### Si el pago se cancela

No hace falta rehacer el formulario completo. El envio puede quedar pendiente de pago y reintentarse mas adelante.

### 4.8 Tracking publico

El tracking publico muestra el estado del envio mediante:

- barra de progreso
- estado actual
- historial de eventos
- actualizaciones en tiempo real

Pasos:

1. Ir a la pagina de tracking.
2. Introducir el numero de seguimiento.
3. Consultar el estado.

Si hay conexion en tiempo real disponible, la pantalla puede actualizarse sin recargar.

### 4.9 Panel de usuario

El panel del usuario se divide en tres pestanas principales:

- `Mi perfil`.
- `Agenda de direcciones`.
- `Mis envios`.

### Mi perfil

Permite:

- editar nombre, email y telefono
- actualizar DNI y telefono de perfil ciudadano
- elegir direccion predeterminada
- cambiar la contrasena

### Agenda de direcciones

Permite:

- anadir direcciones favoritas
- editar una direccion existente
- eliminar direcciones
- reutilizarlas despues durante la contratacion

### Mis envios

Permite:

- consultar envios anteriores
- revisar estado y datos basicos
- descargar etiqueta y factura cuando proceda

---

## 5. Manual de intranet

### 5.1 Comportamiento general

Al entrar en la intranet veras opciones distintas segun el rol. No todos los usuarios internos tienen acceso a las mismas pantallas.

Elementos comunes habituales:

- barra superior con usuario
- estado de conexion de SignalR
- panel de notificaciones
- boton de cierre de sesion

### 5.2 Operario de oficina

El `OperarioOficina` trabaja en oficina fisica. Sus tareas mas habituales son:

- alta presencial de envios
- escaneo de paquetes en oficina
- seguimiento interno basico

### Alta presencial en oficina

Sirve para registrar un envio cuando el cliente acude a ventanilla.

Pasos:

1. Abrir `Alta presencial`.
2. Introducir los datos del paquete.
3. Introducir remitente.
4. Introducir destinatario.
5. Elegir si la entrega final es a domicilio o en oficina.
6. Elegir metodo de cobro.
7. Confirmar el alta.

Resultado esperado:

- Se genera numero de seguimiento.
- Se genera numero de expedicion.
- Se calcula el coste.
- Queda identificado el CTA destino cuando aplica.

### Escaneo de oficina

El operario puede escanear paquetes para registrar eventos de oficina, como recepciones o entregas en oficina destino, segun el flujo operativo habilitado.

### Seguimiento interno

Permite consultar expediciones concretas y revisar su estado operativo sin salir de la intranet.

### 5.3 Operario CTA

El `OperarioCTA` trabaja sobre la cola de tareas de su centro.

Sus acciones principales son:

- consultar asignaciones pendientes
- iniciar tareas
- completar tareas
- usar el escaneo integrado
- revisar seguimientos internos

### Asignaciones y escaneo

La pantalla de asignaciones es el centro del trabajo diario.

Funciones habituales:

- filtrar por estado
- escanear con camara
- introducir codigo manualmente
- procesar el siguiente paso del paquete
- actualizar la lista
- limpiar completadas de la vista

Importante:

- Si aparece un paquete que no corresponde a tus tareas, la intranet puede registrar una incidencia de tipo `PaqueteFueraDeTareas`.

### 5.4 Supervisor

El `Supervisor` tiene una vista mas amplia del CTA.

Puede:

- consultar el dashboard de su CTA
- revisar metricas del centro
- acceder a asignaciones
- reasignar o cancelar tareas
- gestionar equipo de operarios
- revisar incidencias y actividad del centro

### Gestion de equipo

Desde esta pantalla puede:

- ver operarios asignados
- consultar su estado
- revisar tareas pendientes y completadas
- desactivar operarios cuando proceda

### 5.5 Administrador

El `Admin` dispone del backoffice mas amplio de todo el sistema.

Modulos principales:

- gestion de usuarios
- gestion de clientes
- gestion de envios
- gestion de repartidores
- gestion de oficinas
- gestion de CTAs
- gestion de tarifas
- incidencias globales
- movimientos globales
- notificaciones broadcast

### Gestion de usuarios

Permite:

- crear empleados
- cambiar roles
- bloquear y desbloquear cuentas
- restablecer contrasenas
- consultar detalle por usuario

### Gestion de clientes

Permite:

- buscar clientes
- bloquear y desbloquear acceso
- consultar perfil 360
- revisar agenda y envios
- resetear contrasena

### Gestion de envios

Permite:

- filtrar por estado, fecha, CP o pago
- abrir detalle de un envio
- cambiar estado publico o interno
- anular un envio
- reabrir un envio cuando proceda

### Gestion de oficinas

Permite:

- crear oficinas nuevas
- editar oficinas existentes
- activar o desactivar oficinas
- consultar operarios activos por oficina

### Gestion de CTAs

Permite:

- crear y editar CTAs
- activar o desactivar nodos
- definir area zonal
- marcar si un nodo es aereo o maritimo

### Gestion de tarifas

Permite:

- revisar bandas de precio
- modificar precios base
- guardar cambios en bloque
- restaurar valores por defecto

### Incidencias y movimientos globales

Permiten supervision transversal sobre:

- incidencias abiertas, en revision, resueltas o cerradas
- movimientos entre CTAs
- urgencias
- actividad global de la red

### Broadcast de notificaciones

Permite enviar mensajes en tiempo real a:

- todos los conectados
- administradores
- un CTA concreto
- un rol concreto dentro de un CTA

---

## 6. Manual de la app de reparto

### 6.1 Repartidor

El `Repartidor` usa la app para ejecutar la ruta del dia.

### Dashboard del repartidor

Desde el panel principal puede entrar en:

- `Ruta activa y paradas`
- `Escanear paquetes`

### Ruta activa

Si hay ruta asignada, la pantalla mostrara:

- codigo de ruta
- estado de la ruta
- progreso general
- lista secuenciada de entregas
- mapa operativo
- siguiente parada recomendada

### Iniciar la ruta

Pasos:

1. Abrir la ruta.
2. Verificar que el estado es `Planificada`.
3. Pulsar `Iniciar ruta`.

Efecto esperado:

- la ruta pasa a `EnCurso`
- se activa el seguimiento GPS
- se habilita el trabajo normal de entregas

### Confirmar una entrega

Pasos recomendados:

1. Seleccionar la parada.
2. Elegir el resultado.
3. Introducir datos del receptor si procede.
4. Anadir observaciones, foto o firma si es necesario.
5. Confirmar la entrega.

Resultados posibles habituales:

- entregado en domicilio
- entregado en oficina
- entregado a autorizado
- primer intento fallido
- segundo intento fallido
- incidencia por direccion incorrecta
- incidencia por rechazo
- otra incidencia

### Mapa y navegacion

La ruta incorpora un mapa con:

- posicion del repartidor
- paradas
- historial reciente de ubicaciones

Ademas, permite abrir navegacion externa en:

- Google Maps
- Waze

### Trabajo offline

Si se pierde la conexion:

- la app sigue guardando ubicaciones y confirmaciones criticas
- los eventos pendientes quedan en cola
- al recuperar internet, la app intenta sincronizarlos

Recomendacion:

- Si ves un indicador de pendientes en cola, intenta sincronizar antes de cerrar la jornada.

### Escaneo manual

La pantalla de escaneo permite:

- leer codigos con la camara
- consultar expediciones concretas
- registrar acciones rapidas sobre el paquete

Es util cuando necesitas actuar sobre un paquete puntual sin navegar toda la ruta.

### 6.2 Jefe de reparto

El `JefeReparto` no trabaja sobre la ruta como si fuera un repartidor mas. Su experiencia esta orientada a coordinacion.

### Dashboard del jefe

Muestra metricas del dia como:

- repartidores activos
- rutas en curso
- rutas del dia
- entregas pendientes
- entregas completadas
- entregas fallidas

Tambien ofrece accesos rapidos a sus herramientas.

### Bandeja de paquetes

La bandeja muestra paquetes que ya han sido liberados por logistica y estan listos para reparto.

Desde aqui puede:

- seleccionar uno o varios paquetes
- crear una ruta nueva para ellos
- anadirlos a una ruta planificada

Pasos tipicos:

1. Abrir `Bandeja de paquetes`.
2. Seleccionar los paquetes deseados.
3. Elegir `Crear ruta` o `Anadir a ruta`.
4. Confirmar el repartidor o la ruta destino.

### Gestion de rutas

Permite consultar las rutas del dia y revisar:

- repartidor asignado
- numero de entregas
- progreso
- estado general

### Asignar paradas

Sirve para redistribuir entregas entre rutas cuando cambia la carga de trabajo.

Pasos:

1. Abrir `Asignar paradas`.
2. Localizar la entrega pendiente.
3. Elegir una nueva ruta compatible.
4. Confirmar la reasignacion.

### Mapa en tiempo real

Permite ver ubicaciones activas del equipo para supervisar la calle sin llamar a cada repartidor.

Utilidad practica:

- detectar actividad
- comprobar ultima actualizacion GPS
- revisar rutas en curso

### Mis repartidores

Desde esta pantalla puede:

- buscar repartidores
- ver activos e inactivos
- editar telefono o vehiculo
- desactivar o reactivar usuarios de su oficina

---

## 7. Buenas practicas de uso

### 7.1 Para clientes

- Revisa bien el codigo postal antes de pagar.
- Usa la agenda de direcciones para evitar errores repetidos.
- Guarda el numero de seguimiento despues de contratar.

### 7.2 Para operativa interna

- Usa siempre el modo de escaneo correcto.
- No cambies estados internos fuera del flujo si no esta justificado.
- Reporta `PaqueteFueraDeTareas` cuando el paquete no corresponda a la cola esperada.

### 7.3 Para reparto

- Inicia la ruta antes de comenzar entregas.
- Manten el GPS activo cuando la app lo solicite.
- Anade observaciones cuando una incidencia no quede clara con el solo estado.
- Sincroniza la cola offline antes de cerrar la jornada si ha habido problemas de red.

---

## 8. Incidencias frecuentes para el usuario

### 8.1 No puedo iniciar sesion

Revisar:

- email y contrasena
- aplicacion correcta
- cuenta bloqueada por administracion

### 8.2 No encuentro una oficina

Revisar:

- codigo postal correcto
- ciudad escrita sin errores
- si estas buscando la oficina de origen o la de destino

### 8.3 El pago se ha cancelado

No significa necesariamente que el envio se haya perdido. Revisa la pantalla de cancelacion o tu area de envios y reintenta el pago si el sistema lo permite.

### 8.4 El tracking no cambia al instante

Puede ocurrir si:

- el evento aun no se ha consolidado
- no hay conexion en tiempo real
- el envio aun no ha pasado al siguiente hito operativo

### 8.5 El repartidor pierde cobertura

La app esta preparada para guardar eventos pendientes. Cuando vuelva la conexion, debe sincronizarlos.

---

## 9. Recomendacion para la entrega final del TFG

Para la version final entregada al tribunal, se recomienda acompanar este manual con capturas de pantalla numeradas y tituladas. Las imagenes mas utiles para documentar el uso real de la plataforma son:

- registro y login del cliente
- calculadora de tarifas
- wizard de nuevo envio
- tracking publico
- panel de usuario
- dashboard de intranet por rol
- alta presencial en oficina
- asignaciones y escaneo
- panel administrativo
- dashboard del jefe de reparto
- bandeja de paquetes
- ruta activa del repartidor con mapa

---

## 10. Cierre

NexoPostal ha dejado de ser una aplicacion unica con menus distintos para convertirse en un ecosistema de uso por perfiles. Este manual pretende que cada usuario, interno o externo, identifique con claridad donde debe acceder, que operaciones puede realizar y como resolver sus tareas habituales sin necesidad de conocer la arquitectura tecnica subyacente.