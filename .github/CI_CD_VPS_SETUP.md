# Guía de Configuración CI/CD - Deploy a VPS DigitalOcean

## Paso 1: Crear SSH Deploy Key

En tu **VPS**, ejecutar:

```bash
# Crear usuario específico para deploy (recomendado)
adduser deploy
usermod -aG docker deploy

# O si prefieres usar root:
# (saltar este paso si usas root)
```

### Generar clave SSH para GitHub Actions

```bash
# En tu VPS o local (Linux/macOS/Git Bash)
ssh-keygen -t ed25519 -C "github-actions@nexopostal" -f ~/.ssh/github_actions

# Mostrar la clave pública
cat ~/.ssh/github_actions.pub
```

## Paso 2: Añadir Clave Pública a VPS

```bash
# En tu VPS
cat ~/.ssh/github_actions.pub >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

## Paso 3: Añadir Clave Privada a GitHub Secrets

```bash
# Mostrar la clave privada (para copiar)
cat ~/.ssh/github_actions
```

## Paso 4: Configurar Secrets en GitHub

1. Ve a tu repositorio GitHub
2. Settings → Secrets and variables → Actions
3. Añadir cada secrets:

| Secret Name | Value |
|-------------|-------|
| `VPS_HOST` | Tu IP de DigitalOcean (ej: 164.90.123.45) |
| `VPS_USER` | `deploy` (o `root` si prefieres) |
| `VPS_SSH_PRIVATE_KEY` | Contenido del archivo `~/.ssh/github_actions` |

## Paso 5: Verificar Funcionamiento

```bash
# Test de conexión SSH desde GitHub Actions
# Hará un echo "OK" si la conexión funciona
ssh <VPS_USER>@TU_IP "echo OK"
```

## Solución de Problemas

### Error: "Permission denied (publickey)"
```bash
# Verificar que la clave pública está en authorized_keys
cat ~/.ssh/github_actions.pub >> ~/.ssh/authorized_keys
```

### Error: "Host key verification failed"
```bash
# Añadir manualmente el host
ssh-keyscan -H TU_IP >> ~/.ssh/known_hosts
```

---

## Estructura del Deploy

```
Push a main
   ↓
[Build Backend .NET] → [Build Frontend Angular] → [Docker Build]
   ↓ (si todo OK)
[Deploy to VPS]
   ↓
1. SSH conecta a VPS
2. docker compose down
3. docker compose build --no-cache
4. docker compose up -d
5. Health check
```

---

## Comandos Manuales de Emergencia

Si el CI/CD falla, ejecuta en tu VPS:

```bash
# Ver estado
docker ps

# Ver logs
docker logs nexopostal-modulo-ciudadano

# Reiniciar todo
cd /opt/NexoPostal
docker compose -f docker-compose.production.yml restart

# Rebuild manual
docker compose -f docker-compose.production.yml build --no-cache
docker compose -f docker-compose.production.yml up -d
```