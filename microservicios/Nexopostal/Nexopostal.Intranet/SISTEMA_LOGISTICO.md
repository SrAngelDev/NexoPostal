# Sistema Logístico NexoPostal - Módulo Intranet

## Índice

1. [Visión General](#visión-general)
2. [Arquitectura de la Red Logística](#arquitectura-de-la-red-logística)
3. [Áreas Zonales](#áreas-zonales)
4. [Centros de Tratamiento Automatizado (CTAs)](#centros-de-tratamiento-automatizado-ctas)
5. [Tabla de Enrutamiento por Código Postal](#tabla-de-enrutamiento-por-código-postal)
6. [Roles y Responsabilidades](#roles-y-responsabilidades)
7. [Flujo Completo del Paquete](#flujo-completo-del-paquete)
8. [Diferencias entre Envío Normal y Urgente](#diferencias-entre-envío-normal-y-urgente)
9. [Modelo de Datos](#modelo-de-datos)
10. [API Endpoints](#api-endpoints)
11. [Sistema de Notificaciones en Tiempo Real (SignalR)](#sistema-de-notificaciones-en-tiempo-real-signalr)
12. [Data Seeder](#data-seeder)
13. [Configuración y Despliegue](#configuración-y-despliegue)

---

## Visión General

El módulo **Nexopostal.Intranet** es el microservicio de back-office que gestiona toda la operativa logística interna de NexoPostal. Implementa el sistema de clasificación y transporte de paquetes, replicando el modelo real de Correos de España.

**Responsabilidades del módulo:**
- Gestión de los 17 CTAs (Centros de Tratamiento Automatizado) distribuidos por España
- Asignación de operarios a CTAs y gestión de sus roles
- Clasificación y enrutamiento automático de paquetes por código postal
- Movimientos troncales entre CTAs (terrestre, aéreo, marítimo)
- Gestión de incidencias por el Operario Jefe
- **Notificaciones en tiempo real vía SignalR** a los operarios de cada CTA
- **Admisión automática** de paquetes con resolución de CTA por código postal

**Stack tecnológico:**
- .NET 10 / ASP.NET Web API
- Entity Framework Core + PostgreSQL
- JWT Bearer Authentication (misma clave que Auth y Gateway)
- Swagger/OpenAPI para documentación

---

## Arquitectura de la Red Logística

```
                    ┌─────────────────────────────────────────────┐
                    │           RED LOGÍSTICA NEXOPOSTAL          │
                    └─────────────────────────────────────────────┘

    ┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
    │ NOROESTE │     │  NORTE   │     │ NORESTE  │     │  CENTRO  │
    │ CTA-COR  │────▶│ CTA-BIL  │────▶│ CTA-BCN  │────▶│ CTA-MAD  │
    │ CTA-GIJ  │     │ CTA-PNA  │     │ CTA-ZGZ  │     │ CTA-VLL  │
    └──────┬───┘     └────┬─────┘     └──────────┘     └────┬─────┘
           │              │                                  │
           │         ┌────▼─────┐                      ┌────▼─────┐
           │         │   ESTE   │                      │   SUR    │
           └────────▶│ CTA-VLC  │◀─────────────────────│ CTA-SEV  │
                     │ CTA-ALI  │                      │ CTA-MAL  │
                     └──────────┘                      │ CTA-BAD  │
                                                       │ CTA-CEU  │
                     ┌──────────┐                      └──────────┘
                     │ INSULAR  │
                     │ CTA-PMI  │ ✈️🚢
                     │ CTA-LPA  │ ✈️🚢
                     │ CTA-TFE  │ ✈️🚢
                     └──────────┘
```

---

## Áreas Zonales

NexoPostal divide España en **7 Áreas Zonales** para optimizar las rutas de transporte:

| Área | Comunidades Autónomas | CTAs |
|------|----------------------|------|
| **Noroeste** | Galicia, Asturias, León, Zamora | CTA-COR, CTA-GIJ |
| **Norte** | País Vasco, Cantabria, Navarra, La Rioja, norte CyL | CTA-BIL, CTA-PNA |
| **Noreste** | Cataluña, Aragón | CTA-BCN, CTA-ZGZ |
| **Centro** | Madrid, Castilla-La Mancha, centro CyL | CTA-MAD, CTA-VLL |
| **Este** | Comunidad Valenciana, Murcia | CTA-VLC, CTA-ALI |
| **Sur** | Andalucía, Extremadura, Ceuta, Melilla | CTA-SEV, CTA-MAL, CTA-BAD, CTA-CEU |
| **Insular** | Canarias, Baleares | CTA-PMI, CTA-LPA, CTA-TFE |

---

## Centros de Tratamiento Automatizado (CTAs)

Los CTAs son los nodos principales de la red logística. Cada uno cuenta con:
- **Cintas transportadoras** de alta velocidad
- **Arcos de lectura** que escanean códigos de barras en milisegundos
- **Rampas de clasificación** que desvían paquetes según código postal de destino
- **Contenedores rodantes** agrupados por Área Zonal de destino

### Lista de los 17 CTAs

| Código | Nombre | Área | Ciudad | Nodo Aéreo | Nodo Marítimo |
|--------|--------|------|--------|:----------:|:-------------:|
| CTA-COR | CTA A Coruña | Noroeste | A Coruña | ✅ | ❌ |
| CTA-GIJ | CTA Gijón | Noroeste | Gijón | ❌ | ❌ |
| CTA-BIL | CTA Bilbao | Norte | Bilbao | ✅ | ❌ |
| CTA-PNA | CTA Pamplona | Norte | Pamplona | ❌ | ❌ |
| CTA-BCN | CTA Barcelona - El Prat | Noreste | El Prat | ✅ | ❌ |
| CTA-ZGZ | CTA Zaragoza | Noreste | Zaragoza | ✅ | ❌ |
| CTA-MAD | CTA Madrid - Barajas | Centro | Madrid | ✅ | ❌ |
| CTA-VLL | CTA Valladolid | Centro | Valladolid | ❌ | ❌ |
| CTA-VLC | CTA Valencia | Este | Valencia | ✅ | ❌ |
| CTA-ALI | CTA Alicante | Este | Alicante | ✅ | ❌ |
| CTA-SEV | CTA Sevilla | Sur | Sevilla | ✅ | ❌ |
| CTA-MAL | CTA Málaga | Sur | Málaga | ✅ | ❌ |
| CTA-BAD | CTA Badajoz | Sur | Badajoz | ❌ | ❌ |
| CTA-CEU | CTA Ceuta-Melilla | Sur | Málaga (hub) | ❌ | ✅ |
| CTA-PMI | CTA Palma de Mallorca | Insular | Palma | ✅ | ✅ |
| CTA-LPA | CTA Las Palmas | Insular | Las Palmas | ✅ | ✅ |
| CTA-TFE | CTA Santa Cruz de Tenerife | Insular | S/C Tenerife | ✅ | ✅ |

---

## Tabla de Enrutamiento por Código Postal

Los **2 primeros dígitos** del código postal español determinan la provincia y el CTA que gestiona los envíos a esa zona:

### Área Noroeste

| Prefijo CP | Provincia | CTA Asignado |
|:----------:|-----------|:------------:|
| 15 | A Coruña | CTA-COR |
| 27 | Lugo | CTA-COR |
| 32 | Ourense | CTA-COR |
| 36 | Pontevedra | CTA-COR |
| 33 | Asturias | CTA-GIJ |
| 24 | León | CTA-GIJ |
| 49 | Zamora | CTA-GIJ |

### Área Norte

| Prefijo CP | Provincia | CTA Asignado |
|:----------:|-----------|:------------:|
| 01 | Álava | CTA-BIL |
| 20 | Guipúzcoa | CTA-BIL |
| 48 | Vizcaya | CTA-BIL |
| 39 | Cantabria | CTA-BIL |
| 31 | Navarra | CTA-PNA |
| 26 | La Rioja | CTA-PNA |
| 09 | Burgos | CTA-PNA |
| 34 | Palencia | CTA-PNA |
| 42 | Soria | CTA-PNA |

### Área Noreste

| Prefijo CP | Provincia | CTA Asignado |
|:----------:|-----------|:------------:|
| 08 | Barcelona | CTA-BCN |
| 17 | Girona | CTA-BCN |
| 25 | Lleida | CTA-BCN |
| 43 | Tarragona | CTA-BCN |
| 50 | Zaragoza | CTA-ZGZ |
| 22 | Huesca | CTA-ZGZ |
| 44 | Teruel | CTA-ZGZ |

### Área Centro

| Prefijo CP | Provincia | CTA Asignado |
|:----------:|-----------|:------------:|
| 28 | Madrid | CTA-MAD |
| 19 | Guadalajara | CTA-MAD |
| 45 | Toledo | CTA-MAD |
| 16 | Cuenca | CTA-MAD |
| 13 | Ciudad Real | CTA-MAD |
| 02 | Albacete | CTA-MAD |
| 47 | Valladolid | CTA-VLL |
| 37 | Salamanca | CTA-VLL |
| 05 | Ávila | CTA-VLL |
| 40 | Segovia | CTA-VLL |

### Área Este

| Prefijo CP | Provincia | CTA Asignado |
|:----------:|-----------|:------------:|
| 46 | Valencia | CTA-VLC |
| 12 | Castellón | CTA-VLC |
| 03 | Alicante | CTA-ALI |
| 30 | Murcia | CTA-ALI |

### Área Sur

| Prefijo CP | Provincia | CTA Asignado |
|:----------:|-----------|:------------:|
| 41 | Sevilla | CTA-SEV |
| 21 | Huelva | CTA-SEV |
| 11 | Cádiz | CTA-SEV |
| 14 | Córdoba | CTA-SEV |
| 29 | Málaga | CTA-MAL |
| 18 | Granada | CTA-MAL |
| 04 | Almería | CTA-MAL |
| 23 | Jaén | CTA-MAL |
| 06 | Badajoz | CTA-BAD |
| 10 | Cáceres | CTA-BAD |
| 51 | Ceuta | CTA-CEU |
| 52 | Melilla | CTA-CEU |

### Área Insular

| Prefijo CP | Provincia | CTA Asignado |
|:----------:|-----------|:------------:|
| 07 | Islas Baleares | CTA-PMI |
| 35 | Las Palmas | CTA-LPA |
| 38 | S/C de Tenerife | CTA-TFE |

---

## Roles y Responsabilidades

El sistema de la intranet define tres roles operativos, cada uno con responsabilidades claramente diferenciadas:

### Operario (`Operario`)

| Aspecto | Descripción |
|---------|-------------|
| **Función principal** | Ejecutar tareas físicas en el CTA |
| **Tareas** | Recepción, clasificación, carga/descarga de transporte, expedición |
| **Flujo de trabajo** | Ver tareas pendientes → Iniciar → Completar |
| **Prioridad** | Los paquetes urgentes aparecen siempre primero ("pase VIP") |

**Endpoints accesibles:**
- `GET /api/asignaciones/mis-pendientes` — Ver mis tareas pendientes
- `GET /api/asignaciones/mis-en-progreso` — Ver mis tareas en curso
- `PUT /api/asignaciones/{id}/iniciar` — Iniciar una tarea
- `PUT /api/asignaciones/{id}/completar` — Completar una tarea
- `GET /api/operarios/mi-cta` — Ver mi CTA asignado

### Operario Logístico (`OperarioLogistico`)

| Aspecto | Descripción |
|---------|-------------|
| **Función principal** | Gestionar el flujo de paquetes en el CTA |
| **Tareas** | Escanear paquetes entrantes, asignar tareas a operarios, gestionar movimientos troncales |
| **Herramienta clave** | Resolución de CTA destino por código postal |

**Endpoints accesibles:**
- `POST /api/asignaciones` — Crear asignación de tarea a un operario
- `PUT /api/asignaciones/{id}/cancelar` — Cancelar una tarea
- `GET /api/asignaciones/cta/{ctaId}` — Ver todas las asignaciones del CTA
- `POST /api/movimientos` — Crear movimiento entre CTAs
- `PUT /api/movimientos/{id}/despachar` — Despachar un movimiento
- `PUT /api/movimientos/{id}/recibir` — Registrar recepción
- `GET /api/ctas/resolver/{codigoPostal}` — Resolver CTA destino
- `GET /api/ctas/{id}/dashboard` — Ver estadísticas del CTA

### Operario Jefe (`OperarioJefe`)

| Aspecto | Descripción |
|---------|-------------|
| **Función principal** | Gestionar **exclusivamente** las incidencias del CTA |
| **Tareas** | Detectar problemas, investigar causas, aplicar resoluciones |
| **Tipos de incidencia** | Paquete dañado, extraviado, dirección incorrecta, retenido, error de clasificación |

**Endpoints accesibles:**
- `POST /api/incidencias` — Reportar nueva incidencia
- `PUT /api/incidencias/{id}` — Actualizar estado de incidencia
- `GET /api/incidencias/cta/{ctaId}` — Ver incidencias del CTA
- `GET /api/incidencias/paquete/{numExpedicion}` — Ver incidencias de un paquete
- `GET /api/ctas/{id}/dashboard` — Ver estadísticas del CTA
- `POST /api/operarios` — Crear nuevos operarios
- `DELETE /api/operarios/{id}` — Desactivar operarios

---

## Flujo Completo del Paquete

### 1. Admisión Comercial (Módulo Ciudadano)

El cliente crea el envío en la web/oficina → se genera el **NumeroSeguimiento** (público: `NX123456789ES`) y el **NumeroExpedicion** (interno: `NXI-7A3F2K9B`).

### 2. Primera Milla — Recogida y Concentración

```
   Oficinas/Buzones    →    Furgón de recogida    →    CTA de Origen
   (fin del día)                                        (según CP origen)
```

### 3. Clasificación en el CTA de Origen

```
┌─────────────────────────────────────────────────────────────────────┐
│                     CTA DE ORIGEN                                    │
│                                                                      │
│  1. Paquete llega → Tarea: RECEPCIÓN (Operario)                     │
│  2. Cinta transportadora → Arco de lectura escanea código de barras │
│  3. Sistema lee CP destino → Resuelve CTA destino                    │
│     Ejemplo: CP "41001" → prefijo "41" → Sevilla → CTA-SEV          │
│  4. Paquete en rampa → Tarea: CLASIFICACIÓN (Operario)               │
│  5. Agrupación en contenedores por Área Zonal de destino             │
│  6. Carga al transporte → Tarea: CARGA TRANSPORTE (Operario)        │
│  7. Movimiento creado: CTA-MAD → CTA-SEV (Terrestre/Nocturno)       │
│                                                                      │
│  [OperarioLogístico gestiona todo este flujo]                        │
│  [OperarioJefe interviene SOLO si hay incidencia]                    │
└─────────────────────────────────────────────────────────────────────┘
```

### 4. Rutas Troncales — Transporte de Larga Distancia

| Tipo | Uso | Ejemplo |
|------|-----|---------|
| **Terrestre** | Camiones nocturnos entre CTAs peninsulares | Madrid → Sevilla |
| **Aéreo** | Paquetes urgentes larga distancia + destinos insulares | Madrid → Palma |
| **Marítimo** | Paquetes normales a islas y Ceuta/Melilla | Málaga → Las Palmas |

**Reglas de enrutamiento automático:**
1. Si destino es **Insular** → Urgente: Aéreo / Normal: Marítimo
2. Si destino es **Ceuta/Melilla** → Urgente: Aéreo / Normal: Marítimo
3. Si es **urgente** y áreas diferentes → Aéreo (si ambos CTAs tienen nodo aéreo)
4. Por defecto → **Terrestre** (camiones nocturnos)

### 5. Clasificación en el CTA de Destino

```
┌──────────────────────────────────────────────────────────┐
│                   CTA DE DESTINO                          │
│                                                           │
│  1. Camión llega de madrugada                             │
│  2. Tarea: DESCARGA TRANSPORTE (Operario)                 │
│  3. Clasificación fina por Unidad de Reparto              │
│  4. Tarea: CLASIFICACIÓN DESTINO (Operario)               │
│  5. Asignación a ruta de reparto                          │
│  6. Tarea: EXPEDICIÓN (Operario) → sale hacia la UR      │
└──────────────────────────────────────────────────────────┘
```

### 6. Última Milla — Reparto (Módulo Driver)

El paquete sale del CTA y se entrega al destinatario. Esta fase la gestiona el módulo **Driver-App** con los repartidores.

---

## Diferencias entre Envío Normal y Urgente

| Aspecto | Normal (Estándar) | Urgente (Premium) |
|---------|:-----------------:|:-----------------:|
| **Prioridad en CTA** | Cola FIFO (lo primero que entra, primero sale) | **Pase VIP** — se procesa inmediatamente |
| **Transporte** | Solo terrestre, espera a llenar camión | Espacio asegurado en primer transporte |
| **Transporte insular** | Marítimo (barco) | **Aéreo** (avión) |
| **Larga distancia** | Terrestre siempre | **Aéreo** si hay nodo disponible |
| **Tiempo de entrega** | 3-5 días laborables (estimado) | **24-48h** (garantizado) |
| **Reparto** | Cartero tradicional (UR) | Repartidor exclusivo en furgoneta (USE) |
| **Intentos de entrega** | 1 intento | 2 intentos |
| **Tracking** | Básico ("Admitido", "En tránsito", "Entregado") | Escaneos frecuentes en tiempo real |
| **Saturación** | Se retrasa en picos (Black Friday) | Prioridad incluso en saturación |

---

## Modelo de Datos

### Diagrama Entidad-Relación

```
┌─────────────────────┐       ┌──────────────────┐
│  CentroTratamiento  │───1:N─│     RutaCta       │
│  (CTA)              │       │  PrefijoCp → CTA  │
│                     │       └──────────────────┘
│  - Codigo           │
│  - Nombre           │       ┌──────────────────┐
│  - Area (enum)      │───1:N─│   OperarioCta     │
│  - EsNodoAereo      │       │  - IdentityUserId │
│  - EsNodoMaritimo   │       │  - Rol (enum)     │
└───┬─────┬───────────┘       │  - CodigoEmpleado │
    │     │                   └──┬──────────┬─────┘
    │     │                      │          │
    │     │    ┌─────────────────▼──┐  ┌────▼──────────┐
    │     │    │ AsignacionPaquete  │  │  Incidencia    │
    │     └──N─│ - NumExpedicion    │  │ - NumExpedicion│
    │          │ - TipoTarea       │  │ - Tipo (enum)  │
    │          │ - EstadoTarea     │  │ - Estado       │
    │          │ - EsUrgente       │  │ - Descripcion  │
    │          └────────────────────┘  │ - Resolucion  │
    │                                  └───────────────┘
    │
    │     ┌──────────────────────┐
    └──N──│  MovimientoPaquete   │
          │  - NumExpedicion     │
          │  - CtaOrigen → CTA  │
          │  - CtaDestino → CTA │
          │  - TipoTransporte   │
          │  - Estado           │
          │  - EsUrgente        │
          └──────────────────────┘
```

### Enumeraciones

| Enum | Valores |
|------|---------|
| **AreaZonal** | Noroeste, Norte, Noreste, Centro, Este, Sur, Insular |
| **RolOperario** | Operario, OperarioLogistico, OperarioJefe |
| **TipoTarea** | Recepcion, Clasificacion, CargaTransporte, DescargaTransporte, Expedicion |
| **EstadoTarea** | Pendiente, EnProgreso, Completada, Cancelada |
| **EstadoMovimiento** | Programado, EnTransito, Recibido, Cancelado |
| **TipoTransporte** | Terrestre, Aereo, Maritimo |
| **TipoIncidencia** | PaqueteDanado, PaqueteExtraviado, DireccionIncorrecta, PaqueteRetenido, ErrorClasificacion, Otra |
| **EstadoIncidencia** | Abierta, EnRevision, Resuelta, Cerrada |

---

## API Endpoints

### CTAs (`/api/ctas`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| GET | `/api/ctas` | Todos | Listar todos los CTAs |
| GET | `/api/ctas/{id}` | Todos | Detalle de un CTA con operarios y rutas |
| GET | `/api/ctas/resolver/{cp}` | Todos | Resolver CTA destino por código postal |
| GET | `/api/ctas/{id}/dashboard` | Admin, Jefe, Logístico | Dashboard de estadísticas |

### Operarios (`/api/operarios`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| GET | `/api/operarios/mi-cta` | Todos | Info del CTA del operario autenticado |
| GET | `/api/operarios/cta/{ctaId}` | Todos | Listar operarios de un CTA |
| GET | `/api/operarios/{id}` | Todos | Detalle de un operario |
| POST | `/api/operarios` | Admin, Jefe | Crear operario |
| DELETE | `/api/operarios/{id}` | Admin, Jefe | Desactivar operario |

### Asignaciones (`/api/asignaciones`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| POST | `/api/asignaciones` | Admin, Logístico | Crear asignación |
| GET | `/api/asignaciones/mis-pendientes` | Todos | Tareas pendientes propias |
| GET | `/api/asignaciones/mis-en-progreso` | Todos | Tareas en progreso propias |
| GET | `/api/asignaciones/cta/{ctaId}` | Admin, Jefe, Logístico | Todas las asignaciones del CTA |
| GET | `/api/asignaciones/{id}` | Todos | Detalle de asignación |
| PUT | `/api/asignaciones/{id}/iniciar` | Admin, Operario | Iniciar tarea |
| PUT | `/api/asignaciones/{id}/completar` | Admin, Operario | Completar tarea |
| PUT | `/api/asignaciones/{id}/cancelar` | Admin, Logístico | Cancelar tarea |

### Movimientos (`/api/movimientos`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| POST | `/api/movimientos` | Admin, Jefe, Logístico | Crear movimiento entre CTAs |
| GET | `/api/movimientos/cta/{ctaId}` | Admin, Jefe, Logístico | Movimientos de un CTA |
| GET | `/api/movimientos/{id}` | Admin, Jefe, Logístico | Detalle de movimiento |
| GET | `/api/movimientos/paquete/{numExp}` | Admin, Jefe, Logístico | Historial de un paquete |
| PUT | `/api/movimientos/{id}/despachar` | Admin, Jefe, Logístico | Despachar (→ EnTransito) |
| PUT | `/api/movimientos/{id}/recibir` | Admin, Jefe, Logístico | Recibir (→ Recibido) |
| PUT | `/api/movimientos/{id}/cancelar` | Admin, Jefe, Logístico | Cancelar movimiento |

### Incidencias (`/api/incidencias`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| POST | `/api/incidencias` | Admin, Jefe | Reportar incidencia |
| GET | `/api/incidencias/cta/{ctaId}` | Admin, Jefe | Incidencias del CTA |
| GET | `/api/incidencias/{id}` | Admin, Jefe | Detalle de incidencia |
| GET | `/api/incidencias/paquete/{numExp}` | Admin, Jefe | Incidencias de un paquete |
| PUT | `/api/incidencias/{id}` | Admin, Jefe | Actualizar incidencia |

### Admisión de Paquetes (`/api/admision`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| POST | `/api/admision/paquete` | Admin, Jefe, Logístico | Admitir paquete → resuelve CTA por CP + notifica vía SignalR |

**Ejemplo de request:**
```json
{
  "numeroExpedicion": "NXI-7A3F2K9B",
  "codigoPostalDestino": "28919",
  "codigoPostalOrigen": "08001",
  "esUrgente": true,
  "remitente": "Juan García",
  "destinatario": "María López"
}
```

**Flujo automático:**
1. CP destino `28919` → prefijo `28` → Madrid → **CTA-MAD** (Centro)
2. CP origen `08001` → prefijo `08` → Barcelona → **CTA-BCN** (Noreste)
3. CTAs diferentes → crea **movimiento troncal** CTA-BCN → CTA-MAD
4. Urgente + larga distancia → transporte **Aéreo**
5. Notifica vía SignalR a OperarioLogísticos de CTA-MAD

---

## Sistema de Notificaciones en Tiempo Real (SignalR)

### Arquitectura

El módulo utiliza **ASP.NET Core SignalR** para enviar notificaciones push en tiempo real a los operarios conectados a la intranet. Los operarios se agrupan automáticamente por CTA y rol al conectarse.

```
┌──────────────────────────────────────────────────────────────┐
│                    SERVIDOR (Hub SignalR)                      │
│                   /hubs/intranet                               │
│                                                                │
│   Al conectarse, cada operario se une a sus grupos:            │
│                                                                │
│   ┌─────────────────────────────────────────────────────────┐  │
│   │ Grupos por CTA:                                         │  │
│   │   cta-{id}           → Todos los operarios del CTA      │  │
│   │   cta-{id}-logistico → Solo OperarioLogisticos          │  │
│   │   cta-{id}-jefe      → Solo OperarioJefes               │  │
│   │   cta-{id}-operarios → Solo Operarios base              │  │
│   │   operario-{id}      → Operario individual              │  │
│   └─────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘

   Ejemplo: Operario Logístico del CTA-MAD se une a:
     ✅ cta-7            (grupo general de CTA-MAD)
     ✅ cta-7-logistico  (solo logísticos de CTA-MAD)
     ✅ operario-15      (su grupo personal)
```

### Conexión desde el Frontend (Angular)

```typescript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://api.nexopostal.es/hubs/intranet', {
    accessTokenFactory: () => localStorage.getItem('token') ?? ''
  })
  .withAutomaticReconnect()
  .build();

// Escuchar evento de paquete recibido
connection.on('PaqueteRecibidoEnCta', (notificacion) => {
  console.log('Paquete pendiente:', notificacion);
  // Mostrar toast/alerta al operario logístico
});

// Escuchar evento de tarea asignada
connection.on('TareaAsignada', (notificacion) => {
  console.log('Nueva tarea:', notificacion);
  // Mostrar notificación al operario
});

// Confirmar conexión
connection.on('ConexionEstablecida', (info) => {
  console.log(`Conectado a ${info.ctaCodigo} como ${info.rol}`);
});

await connection.start();
```

### Eventos SignalR

| Evento | Destino | Trigger | Descripción |
|--------|---------|---------|-------------|
| `ConexionEstablecida` | Caller | Al conectarse | Confirma conexión con info del operario y CTA |
| `PaqueteRecibidoEnCta` | `cta-{id}-logistico` | POST `/api/admision/paquete` | Paquete llegó al CTA, pendiente de asignar |
| `TareaAsignada` | `operario-{id}` | POST `/api/asignaciones` | Tarea asignada al operario específico |
| `TareaIniciada` | `cta-{id}-logistico` | PUT `/api/asignaciones/{id}/iniciar` | Operario empezó la tarea |
| `TareaCompletada` | `cta-{id}-logistico` | PUT `/api/asignaciones/{id}/completar` | Operario terminó la tarea |
| `TareaCancelada` | `cta-{id}` | PUT `/api/asignaciones/{id}/cancelar` | Tarea cancelada (notifica a todos) |
| `MovimientoDespachado` | `cta-{destino}-logistico` | PUT `/api/movimientos/{id}/despachar` | Paquete salió de otro CTA hacia este |
| `MovimientoRecibido` | `cta-{destino}-logistico` | PUT `/api/movimientos/{id}/recibir` | Paquete llegó desde otro CTA |
| `IncidenciaCreada` | `cta-{id}` | POST `/api/incidencias` | Nueva incidencia (todo el CTA) |
| `IncidenciaActualizada` | `cta-{id}` | PUT `/api/incidencias/{id}` | Cambio de estado de incidencia |
| `NotificacionGeneral` | `cta-{id}` | Manual | Mensaje general para el CTA |

### Formato de Notificación

Todas las notificaciones siguen la estructura `NotificacionDto`:

```json
{
  "tipo": "PaqueteRecibidoEnCta",
  "titulo": "🔴 URGENTE · Paquete pendiente de asignar",
  "mensaje": "El paquete NXI-7A3F2K9B con destino a Madrid ha llegado al CTA-MAD y está pendiente de clasificación.",
  "ctaId": 7,
  "ctaCodigo": "CTA-MAD",
  "numeroExpedicion": "NXI-7A3F2K9B",
  "esUrgente": true,
  "fechaHora": "2026-02-24T14:30:00Z",
  "datos": {
    "provincia": "Madrid",
    "accionRequerida": "Asignar a un operario para clasificación"
  }
}
```

### Flujo de Ejemplo Completo con Notificaciones

```
1. POST /api/admision/paquete  (CP destino: 28919, urgente)
   └─ Sistema resuelve: CP 28 → CTA-MAD
   └─ 📡 SignalR → "PaqueteRecibidoEnCta" → logísticos CTA-MAD
   └─ 🔔 Logístico recibe: "🔴 URGENTE · Paquete NXI-... pendiente de asignar"

2. POST /api/asignaciones  (asignar a Operario 1)
   └─ Logístico crea tarea de Clasificación para el operario
   └─ 📡 SignalR → "TareaAsignada" → operario-15 (personal)
   └─ 🔔 Operario recibe: "Nueva tarea: Clasificación 🔴 URGENTE"

3. PUT /api/asignaciones/{id}/iniciar
   └─ Operario empieza a clasificar
   └─ 📡 SignalR → "TareaIniciada" → logísticos CTA-MAD
   └─ 🔔 Logístico recibe: "Operario 1 ha iniciado Clasificación"

4. PUT /api/asignaciones/{id}/completar
   └─ Operario termina de clasificar
   └─ 📡 SignalR → "TareaCompletada" → logísticos CTA-MAD
   └─ 🔔 Logístico recibe: "Operario 1 ha completado Clasificación"
```

### Autenticación JWT en WebSocket

SignalR usa WebSocket como transporte por defecto. Como WebSocket no envía headers HTTP, el token JWT se pasa por **query string**:

```
wss://api.nexopostal.es/hubs/intranet?access_token=eyJhbGciOiJIUzI1NiIs...
```

El servidor extrae el token automáticamente en el evento `OnMessageReceived` de JWT Bearer.

---

## Data Seeder

Al iniciar la aplicación en entorno `Development`, el **IntranetDataSeeder** siembra automáticamente:

### Datos sembrados

| Entidad | Cantidad | Descripción |
|---------|:--------:|-------------|
| CTAs | 17 | Distribuidos en 7 Áreas Zonales |
| Rutas | 52 | Todos los prefijos CP de España (01-52) |
| Operarios | 68 | 4 por CTA (1 Jefe + 1 Logístico + 2 Operarios) |

### Operarios por CTA

Cada CTA se inicializa con:

| Rol | Código Ejemplo | Nombre Ejemplo | Función |
|-----|----------------|----------------|---------|
| OperarioJefe | EMP-001 | Jefe A Coruña | Gestiona incidencias |
| OperarioLogistico | EMP-002 | Logístico A Coruña | Asigna paquetes a operarios |
| Operario | EMP-003 | Operario 1 A Coruña | Ejecuta tareas físicas |
| Operario | EMP-004 | Operario 2 A Coruña | Ejecuta tareas físicas |

> **Nota:** Los `IdentityUserId` de los operarios del seeder son provisionales (`seed-*`). En producción, se vincularían con usuarios reales creados en el microservicio Auth.

---

## Configuración y Despliegue

### Estructura de archivos

```
Nexopostal.Intranet/
├── Controllers/
│   ├── AdmisionController.cs        ← NUEVO: Admisión de paquetes por CP
│   ├── AsignacionesController.cs
│   ├── CtasController.cs
│   ├── IncidenciasController.cs
│   ├── MovimientosController.cs
│   └── OperariosController.cs
├── Data/
│   ├── IntranetDbContext.cs
│   └── IntranetDataSeeder.cs
├── DTOs/
│   ├── AsignacionDtos.cs
│   ├── CtaDtos.cs
│   ├── IncidenciaDtos.cs
│   ├── MovimientoDtos.cs
│   ├── NotificacionDtos.cs          ← NUEVO: DTOs de notificaciones y admisión
│   └── OperarioDtos.cs
├── Hubs/
│   └── IntranetHub.cs               ← NUEVO: Hub SignalR
├── Migrations/
│   └── (migración EF Core InitialCreate)
├── Models/
│   ├── AsignacionPaquete.cs
│   ├── CentroTratamiento.cs
│   ├── Enums.cs
│   ├── Incidencia.cs
│   ├── MovimientoPaquete.cs
│   ├── OperarioCta.cs
│   └── RutaCta.cs
├── Services/
│   ├── AdmisionService.cs           ← NUEVO: Lógica de admisión + enrutamiento
│   ├── AsignacionService.cs         (+ notificaciones SignalR)
│   ├── ClasificacionService.cs
│   ├── IncidenciaService.cs         (+ notificaciones SignalR)
│   ├── MovimientoService.cs         (+ notificaciones SignalR)
│   ├── NotificacionService.cs       ← NUEVO: Envío de notificaciones SignalR
│   └── OperarioService.cs
├── Program.cs
├── appsettings.json
├── Nexopostal.Intranet.csproj
└── Dockerfile
```

### Base de datos

- **Servidor:** PostgreSQL
- **Base de datos:** `nexopostal_intranet`
- **Puerto desarrollo:** 5434
- **Migraciones:** Se aplican automáticamente al arrancar en Development

### Variables de configuración (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5434;Database=nexopostal_intranet;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "(misma clave que Auth y Gateway)",
    "Issuer": "NexoPostal.Auth",
    "Audience": "nexopostal-api"
  }
}
```

### Docker Compose (añadir al docker-compose.yml)

```yaml
  modulo-intranet:
    build:
      context: ./microservicios/Nexopostal
      dockerfile: Nexopostal.Intranet/Dockerfile
    container_name: modulo-intranet
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80
      - ConnectionStrings__DefaultConnection=Host=postgres-intranet;Port=5432;Database=nexopostal_intranet;Username=postgres;Password=postgres
    expose:
      - "80"
    networks:
      - nexopostal-net
    depends_on:
      postgres-intranet:
        condition: service_healthy

  postgres-intranet:
    image: postgres:16-alpine
    container_name: postgres-intranet
    environment:
      POSTGRES_DB: nexopostal_intranet
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5434:5432"
    volumes:
      - postgres-intranet-data:/var/lib/postgresql/data
    networks:
      - nexopostal-net
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d nexopostal_intranet"]
      interval: 5s
      timeout: 5s
      retries: 10
```

Y añadir al bloque `volumes:`:
```yaml
  postgres-intranet-data:
```

---

*Documentación generada para el TFG NexoPostal — Módulo Intranet (Logística)*
