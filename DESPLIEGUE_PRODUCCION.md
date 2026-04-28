# 🚀 NexoPostal — Guía de Despliegue en Producción

> Documento generado el 6 de marzo de 2026.
> Análisis completo de opciones de despliegue para la plataforma NexoPostal.

---

## 📋 Índice

1. [Resumen de la Arquitectura](#1-resumen-de-la-arquitectura)
2. [Inventario de Componentes](#2-inventario-de-componentes)
3. [Requisitos Mínimos de Producción](#3-requisitos-mínimos-de-producción)
4. [⭐ OPCIÓN RECOMENDADA — DigitalOcean con GitHub Student Pack ($200 GRATIS)](#4--opción-recomendada--digitalocean-con-github-student-pack-200-gratis)
5. [Opción B — VPS Único con Docker Compose (Otros proveedores)](#5-opción-b--vps-único-con-docker-compose-otros-proveedores)
6. [Opción C — VPS + Servicios Gestionados para BD](#6-opción-c--vps--servicios-gestionados-para-bd)
7. [Opción D — Cloud Gratuito (Free Tiers)](#7-opción-d--cloud-gratuito-free-tiers)
8. [Opción E — Kubernetes en la Nube](#8-opción-e--kubernetes-en-la-nube)
9. [Opción F — PaaS Completo (Azure / Railway / Render)](#9-opción-f--paas-completo-azure--railway--render)
10. [Comparativa de Costes](#10-comparativa-de-costes)
11. [Dominio, DNS y Certificados SSL](#11-dominio-dns-y-certificados-ssl)
12. [CI/CD — Pipeline de Despliegue](#12-cicd--pipeline-de-despliegue)
13. [Checklist de Producción](#13-checklist-de-producción)
14. [Recomendación Final](#14-recomendación-final)

---

## 1. Resumen de la Arquitectura

NexoPostal es una plataforma de logística postal con arquitectura de **microservicios**:

```
                        ┌─────────────────────────┐
                        │    NGINX Reverse Proxy   │
                        │   (SSL/TLS Termination)  │
                        │    Puertos: 80 / 443     │
                        └──────────┬──────────────┘
                                   │
               ┌───────────────────┼────────────────────┐
               │                   │                     │
     nexopostal.es      intranet.nexopostal.es   driver.nexopostal.es
               │                   │                     │
        ┌──────▼──────┐    ┌──────▼──────┐       ┌──────▼──────┐
        │ clientes-app│    │ intranet-app│       │  driver-app │
        │  (Angular)  │    │  (Angular)  │       │  (Angular)  │
        └──────┬──────┘    └──────┬──────┘       └──────┬──────┘
               │                   │                     │
               └───────────────────┼─────────────────────┘
                                   │
                          ┌────────▼────────┐
                          │   API Gateway   │
                          │  (.NET 10.0)    │
                          └────────┬────────┘
                                   │
             ┌──────────┬──────────┼──────────┬──────────┐
             │          │          │          │          │
      ┌──────▼──┐ ┌─────▼────┐ ┌──▼───────┐ ┌▼─────────┐
      │  Auth   │ │Ciudadano │ │ Intranet │ │ Reparto  │
      │(.NET10) │ │(.NET 10) │ │(.NET 10) │ │(.NET 10) │
      └────┬────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘
           │           │            │             │
      ┌────▼────┐ ┌────▼─────┐ ┌───▼──────┐ ┌───▼──────┐
      │ PG Auth │ │PG Ciudad.│ │PG Intra. │ │PG Reparto│
      │ :15432  │ │  :15433  │ │  :15434  │ │  :15435  │
      └─────────┘ └──────────┘ └──────────┘ └──────────┘
```

**Stack de Monitorización** (opcional en producción):

```
Prometheus → Grafana (dashboards)
Promtail → Loki (logs)
Watchtower (auto-update de contenedores)
```

---

## 2. Inventario de Componentes

### Contenedores de aplicación (9)

| Contenedor | Tecnología | RAM estimada | Función |
|---|---|---|---|
| `nexopostal-proxy` | Nginx | ~20 MB | Proxy inverso + SSL |
| `api-gateway` | .NET 10.0 | ~100-150 MB | Orquestador de rutas |
| `modulo-seguridad` | .NET 10.0 + Identity | ~120-180 MB | Auth + JWT |
| `modulo-ciudadano` | .NET 10.0 + SignalR | ~150-250 MB | Portal ciudadanos, pagos Stripe, PDFs, emails |
| `modulo-logistica` | .NET 10.0 + SignalR | ~150-250 MB | Back-office logístico |
| `modulo-reparto` | .NET 10.0 | ~100-150 MB | Última milla, optimización rutas |
| `clientes-app` | Angular 21 + Nginx | ~20 MB | Frontend ciudadanos |
| `intranet-app` | Angular 21 + Nginx | ~20 MB | Frontend operarios |
| `driver-app` | Angular 21 + Nginx | ~20 MB | Frontend conductores |

### Bases de datos (4)

| Base de datos | Tecnología | RAM estimada |
|---|---|---|
| `postgres-auth` | PostgreSQL 16 | ~50-100 MB |
| `postgres-ciudadano` | PostgreSQL 16 | ~50-100 MB |
| `postgres-intranet` | PostgreSQL 16 | ~50-100 MB |
| `postgres-reparto` | PostgreSQL 16 | ~50-100 MB |

### Monitorización (5 contenedores opcionales)

| Contenedor | RAM estimada |
|---|---|
| Prometheus | ~200 MB |
| Grafana | ~100 MB |
| Loki | ~150 MB |
| Promtail | ~50 MB |
| Watchtower | ~30 MB |

### Totales estimados

| Escenario | Contenedores | RAM Total Estimada |
|---|---|---|
| **Solo aplicación** | 13 | **~1.1 - 1.6 GB** |
| **Aplicación + monitorización** | 18 | **~1.6 - 2.2 GB** |

---

## 3. Requisitos Mínimos de Producción

| Recurso | Mínimo | Recomendado |
|---|---|---|
| **CPU** | 2 vCPU | 4 vCPU |
| **RAM** | 4 GB | 8 GB |
| **Disco** | 40 GB SSD | 80 GB SSD |
| **SO** | Ubuntu 22.04+ / Debian 12 | Ubuntu 24.04 LTS |
| **Docker** | Docker Engine 24+ | Docker Engine 27+ |
| **Docker Compose** | v2.20+ | v2.30+ |

---

## 4. ⭐ OPCIÓN RECOMENDADA — DigitalOcean con GitHub Student Pack ($200 GRATIS)

> **💰 Coste: $0 durante 3 meses (marzo-junio 2026) | Dificultad: Media | Ideal para: TU CASO — tienes $200 de crédito y 3 meses para aprovecharlos**

Tienes **$200 de crédito en DigitalOcean** del GitHub Student Developer Pack y necesitas usarlos **antes de junio de 2026** (~3 meses). Esto significa un presupuesto de **~$66/mes**, lo que te permite montar una infraestructura **muy potente** completamente gratis.

### ¿Qué te da el GitHub Student Developer Pack en DigitalOcean?

| Beneficio | Detalle |
|---|---|
| **Crédito** | $200 en servicios de DigitalOcean |
| **Tu plazo** | **3 meses** (marzo → junio 2026) |
| **Presupuesto mensual** | **~$66/mes** para gastar |
| **Restricción** | Solo para cuentas nuevas de DigitalOcean |
| **Activar** | [education.github.com/pack](https://education.github.com/pack) → DigitalOcean |

---

### 4.1 Plan recomendado: Droplet único con Docker Compose

El mismo enfoque del `docker-compose.yml` que ya tienes, pero en DigitalOcean aprovechando tus créditos.

#### Planes de Droplet disponibles (Premium Intel — NVMe SSD)

Estos son los planes reales que ves en tu panel. Con **~$66/mes de presupuesto** puedes ir a por una máquina potente:

| Plan | vCPU | RAM | Disco NVMe | Transfer | Precio/mes | Coste 3 meses | Cabe en $200? |
|---|---|---|---|---|---|---|---|
| Premium Intel | 1 | 1 GB | 35 GB | 1 TB | $8 | $24 | ✅ Sobra $176 (demasiado pequeño) |
| Premium Intel | 1 | 2 GB | 70 GB | 2 TB | $16 | $48 | ✅ Sobra $152 (muy justo de RAM) |
| Premium Intel | 2 | 2 GB | 90 GB | 3 TB | $24 | $72 | ✅ Sobra $128 (insuficiente RAM) |
| **Premium Intel** | **2** | **4 GB** | **120 GB** | **4 TB** | **$32** | **$96** | **✅ Sobra $104** |
| **Premium Intel** ⭐ | **2** | **8 GB** | **160 GB** | **5 TB** | **$48** | **$144** | **✅✅ RECOMENDADO** |
| Premium Intel | 4 | 8 GB | 240 GB | 6 TB | $64 | $192 | ✅ Sobra $8 (justo) |
| Premium Intel | 4 | 16 GB | 320 GB | 8 TB | $96 | $288 | ❌ Se pasa |

> **🏆 Recomendación**: El **Droplet de $48/mes (2 Intel CPUs, 8 GB RAM, 160 GB NVMe)** es la opción óptima. Tienes RAM de sobra para los 13 contenedores + monitorización completa, disco NVMe rápido, y aún te sobran **$56 del crédito** para backups y extras.
>
> La alternativa de $32/mes (4 GB RAM) también funciona, pero irás más justo de RAM si activas la monitorización.

#### Desglose de costes — 3 meses con $200 (RECOMENDADO)

| Servicio DigitalOcean | Coste/mes | Coste 3 meses | Subtotal |
|---|---|---|---|
| Droplet 8 GB Premium Intel (2 vCPU, 160 GB NVMe) | $48 | $144 | $144 |
| Backups automáticos (20% del droplet) | $9.60 | $28.80 | $172.80 |
| **TOTAL** | **$57.60** | **$172.80** | ✅ **Sobran $27.20** |

> Con el plan recomendado gastas **$172.80 en 3 meses**, te sobran **$27.20** que puedes usar en:
> - **DigitalOcean Spaces** ($5/mes × 3 = $15) — almacenamiento de backups de BD
> - **Snapshots extra** bajo demanda
> - **Managed Database** un mes para probar (+$15)

#### Alternativa con Droplet de 4 vCPU ($64/mes)

| Servicio DigitalOcean | Coste/mes | Coste 3 meses | Subtotal |
|---|---|---|---|
| Droplet 8 GB Premium Intel (4 vCPU, 240 GB NVMe) | $64 | $192 | $192 |
| Backups automáticos | ❌ no activar | $0 | $192 |
| **TOTAL** | **$64** | **$192** | ✅ **Sobran $8** |

> Con 4 vCPU y 240 GB NVMe tienes el doble de CPU y disco, pero no te da para backups automáticos. Puedes hacer backups manuales con `pg_dump` + cron (gratis).

---

### 4.2 Guía paso a paso — Despliegue en DigitalOcean

#### Paso 1: Activar el crédito

1. Ve a [education.github.com/pack](https://education.github.com/pack)
2. Busca **DigitalOcean** en la lista de ofertas
3. Haz clic en "Get access" / "Activate"
4. Crea una cuenta en DigitalOcean con el enlace proporcionado
5. Verifica que los **$200 aparecen** en Billing → Credits

#### Paso 2: Crear el Droplet

> ⚠️ **IMPORTANTE**: Tu panel muestra **SFO3 (San Francisco)** por defecto. **Cámbialo a Frankfurt o Amsterdam** para tener la menor latencia desde España.

```
1. Panel de DigitalOcean → Create → Droplets

2. 🌍 REGIÓN: ¡¡CAMBIAR!! No dejes San Francisco (SFO3)
   → Seleccionar: Frankfurt (FRA1) o Amsterdam (AMS3)
   (Son los datacenter más cercanos a España, ~30-40ms de latencia)

3. 💾 IMAGEN: Ubuntu (ya seleccionado) → Versión: 24.04 LTS

4. 📊 TAMAÑO:
   • Droplet Type: Basic (Shared CPU) ✔️
   • CPU options: Premium Intel (NVMe SSD) ✔️
   • Plan: $48/mo → 8 GB / 2 Intel CPUs / 160 GB NVMe / 5 TB transfer
   (Es el que está justo a la derecha del seleccionado actualmente)

5. 🔐 AUTENTICACIÓN: SSH Key (más seguro) o Password
   • Si eliges Password: usa una contraseña fuerte (mín. 10 chars, mayúscula, número)
   • Si eliges SSH Key: sube tu clave pública (~/.ssh/id_rsa.pub)

6. ✅ OPCIONES RECOMENDADAS:
   • ✅ Enable automated backup plan (añade $9.60/mes)
   • ✅ Add improved metrics monitoring and alerting (GRATIS)
   • ❌ Add a worry-free Managed Database → NO (lo montamos con Docker)

7. 🏠 HOSTNAME: nexopostal-prod

8. Click "Create Droplet"
```

> 💡 **Sobre la Managed Database (+$15)**: DigitalOcean te ofrece añadir PostgreSQL gestionado. **No lo necesitas ahora** porque tu `docker-compose.yml` ya incluye 4 contenedores PostgreSQL. Si más adelante quieres backups automáticos de BD con PITR y failover, puedes añadirlo después (ver [sección 4.4](#44-alternativa-avanzada-digitalocean-managed-database--droplet-reducido)).

#### Paso 3: Configurar el servidor

```bash
# 1. Conectar al Droplet
ssh root@TU_IP_DROPLET

# 2. Actualizar el sistema
apt update && apt upgrade -y

# 3. Instalar Docker y Docker Compose
curl -fsSL https://get.docker.com | sh
apt install docker-compose-plugin -y

# 4. Configurar firewall (UFW)
ufw allow 22/tcp    # SSH
ufw allow 80/tcp    # HTTP
ufw allow 443/tcp   # HTTPS
ufw enable

# 5. Crear usuario no-root (buenas prácticas)
adduser deploy
usermod -aG sudo deploy
usermod -aG docker deploy

# 6. Copiar claves SSH al nuevo usuario
rsync --archive --chown=deploy:deploy ~/.ssh /home/deploy

# 7. Reconectar como el nuevo usuario
exit
ssh deploy@TU_IP_DROPLET
```

#### Paso 4: Desplegar NexoPostal

```bash
# 1. Clonar el repositorio
cd /opt
sudo git clone https://github.com/SrAngelDev/NexoPostal.git
sudo chown -R deploy:deploy NexoPostal
cd NexoPostal

# 2. Crear archivo .env de producción
cat > .env << 'EOF'
# ============================================
# NEXOPOSTAL — PRODUCCIÓN (DigitalOcean)
# ============================================

# Entorno
ASPNETCORE_ENVIRONMENT=Production

# JWT (genera una clave segura: openssl rand -base64 64)
JWT_SECRET_KEY=GENERA_UNA_CLAVE_SEGURA_CON_openssl_rand_base64_64

# PostgreSQL (cambia estas contraseñas!)
POSTGRES_PASSWORD=CambiaEstaPorUnaContraseñaSegura2026!

# Grafana
GRAFANA_PASSWORD=OtraContraseñaSeguraParaGrafana2026!

# Stripe (modo live)
STRIPE_SECRET_KEY=sk_live_...
STRIPE_WEBHOOK_SECRET=whsec_...

# Email (MailKit)
SMTP_HOST=smtp.tuproveedor.com
SMTP_PORT=587
SMTP_USER=noreply@nexopostal.es
SMTP_PASSWORD=tu_contraseña_smtp
EOF

# 3. Generar JWT Secret Key segura
echo "JWT_SECRET_KEY=$(openssl rand -base64 64 | tr -d '\n')" 
# Copia el resultado y pégalo en el .env

# 4. Levantar toda la aplicación
docker compose up -d

# 5. Verificar que todo está corriendo
docker compose ps

# 6. (Opcional) Añadir monitorización
docker compose -f docker-compose.monitoring.yml up -d
```

#### Paso 5: SSL con Let's Encrypt (GRATUITO)

```bash
# Instalar Certbot
sudo apt install certbot -y

# Detener nginx temporalmente
docker compose stop reverse-proxy

# Generar certificados para los 3 dominios
sudo certbot certonly --standalone \
  -d nexopostal.es \
  -d intranet.nexopostal.es \
  -d driver.nexopostal.es

# Copiar certificados al directorio del proyecto
sudo cp /etc/letsencrypt/live/nexopostal.es/fullchain.pem ./nginx/certs/nexopostal.crt
sudo cp /etc/letsencrypt/live/nexopostal.es/privkey.pem ./nginx/certs/nexopostal.key
sudo chown deploy:deploy ./nginx/certs/*

# Reiniciar todo
docker compose up -d

# Configurar auto-renovación
sudo crontab -e
# Añadir esta línea:
# 0 3 1 */2 * certbot renew --pre-hook 'cd /opt/NexoPostal && docker compose stop reverse-proxy' --post-hook 'cp /etc/letsencrypt/live/nexopostal.es/fullchain.pem /opt/NexoPostal/nginx/certs/nexopostal.crt && cp /etc/letsencrypt/live/nexopostal.es/privkey.pem /opt/NexoPostal/nginx/certs/nexopostal.key && cd /opt/NexoPostal && docker compose up -d reverse-proxy'
```

#### Paso 6: Configurar DNS

Tienes dos opciones para el DNS:

**Opción A — DNS en DigitalOcean (incluido gratis):**

```
1. Panel DigitalOcean → Networking → Domains
2. Añadir dominio: nexopostal.es
3. Crear registros:
   - nexopostal.es          → A → IP_DEL_DROPLET
   - intranet.nexopostal.es → A → IP_DEL_DROPLET
   - driver.nexopostal.es   → A → IP_DEL_DROPLET
4. En tu registrador de dominio, cambiar los nameservers a:
   - ns1.digitalocean.com
   - ns2.digitalocean.com
   - ns3.digitalocean.com
```

**Opción B — DNS en Cloudflare (CDN + DDoS gratis):**

```
1. Crear cuenta en cloudflare.com
2. Añadir sitio nexopostal.es (plan Free)
3. Crear registros A apuntando a la IP del Droplet
4. Cambiar nameservers en tu registrador a los de Cloudflare
5. Activar proxy (nube naranja) para CDN y protección DDoS gratis
```

---

### 4.3 Extras de DigitalOcean incluidos GRATIS

DigitalOcean ofrece varios servicios gratuitos que puedes aprovechar:

| Servicio | Coste | Descripción |
|---|---|---|
| **Monitoring** | GRATIS | Métricas de CPU, RAM, disco, red del Droplet |
| **Alertas** | GRATIS | Alertas por email cuando CPU > 80%, disco lleno, etc. |
| **Firewall Cloud** | GRATIS | Firewall a nivel de red (complementa UFW) |
| **DNS Hosting** | GRATIS | DNS autoritativo para tu dominio |
| **Snapshots manuales** | $0.06/GB/mes | Snapshot bajo demanda del Droplet |
| **DigitalOcean Spaces** | $5/mes (250 GB) | Object storage (para backups de BD) |
| **Container Registry** | GRATIS (500 MB) | Registry privado para tus imágenes Docker |

---

### 4.4 Alternativa avanzada: DigitalOcean Managed Database + Droplet reducido

En la pantalla de creación del Droplet ves la opción **"Add a worry-free Managed Database (+$15.00)"**. Si la activas, no necesitas los contenedores PostgreSQL en Docker:

| Servicio | Coste/mes | Coste 3 meses |
|---|---|---|
| Droplet Premium Intel 4 GB (2 vCPU, 120 GB NVMe) | $32 | $96 |
| Managed PostgreSQL (1 GB RAM, 10 GB, backups diarios + PITR) | $15 | $45 |
| Backups del Droplet | $6.40 | $19.20 |
| **Total** | **$53.40** | **$160.20 ✅** |

> Te sobrarían **$39.80** del crédito. Con esta opción usas **una sola instancia PostgreSQL gestionada** con las 4 bases de datos dentro (mismo patrón Database-per-Service pero en un solo servidor PostgreSQL). Incluye:
> - **Backups diarios** automáticos con retención de 7 días
> - **PITR** (Point-in-Time Recovery) — restaurar a cualquier minuto
> - **Failover automático** — alta disponibilidad
> - **SSL end-to-end** incluido
> - **Actualizaciones de seguridad** automáticas

```bash
# En docker-compose.yml: quitar los 4 servicios postgres-* y sus volúmenes
# Cambiar la connection string de cada microservicio a:
ConnectionStrings__DefaultConnection=Host=db-nexopostal-do-user-xxxxx-0.db.ondigitalocean.com;Port=25060;Database=nexopostal_auth;Username=doadmin;Password=xxxxx;SSL Mode=Require
```

---

### 4.5 Distribución de los $200 en 3 meses — Escenarios

| Escenario | Coste/mes | Coste 3 meses | Sobra | Incluye |
|---|---|---|---|---|
| **Mínimo** | $32 | $96 | $104 | Droplet 4 GB NVMe, todo en Docker |
| **Mínimo + backups** | $38.40 | $115.20 | $84.80 | Droplet 4 GB NVMe + backups |
| **BD gestionada** | $53.40 | $160.20 | $39.80 | Droplet 4 GB NVMe + PG gestionado + backups |
| **⭐ RECOMENDADO** | **$57.60** | **$172.80** | **$27.20** | **Droplet 8 GB NVMe + backups** |
| **Premium + Spaces** | $62.60 | $187.80 | $12.20 | Droplet 8 GB NVMe + backups + Spaces |
| **4 vCPU (sin backups)** | $64 | $192 | $8 | Droplet 8 GB 4vCPU NVMe, backups manuales |

> 💡 **Con $200 y 3 meses, la mejor relación es el Droplet de 8 GB ($48) + backups ($9.60) = $57.60/mes.** Gastas $172.80, sobran $27.20.

### 4.6 ¿Y después de junio 2026? (cuando se acabe el crédito)

En junio se acaba tu crédito. Tienes estas opciones:

1. **🏆 Migrar a Hetzner CX22** (4,35 €/mes) — el más barato de Europa. Haces un backup de las BD con `pg_dump`, transfieres los datos y levantas el mismo `docker-compose.yml` en Hetzner. Migración de ~1 hora.
2. **Reducir el Droplet** a $32/mes (4 GB) o $16/mes (2 GB) en DigitalOcean.
3. **Solicitar más créditos** — el GitHub Student Pack se puede renovar si sigues siendo estudiante.

#### Guía rápida de migración a Hetzner (post-junio)

```bash
# === EN EL DROPLET DE DIGITALOCEAN (antes de apagarlo) ===

# 1. Backup de todas las bases de datos
docker exec postgres-auth pg_dump -U postgres nexopostal_auth > backup_auth.sql
docker exec postgres-ciudadano pg_dump -U postgres nexopostal_ciudadano > backup_ciudadano.sql
docker exec postgres-intranet pg_dump -U postgres nexopostal_intranet > backup_intranet.sql
docker exec postgres-reparto pg_dump -U postgres nexopostal_reparto > backup_reparto.sql

# 2. Copiar backups y .env al nuevo VPS Hetzner
scp backup_*.sql .env deploy@IP_HETZNER:/opt/NexoPostal/

# === EN EL NUEVO VPS DE HETZNER ===

# 3. Clonar repo, copiar .env, levantar
cd /opt && git clone https://github.com/SrAngelDev/NexoPostal.git && cd NexoPostal
docker compose up -d

# 4. Restaurar bases de datos
docker exec -i postgres-auth psql -U postgres nexopostal_auth < backup_auth.sql
docker exec -i postgres-ciudadano psql -U postgres nexopostal_ciudadano < backup_ciudadano.sql
docker exec -i postgres-intranet psql -U postgres nexopostal_intranet < backup_intranet.sql
docker exec -i postgres-reparto psql -U postgres nexopostal_reparto < backup_reparto.sql

# 5. Actualizar DNS a la nueva IP
# Cambiar registros A en Cloudflare/DigitalOcean DNS
```

### Ventajas de DigitalOcean

- ✅ **$200 gratis** — 3 meses de producción premium sin coste
- ✅ **8 GB de RAM + NVMe** con tu presupuesto — app + monitorización completa sin problemas
- ✅ **Disco NVMe SSD** — mucho más rápido que SSD normal (ideal para PostgreSQL)
- ✅ Panel de control intuitivo y bien documentado
- ✅ Monitoring y alertas incluidos gratis
- ✅ Data center en Frankfurt/Amsterdam (baja latencia desde España)
- ✅ Snapshots y backups integrados
- ✅ Firewall cloud gratuito
- ✅ Container Registry gratuito (500 MB)
- ✅ DNS hosting gratuito
- ✅ Excelente documentación y tutoriales
- ✅ Tu `docker-compose.yml` funciona tal cual
- ✅ Migración fácil a Hetzner después (mismo Docker Compose)

### Desventajas

- ❌ Solo 3 meses con el crédito (marzo → junio 2026)
- ❌ Después hay que migrar o pagar $48/mes
- ❌ Punto único de fallo (un solo Droplet)

---

## 5. Opción B — VPS Único con Docker Compose (Otros proveedores)

> **💰 Coste: 4-7 €/mes | Dificultad: Media | Ideal para: Después de DigitalOcean, o si prefieres menor coste mensual**

Misma arquitectura que la opción A pero en proveedores más baratos (sin créditos gratis).

### Proveedores alternativos

| Proveedor | Plan | vCPU | RAM | Disco | Precio/mes | Enlace |
|---|---|---|---|---|---|---|
| **Hetzner Cloud** ⭐ | CX22 | 2 | 4 GB | 40 GB SSD | **4,35 €** | [hetzner.com/cloud](https://www.hetzner.com/cloud) |
| **Hetzner Cloud** | CX32 | 4 | 8 GB | 80 GB SSD | **7,35 €** | [hetzner.com/cloud](https://www.hetzner.com/cloud) |
| **Netcup** | VPS 1000 G11 | 4 | 8 GB | 256 GB SSD | **8,63 €** | [netcup.eu](https://www.netcup.eu) |
| **Contabo** | Cloud VPS S | 4 | 8 GB | 200 GB SSD | **6,99 €** | [contabo.com](https://contabo.com) |
| **OVHcloud** | VPS Starter | 2 | 4 GB | 80 GB SSD | **5,52 €** | [ovhcloud.com](https://www.ovhcloud.com) |
| **Ionos** | VPS Linux M | 2 | 4 GB | 160 GB SSD | **6 €** | [ionos.es](https://www.ionos.es) |

Los pasos de despliegue son idénticos a la [Opción A (DigitalOcean)](#42-guía-paso-a-paso--despliegue-en-digitalocean), simplemente cambiando el proveedor del VPS.

### Ventajas

- ✅ **Más barato a largo plazo** (desde 4,35 €/mes)
- ✅ Ya tienes el `docker-compose.yml` listo
- ✅ Un solo servidor, fácil de administrar
- ✅ Hetzner tiene data centers en Europa (baja latencia)

### Desventajas

- ❌ Sin créditos gratis (pagas desde el día 1)
- ❌ Punto único de fallo

---

## 6. Opción C — VPS + Servicios Gestionados para BD

> **💰 Coste: 15-30 €/mes | Dificultad: Media | Ideal para: Producción "seria" con datos importantes**

Las bases de datos se mueven a un servicio gestionado (backups automáticos, alta disponibilidad, actualizaciones de seguridad incluidas). El VPS solo ejecuta los contenedores de aplicación.

### Arquitectura

```
┌──────────────────────────────┐     ┌────────────────────────────┐
│         VPS (Hetzner)        │     │   PostgreSQL Gestionado    │
│  • Nginx Proxy               │────▶│  (Supabase / Neon / Aiven) │
│  • API Gateway               │     │  4 bases de datos          │
│  • 4 Microservicios .NET     │     │  Backups automáticos       │
│  • 3 Frontends Angular       │     │  SSL incluido              │
│  RAM necesaria: ~2 GB        │     └────────────────────────────┘
└──────────────────────────────┘
```

### Proveedores de PostgreSQL gestionado

| Proveedor | Plan Gratuito | Plan Pago | Límites Free | Notas |
|---|---|---|---|---|
| **Supabase** ⭐ | Sí | Desde 25 $/mes | 500 MB, 2 proyectos | Puedes crear 2 proyectos gratis (necesitas 4 BD → 2 gratis + 2 pagando o consolidar BD) |
| **Neon** ⭐ | Sí | Desde 19 $/mes | 512 MB, 1 proyecto, branching | Escala a cero (no consume cuando no se usa). Puedes tener 4 BD en 1 proyecto |
| **Aiven** | Sí | Desde 19 €/mes | 1 servicio gratuito | PostgreSQL gestionado de alta calidad |
| **Railway** | Sí (trial 5$) | Desde 5 $/mes | — | Simple, integrado con deploy |
| **ElephantSQL** | Sí | Desde 19 $/mes | 20 MB gratis | Muy limitado en free tier |

### Opción más barata: Neon (GRATIS para las 4 BD)

Neon permite crear múltiples bases de datos dentro de un solo proyecto gratuito:

```
# En las variables de entorno del docker-compose, cambiar:
ConnectionStrings__DefaultConnection=Host=ep-xxxx.eu-central-1.aws.neon.tech;Port=5432;Database=nexopostal_auth;Username=neondb_owner;Password=xxx;SSL Mode=Require
```

**Coste total con Neon gratuito:**

| Componente | Coste |
|---|---|
| VPS Hetzner CX22 (2 vCPU, 4 GB) | 4,35 €/mes |
| Neon PostgreSQL (4 BD) | **GRATIS** |
| **TOTAL** | **~4,35 €/mes** |

### Ventajas

- ✅ VPS más barato (sin BD, necesita menos RAM)
- ✅ Backups automáticos de BD
- ✅ Actualizaciones de seguridad de BD gestionadas
- ✅ Posibilidad de escalar BD independientemente

### Desventajas

- ❌ Latencia añadida entre VPS y BD (red pública)
- ❌ Dependencia de un proveedor externo para BD
- ❌ Neon free tier tiene límites de compute (auto-suspend tras inactividad)

---

## 7. Opción D — Cloud Gratuito (Free Tiers)

> **💰 Coste: GRATIS (0 €) | Dificultad: Alta | Ideal para: Demo, prueba de concepto, portfolio**

Utilizar los tiers gratuitos de múltiples proveedores cloud para ejecutar cada componente.

### ⚠️ Limitaciones importantes

- Los free tiers tienen **cold starts** (5-30 segundos de espera tras inactividad)
- La **RAM es muy limitada** (256-512 MB por servicio)
- Los servicios se **suspenden tras inactividad** (15-30 min)
- **No soportan WebSockets** en algunos proveedores (problema para SignalR)
- Rendimiento **inconsistente**

### Distribución gratuita

| Componente | Proveedor | Plan | Límite |
|---|---|---|---|
| **3 Frontends Angular** | **Cloudflare Pages** | Free | Ilimitado (sitios estáticos) |
| **API Gateway** | **Render** | Free Web Service | 750 h/mes, 512 MB RAM, spin-down tras 15min |
| **modulo-seguridad** | **Render** | Free Web Service | 750 h/mes, 512 MB RAM |
| **modulo-ciudadano** | **Koyeb** | Free | 1 servicio, 512 MB RAM, eco-friendly |
| **modulo-logistica** | **Fly.io** | Free | 3 VMs compartidas, 256 MB cada una |
| **modulo-reparto** | **Fly.io** | Free | (dentro de las 3 VMs) |
| **4 Bases de datos** | **Neon** | Free | 512 MB, 1 proyecto, 4 BD |
| **Monitorización** | ❌ No viable | — | Demasiados recursos |

### Despliegue de frontends en Cloudflare Pages (GRATIS e ILIMITADO)

Los 3 frontends Angular son aplicaciones estáticas → perfectos para CDN gratuita:

```bash
# 1. Build de producción
cd clientes-app
npm run build -- --configuration=production
# Genera dist/clientes-app/browser/

# 2. Subir a Cloudflare Pages
# - Ir a dash.cloudflare.com → Pages → Create Project
# - Conectar repositorio GitHub
# - Build command: cd clientes-app && npm install && npm run build
# - Output: clientes-app/dist/clientes-app/browser
# - Repetir para intranet-app y driver-app
```

**Cada frontend con su propio dominio custom:**
- `nexopostal.es` → Cloudflare Pages (clientes-app)
- `intranet.nexopostal.es` → Cloudflare Pages (intranet-app)
- `driver.nexopostal.es` → Cloudflare Pages (driver-app)

### Ventajas

- ✅ **Completamente gratis**
- ✅ Los frontends en Cloudflare Pages son muy rápidos (CDN global)
- ✅ Ideal para demos y portfolio

### Desventajas

- ❌ **Cold starts** — los microservicios tardan 5-30s en arrancar tras inactividad
- ❌ **SignalR limitado** — WebSockets no funcionan en todos los free tiers
- ❌ **Fragmentación** — cada servicio en un proveedor diferente, complejo de mantener
- ❌ **No apto para producción real** con usuarios activos
- ❌ Sin monitorización
- ❌ Los background services (.NET) no se ejecutarán correctamente con spin-down

---

## 8. Opción E — Kubernetes en la Nube

> **💰 Coste: 30-80 €/mes | Dificultad: Alta | Ideal para: Escalabilidad futura, equipos grandes**

### Proveedores

| Proveedor | Servicio | Control Plane | Workers (mín.) | Coste estimado/mes |
|---|---|---|---|---|
| **DigitalOcean** | DOKS | GRATIS | 1 nodo (4GB) = $24 | ~30 $ |
| **Hetzner** | k3s manual | — | 1 nodo CX32 = 7,35€ | ~7-15 € |
| **OVH** | Managed K8s | GRATIS | 1 nodo b2-7 = ~12€ | ~12 € |
| **Google Cloud** | GKE Autopilot | GRATIS* | Pay-per-pod | ~40-60 $ |
| **Azure** | AKS | GRATIS | 1 nodo B2s = ~30€ | ~30-50 € |

### Hetzner + k3s (la más barata)

Instalar k3s (Kubernetes ligero) en un VPS de Hetzner:

```bash
# En el VPS
curl -sfL https://get.k3s.io | sh -

# Desplegar NexoPostal con manifiestos K8s
# (Convertir docker-compose.yml a manifiestos con kompose)
kompose convert -f docker-compose.yml
kubectl apply -f .
```

### Ventajas

- ✅ Escalabilidad horizontal (añadir nodos)
- ✅ Auto-healing (reinicia contenedores caídos)
- ✅ Rolling updates sin downtime
- ✅ Ingress controller con cert-manager (SSL automático)

### Desventajas

- ❌ Curva de aprendizaje alta
- ❌ Más caro que un VPS simple
- ❌ Over-engineering para un proyecto de este tamaño
- ❌ Necesitas conocimientos de K8s

---

## 9. Opción F — PaaS Completo (Azure / Railway / Render)

> **💰 Coste: 20-100+ €/mes | Dificultad: Baja | Ideal para: No querer gestionar infraestructura**

### E.1 Railway

| Recurso | Free | Pro (5$/mes base) |
|---|---|---|
| Créditos | 5$ trial | 5$/mes incluido, luego por uso |
| RAM | 512 MB/servicio | 8 GB/servicio |
| Persistencia | Sí | Sí |
| PostgreSQL | Incluido | Incluido |

**Coste estimado:** 9 servicios (.NET + frontends) × ~2-5 $/mes = **~20-45 $/mes**

```bash
# Railway CLI
npm install -g @railway/cli
railway login
railway init
railway up  # Deploy desde el directorio
```

### E.2 Azure Container Apps

| Plan | Incluye | Coste |
|---|---|---|
| Consumption | 180.000 vCPU·s/mes gratis, 360.000 GiB·s/mes gratis | Pay-as-you-go después |
| PostgreSQL Flexible | Tier Burstable B1ms | ~15 €/mes |

**Coste estimado con tráfico bajo:** **~15-30 €/mes**

```bash
# Azure CLI
az containerapp compose create \
  --resource-group nexopostal-rg \
  --environment nexopostal-env \
  --compose-file-path docker-compose.yml
```

### E.3 Google Cloud Run

| Recurso | Free Tier |
|---|---|
| Requests | 2 millones/mes |
| vCPU | 180.000 vCPU·s/mes |
| RAM | 360.000 GiB·s/mes |
| Cloud SQL (PostgreSQL) | Sin free tier (~10 $/mes mín.) |

**Coste estimado:** **~15-40 $/mes** (principalmente por Cloud SQL)

### Ventajas

- ✅ Zero mantenimiento de infraestructura
- ✅ Auto-scaling
- ✅ SSL automático
- ✅ CI/CD integrado

### Desventajas

- ❌ Más caro que VPS
- ❌ Vendor lock-in
- ❌ Menos control sobre la configuración
- ❌ WebSockets/SignalR pueden requerir configuración especial

---

## 10. Comparativa de Costes

| Opción | Coste/mes | Con tus $200 | Dificultad | Prod-Ready | SignalR | Monitorización | Escalable |
|---|---|---|---|---|---|---|---|
| **A. DigitalOcean** ⭐⭐⭐ | $24-28 | **$0 (7-8 meses)** | Media | ✅✅ | ✅ | ✅ | ❌ |
| **B. VPS otros (Hetzner)** | **4-7 €** | N/A | Media | ✅ | ✅ | ✅ | ❌ |
| **C. VPS + BD gestionada** | **4-30 €** | N/A | Media | ✅✅ | ✅ | ✅ | Parcial |
| **D. Cloud Gratuito** | **0 €** | N/A | Alta | ❌ | ⚠️ | ❌ | ❌ |
| **E. Kubernetes (k3s)** | **7-80 €** | N/A | Alta | ✅✅✅ | ✅ | ✅ | ✅✅✅ |
| **F. PaaS** | **15-100 €** | N/A | Baja | ✅✅ | ⚠️ | Parcial | ✅✅ |

---

## 11. Dominio, DNS y Certificados SSL

### Dominio `nexopostal.es`

| Proveedor | Precio/año | DNS incluido |
|---|---|---|
| **Cloudflare Registrar** ⭐ | ~8-10 € | Sí (+ CDN gratis + protección DDoS) |
| **Namecheap** | ~10-15 € | Sí |
| **Porkbun** | ~8-12 € | Sí |
| **Google Domains** (Squarespace) | ~12 € | Sí |
| **Dondominio** (España) | ~8-15 € | Sí |

### Configuración DNS

```
# Registros DNS necesarios (apuntando a la IP del VPS)
nexopostal.es          A    → IP_DEL_VPS
intranet.nexopostal.es A    → IP_DEL_VPS
driver.nexopostal.es   A    → IP_DEL_VPS

# O con Cloudflare (proxy activado = CDN + protección gratis)
nexopostal.es          A    → IP_DEL_VPS  (Proxied ☁️)
intranet.nexopostal.es A    → IP_DEL_VPS  (DNS Only ☁️ — para WebSocket)
driver.nexopostal.es   A    → IP_DEL_VPS  (Proxied ☁️)
```

> ⚠️ **Nota sobre WebSockets**: Si usas Cloudflare con proxy activado, los WebSockets (SignalR) funcionan en el plan gratuito, pero puede haber limitaciones. Si experimentas problemas, desactiva el proxy para `intranet.nexopostal.es` (donde se usa más SignalR).

### Certificados SSL (GRATIS)

| Método | Coste | Renovación |
|---|---|---|
| **Let's Encrypt + Certbot** ⭐ | Gratis | Auto cada 90 días |
| **Cloudflare Origin Certificates** | Gratis | 15 años |
| **ZeroSSL** | Gratis | 90 días |

---

## 12. CI/CD — Pipeline de Despliegue

### GitHub Actions (GRATIS para repos públicos, 2000 min/mes para privados)

Crear `.github/workflows/deploy.yml`:

```yaml
name: Deploy NexoPostal

on:
  push:
    branches: [master]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      # Build y push de imágenes Docker
      - name: Login to Docker Hub
        uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKER_USERNAME }}
          password: ${{ secrets.DOCKER_PASSWORD }}

      - name: Build and push images
        run: |
          docker compose build
          docker compose push

      # Deploy al VPS via SSH
      - name: Deploy to VPS
        uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.VPS_HOST }}
          username: ${{ secrets.VPS_USER }}
          key: ${{ secrets.VPS_SSH_KEY }}
          script: |
            cd /opt/NexoPostal
            git pull origin master
            docker compose pull
            docker compose up -d --build
            docker system prune -f
```

### Alternativa: Watchtower (ya incluido)

Tu `docker-compose.monitoring.yml` ya incluye **Watchtower**, que actualiza automáticamente los contenedores cuando detecta nuevas imágenes en el registry. Solo necesitas:

1. Publicar las imágenes a Docker Hub o GitHub Container Registry (GHCR)
2. Watchtower las descarga automáticamente a las 04:00 cada día

---

## 13. Checklist de Producción

### Seguridad

- [ ] Cambiar **todas las contraseñas** por defecto (PostgreSQL, Grafana, etc.)
- [ ] Configurar una **JWT Secret Key** fuerte (≥64 caracteres, generada aleatoriamente)
- [ ] Establecer `ASPNETCORE_ENVIRONMENT=Production` en todos los microservicios
- [ ] **Eliminar Swagger UI** en producción (ya lo hace tu código con el `if (app.Environment.IsDevelopment())`)
- [ ] **No exponer puertos de PostgreSQL** al exterior (quitar los `ports: 15432-15435`)
- [ ] Configurar **firewall** (UFW) — solo permitir puertos 22, 80, 443
- [ ] Deshabilitar acceso SSH por contraseña (solo clave pública)
- [ ] Configurar **fail2ban** contra fuerza bruta SSH
- [ ] Usar **Cloudflare** como proxy para protección DDoS

### Rendimiento

- [ ] Habilitar **GZIP** en Nginx para los frontends
- [ ] Configurar **cache headers** para assets estáticos de Angular
- [ ] Ajustar `shared_buffers` y `work_mem` de PostgreSQL según RAM disponible
- [ ] Considerar **connection pooling** con PgBouncer si hay muchas conexiones

### Datos

- [ ] Configurar **backups automáticos** de las 4 bases de datos PostgreSQL
- [ ] Programar **snapshots del VPS** (semanal)
- [ ] Configurar un **volumen externo** o block storage para datos persistentes

### Monitorización

- [ ] Desplegar stack de monitorización (`docker-compose.monitoring.yml`)
- [ ] Configurar **alertas** en Grafana (caída de servicio, disco lleno, RAM alta)
- [ ] Configurar **uptime monitoring** externo gratuito (UptimeRobot, Hetrixtools)

### Entorno

- [ ] Configurar archivo `.env` con todos los secretos
- [ ] **No** subir `.env` a Git (añadir a `.gitignore`)
- [ ] Crear `.env.example` con valores placeholder
- [ ] Configurar variables de Stripe en modo **Live** (no test)
- [ ] Configurar SMTP de producción para MailKit

### Docker

- [ ] Poner `restart: unless-stopped` en todos los servicios
- [ ] Limitar recursos de contenedores con `deploy.resources.limits`
- [ ] Configurar **log rotation** para evitar que los logs llenen el disco

```yaml
# Ejemplo: añadir a todos los servicios en docker-compose.yml
logging:
  driver: "json-file"
  options:
    max-size: "10m"
    max-file: "3"
```

---

## 14. Recomendación Final

### 🏆🏆🏆 MEJOR OPCIÓN PARA TI: DigitalOcean 3 meses GRATIS ($200 GitHub Student Pack)

> **Droplet 8 GB + Backups + Cloudflare + Let's Encrypt = $0 durante 3 meses**

| Componente | Coste real/mes | Coste 3 meses | Con tus $200 |
|---|---|---|---|
| Droplet 8 GB Premium Intel (2 vCPU, 160 GB NVMe) | $48 | $144 | **$0** |
| Backups automáticos del Droplet | $9.60 | $28.80 | **$0** |
| Monitoring + Alertas DigitalOcean | GRATIS | GRATIS | GRATIS |
| DNS DigitalOcean | GRATIS | GRATIS | GRATIS |
| Container Registry (500 MB) | GRATIS | GRATIS | GRATIS |
| Cloudflare CDN + protección DDoS | GRATIS | GRATIS | GRATIS |
| Let's Encrypt SSL | GRATIS | GRATIS | GRATIS |
| GitHub Actions CI/CD | GRATIS | GRATIS | GRATIS |
| Dominio `nexopostal.es` | ~0,70 € | ~2,10 € | ~2,10 € |
| **TOTAL crédito DigitalOcean** | **$57.60** | **$172.80** | **Sobran $27.20** |
| **TOTAL de tu bolsillo** | **~0,70 €** | **~2,10 €** | **Solo el dominio** |

### Plan de acción

```
📅 MARZO 2026 (AHORA):
   1. Activar crédito DigitalOcean en education.github.com/pack
   2. Crear Droplet 8 GB Premium Intel ($48/mes) en Frankfurt
      • ¡¡NO dejar San Francisco!! Cambiarlo a Frankfurt (FRA1)
   3. Desplegar con docker compose up -d
   4. Configurar SSL con Let's Encrypt
   5. Configurar DNS (Cloudflare o DigitalOcean)
   → Tienes NexoPostal en producción con:
     • 2 Intel CPUs, 8 GB RAM, 160 GB NVMe SSD
     • App completa + monitorización (Prometheus+Grafana+Loki)
     • Backups automáticos del Droplet

📅 JUNIO 2026 (crédito agotado):
   Opción 1: Migrar a Hetzner CX22 (4,35 €/mes) ← RECOMENDADO
   Opción 2: Reducir Droplet a $24/mes si genera ingresos
   Opción 3: Renovar Student Pack si sigues siendo estudiante
```

### 🏆 Después de junio — Hetzner (4,35 €/mes):

| Componente | Coste |
|---|---|
| VPS Hetzner CX22 (2 vCPU, 4 GB RAM) | 4,35 €/mes |
| Dominio + Cloudflare + SSL | ~0,70 €/mes |
| **TOTAL** | **~5 €/mes** |

> La migración es sencilla: `pg_dump` de las 4 BD, clonar repo en Hetzner, `docker compose up -d`, restaurar BD, cambiar DNS. ~1 hora de trabajo.

---

### Resumen ejecutivo

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│   🎓 NexoPostal — 3 meses GRATIS en DigitalOcean                      │
│                                                                        │
│   $200 crédito ÷ 3 meses = $66/mes de presupuesto                      │
│                                                                        │
│   ⭐ Droplet Premium Intel: 2 vCPU, 8 GB RAM, 160 GB NVMe ($48/mes) │
│   ✅ 18 contenedores Docker (app + monitorización completa)           │
│   ✅ 4 bases de datos PostgreSQL con backups automáticos              │
│   ✅ Prometheus + Grafana + Loki (monitorización avanzada)            │
│   ✅ SSL gratuito con Let's Encrypt                                   │
│   ✅ CDN y protección DDoS con Cloudflare (gratis)                    │
│   ✅ CI/CD con GitHub Actions (gratis)                                │
│                                                                        │
│   Coste total: $172.80 de crédito + ~2€ de dominio                     │
│   Después de junio → Hetzner por 4,35 €/mes                           │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```
