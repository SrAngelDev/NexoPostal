# NexoPostal

NexoPostal es una plataforma completa de paqueteria con tres apps:

- **nexopostal.es (clientes)**: alta, login, creacion de envios, pago y seguimiento.
- **Intranet**: gestion operativa y logistica (CTAs, oficinas, incidencias, admision).
- **Driver app**: herramientas para repartidores (rutas, entregas, confirmaciones, ubicacion).

Todo esta orquestado por un API Gateway y microservicios .NET con bases de datos Postgres.

---

## Aplicaciones y funcionalidades

### NexoPostal.es (clientes)
- Registro y login de clientes.
- Cotizacion y calculo de tarifas.
- Creacion de envios y pago con Stripe Checkout.
- Seguimiento por numero de envio.
- Descarga de etiqueta y factura en PDF.
- Buscador de oficinas por codigo postal o texto.
- Perfil de cliente y gestion de direcciones.
- Notificaciones de estado en tiempo real via SignalR.

### Intranet (operaciones)
- Dashboard operativo de la red logistica.
- Gestion de CTAs y rutas de enrutamiento por prefijos de CP.
- Admision de envios y asignaciones internas.
- Registro de movimientos, incidencias y escaneos.
- Consulta de oficinas y operarios asignados.
- Notificaciones internas en tiempo real via SignalR.

### Driver app (reparto)
- Vista de ruta y entregas asignadas.
- Confirmacion de entrega y cambios de estado.
- Envio de ubicacion para seguimiento en tiempo real.

---

## Arquitectura

- **API Gateway**: enrutamiento y seguridad de rutas publicas/privadas.
- **Microservicios**:
	- **Auth**: autenticacion y usuarios (Identity + JWT).
	- **Ciudadano**: envios, tarifas, pagos, oficinas, tracking.
	- **Intranet (Logistica)**: CTAs, operativa, incidencias, oficinas.
	- **Reparto**: rutas, entregas y ubicacion.
- **Nginx**: reverse proxy y SSL.
- **Postgres**: una base por microservicio.
- **Stripe**: pagos con Checkout y webhook.
- **SignalR**: tracking y notificaciones en tiempo real.

---

## Sistema de roles (actualizado)

| Rol | Enum value | App |
|-----|-----------:|-----|
| Cliente | 0 | clientes-app |
| Admin | 1 | intranet-app |
| OperarioOficina | 2 | intranet-app |
| OperarioCTA | 3 | intranet-app |
| Supervisor | 4 | intranet-app |
| Repartidor | 5 | driver-app |
| JefeReparto | 7 | driver-app |

Nota: el valor 6 se omite de forma intencional (rol eliminado en la reestructuracion).

---

## Credenciales por defecto (solo desarrollo)

Estas cuentas se crean automaticamente en el seed del modulo Auth.
**No usar en produccion.** Cambia passwords y elimina usuarios si es necesario.

| App | Rol | Email | Password |
|-----|-----|-------|----------|
| Intranet | Admin | admin@nexopostal.es | Admin123! |
| Intranet | OperarioOficina | operario@nexopostal.es | Operario123! |
| Intranet | OperarioOficina | operario2@nexopostal.es | Operario123! |
| Intranet | OperarioCTA | operario.cta@nexopostal.es | Operario123! |
| Intranet | OperarioCTA | operario.cta2@nexopostal.es | Operario123! |
| Intranet | Supervisor | supervisor@nexopostal.es | Operario123! |
| Intranet | Supervisor | supervisor2@nexopostal.es | Operario123! |
| Driver | Repartidor | repartidor@nexopostal.es | Repartidor123! |
| Driver | Repartidor | repartidor2@nexopostal.es | Repartidor123! |
| Driver | JefeReparto | jefe.reparto@nexopostal.es | Repartidor123! |
| Clientes | Cliente demo | cliente@example.com | Cliente123! |

---

## URLs

### Desarrollo local
- Clientes: http://localhost
- Intranet: http://localhost:4202
- Driver: http://localhost:4201

### Produccion
- Clientes: https://nexopostal.es
- Intranet: https://intranet.nexopostal.es
- Driver: https://driver.nexopostal.es

---

## Quick Start

### Desarrollo local
```bash
docker compose up -d --build
```

### Produccion (VPS)
```bash
# CI/CD despliega automaticamente al hacer push a master
```

---

## Comandos utiles

```bash
# Desarrollo
docker compose up -d --build
docker compose logs -f
docker compose down

# Produccion (manual)
docker compose -f docker-compose.production.yml up -d
docker compose -f docker-compose.production.yml logs -f
docker compose -f docker-compose.production.yml down
```

---

## CI/CD

Al hacer push a `master`, GitHub Actions automaticamente:
1. Compila .NET
2. Compila Angular
3. Build Docker
4. Despliega a VPS (DigitalOcean)

---

## Requisitos

- Docker
- Docker Compose
