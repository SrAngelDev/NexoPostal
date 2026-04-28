# ============================================
# NEXOPOSTAL - GUÍA DE DESPLIEGUE
# ============================================

## Requisitos Previos

1. **Docker y Docker Compose** instalados
2. **Git** para clonar el repositorio
3. **OpenSSL** para generar certificados (incluido en Git Bash, macOS, Linux)

---

## Paso 1: Preparar el Entorno

### 1.1 Clonar el repositorio
```bash
git clone <repositorio>
cd NexoPostal
```

### 1.2 Copiar archivo de variables de entorno
```bash
cp .env.example .env
```

### 1.3 Generar clave JWT segura
```bash
# En Linux/macOS/Git Bash:
openssl rand -base64 64 | tr -d '\n'

# Copia la clave generada y reemplaza JWT_SECRET_KEY en .env
```

### 1.4 Generar certificados SSL (desarrollo)
```bash
mkdir -p nginx/certs
openssl genrsa -out nginx/certs/nexopostal.key 2048
openssl req -new -x509 -key nginx/certs/nexopostal.key \
    -out nginx/certs/nexopostal.crt \
    -days 365 \
    -subj "/C=ES/ST=Madrid/L=Madrid/O=NexoPostal/CN=nexopostal.es"
```

---

## Paso 2: Iniciar en Desarrollo Local

```bash
# Iniciar todos los servicios
docker-compose up -d

# Ver estado
docker-compose ps

# Ver logs
docker-compose logs -f
```

**Servicios disponibles:**
| Servicio | URL |
|----------|-----|
| Clientes (Frontend) | http://localhost |
| Intranet | http://localhost:4202 |
| Driver | http://localhost:4201 |
| API Gateway | http://localhost:8080 |

**Bases de datos:**
| Servicio | Puerto |
|----------|--------|
| Auth | localhost:15432 |
| Ciudadano | localhost:15433 |
| Intranet | localhost:15434 |
| Reparto | localhost:15435 |

---

## Paso 3: Configurar para Producción

### 3.1 Editar `.env` para producción

```bash
cp .env.example .env
nano .env
```

### Variables esenciales para producción:

```bash
# Entorno
ASPNETCORE_ENVIRONMENT=Production

# JWT (OBLIGATORIO - genera una clave segura)
JWT_SECRET_KEY=<clave_generada_con_openssl>

# Dominios reales
DOMINIO_CLIENTES=nexopostal.es
DOMINIO_INTRANET=intranet.nexopostal.es
DOMINIO_DRIVER=driver.nexopostal.es

# Bases de datos (si usas servicio externo)
POSTGRES_AUTH_HOST=<servidor-postgres>
POSTGRES_AUTH_PASSWORD=<contraseña_segura>

# Stripe (prod)
STRIPE_SECRET_KEY=sk_live_...
STRIPE_WEBHOOK_SECRET=whsec_...

# Email profissional
SMTP_HOST=smtp.proveedor.com
SMTP_PORT=587
SMTP_USERNAME=tu@email.com
SMTP_PASSWORD=<contraseña>
```

### 3.2 Obtener certificados SSL válidos

**Opción A: Let's Encrypt (gratis)**
```bash
# Usar Certbot
certbot --nginx -d nexopostal.es -d intranet.nexopostal.es -d driver.nexopostal.es
```

**Opción B: Comprar certificado (Recomendado para producción)**
- Comodo/Symantec
- DigiCert
- GlobalSign

### 3.3 Usar docker-compose de producción

```bash
# Usar el archivo de producción
docker-compose -f docker-compose.production.yml up -d
```

---

## Paso 4: Proveedores Cloud Gratuitos/Low-Cost

### Opción A: Railway (Recomendado)

1.注册 railway.app
2. Conectar tu repositorio GitHub
3. Crear servicios:
   - PostgreSQL x4
   - Docker container

```bash
# railway.yml de ejemplo
services:
  web:
    build: .
    ports:
      - 80:80
      - 443:443
    env:
      - POSTGRES_AUTH_HOST=${{ secrets.PG_AUTH_HOST }}
```

### Opción B: DigitalOcean

1. Crear Droplet (Ubuntu)
2. Instalar Docker

```bash
# En el droplet
curl -fsSL https://get.docker.com | sh
usermod -aG docker $USER

# Subir archivos
rsync -avz --exclude '.git' ./ nexopostal:/opt/nexopostal/
ssh nexopostal "cd /opt/nexopostal && docker-compose up -d"
```

### Opción C: Hetzner Cloud ( muy barato)

- Servidores desde 4€/mes
- Docker preinstalado disponible

### Opción D: Oracle Cloud (Gratis)

- Always Free Tier: 2 VMs, 2 DBs, 1 TB transfer
- Perfecto para pruebas de producción

```bash
# Configuración de VM
# 1VM ARM + 2 VMs AMD (para microservicios)
```

---

## Paso 5: Configurar DNS

### Registros DNS necesarios:

| Tipo | Nombre | Valor |
|------|--------|-------|
| A | nexopostal.com | <IP-SERVIDOR> |
| A | www.nexopostal.com | <IP-SERVIDOR> |
| A | intranet.nexopostal.com | <IP-SERVIDOR> |
| A | driver.nexopostal.com | <IP-SERVIDOR> |

### Si usas Cloudflare:
- Configurar Proxy SSL en "Flexible"
- Reglas de página para fuerza HTTPS

---

## Paso 6: Verificar Producción

```bash
# Ver contenedores
docker-compose ps

# Ver logs
docker-compose logs -f --tail=50

# Ver recursos
docker stats
```

### Health checks:
```bash
# Verificar que todos los servicios responden
curl -I http://localhost/api/auth
curl -I http://localhost/api/envios/track/TEST
```

---

## Comandos Útiles

```bash
# Reiniciar todos los servicios
docker-compose restart

# Actualizar y construir
docker-compose build --no-cache
docker-compose up -d --force-recreate

# Ver logs de un servicio específico
docker-compose logs -f modulo-ciudadano

# Backup de bases de datos
docker-compose exec postgres-auth pg_dump -U postgres nexopostal_auth > backup_auth.sql

# Restaurar base de datos
docker-compose exec -T postgres-auth psql -U postgres nexopostal_auth < backup_auth.sql

# Eliminar todo (¡CUIDADO!)
docker-compose down -v
```

---

## Problemas Comunes

### 1. Puertos en uso
```bash
# Ver qué proceso usa el puerto 80
netstat -tlnp | grep :80

# Cambiar puerto en docker-compose.yml
```

### 2.Error de conexión a PostgreSQL
```bash
# Ver logs de PostgreSQL
docker-compose logs postgres-auth

# Verificar que el contenedor está corriendo
docker-compose ps
```

### 3. Error de certificados SSL
```bash
# Regenerar certificados
openssl req -new -x509 -key nginx/certs/nexopostal.key -out nginx/certs/nexopostal.crt -days 365
```

---

## Seguridad en Producción

1. ⚠️ **Cambiar todas las contraseñas**
2. ⚠️ **Usar certificados SSL válidos**
3. ⚠️ **Configurar firewall** (solo puertos 80, 443)
4. ⚠️ **Habilitar HTTPS redirection**
5. ⚠️ **No exponer puertos de bases de datos** (solo en red interna)

---

## Costes Estimados

| Servicio | Precio |
|----------|--------|
| VPS básico (Hetzner/DigitalOcean) | 4-5€/mes |
| Dominio (.es) | 10€/año |
| Certificado SSL (Let's Encrypt) | Gratis |
| PostgreSQL (gestionado) | 0-20€/mes |
| TOTAL | ~15-25€/mes |

---

## Próximos Pasos

1. [ ] Configurar dominio DNS
2. [ ] Obtener certificados SSL válidos
3. [ ] Configurar backup automático
4. [ ] Configurar monitorización
5. [ ] Configurar CI/CD

---

*Documento actualizado: 2026*