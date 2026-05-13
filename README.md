# NexoPostal

## 🚀 Quick Start

### Desarrollo Local
```bash
# Iniciar
docker-compose -f docker-compose.local.yml up -d

# Detener
docker-compose -f docker-compose.local.yml down
```

**URLs:**
- Clientes: http://localhost
- Intranet: http://localhost:4202
- Driver: http://localhost:4201

### Producción (VPS)
```bash
# Solo ejecuta: git push origin master
# CI/CD despliega automáticamente
```

---

## 📁 Estructura de Archivos

| Archivo | Uso |
|---------|-----|
| `docker-compose.local.yml` | Desarrollo local |
| `docker-compose.production.yml` | Producción (VPS) |
| `nginx/nginx.local.conf` | Nginx sin SSL (local) |
| `nginx/nginx.production.conf` | Nginx con SSL (producción) |

---

## 🔧 Comandos Útiles

```bash
# Desarrollo
docker-compose -f docker-compose.local.yml up -d
docker-compose -f docker-compose.local.yml logs -f
docker-compose -f docker-compose.local.yml down

# Producción (manual)
docker-compose -f docker-compose.production.yml up -d
docker-compose -f docker-compose.production.yml logs -f
docker-compose -f docker-compose.production.yml down
```

---

## 🌿 Ramas Git

| Rama | Entorno |
|------|---------|
| `develop` | (no despliega) |
| `master` | Producción (VPS) |

---

## CI/CD

Al hacer push a `master`, GitHub Actions automáticamente:
1. Compila .NET
2. Compila Angular
3. Build Docker
4. Despliega a VPS (DigitalOcean)

---

## 📋 Requisitos

- Docker
- Docker Compose

## 🐳 Docker

Las imágenes se construyen automáticamente desde los Dockerfiles en cada microservicio.
