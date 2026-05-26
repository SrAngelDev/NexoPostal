# Nexopostal.Tests.E2E

Tests End-to-End de NexoPostal con **Playwright + NUnit**.

## Estructura

```
Nexopostal.Tests.E2E/
├── E2ETestBase.cs                         # Clase base: URLs, screenshots, config navegador
├── Extensions/
│   └── LocatorExtensions.cs               # Helper .TestId(name) para data-testid
├── Clientes/
│   ├── Auth/ClientesAuthTests.cs          # Login modal del portal público
│   └── Home/HomePublicTests.cs            # Home page, tracking, navegación
├── Driver/
│   ├── Auth/DriverAuthTests.cs            # Login de la app de repartidores
│   └── Dashboard/DriverDashboardTests.cs  # Dashboard, guardas, navegación
└── Intranet/
    ├── Auth/IntranetAuthTests.cs          # Login de la intranet
    └── Dashboard/IntranetDashboardTests.cs # Panel admin, guardas, roles
```

## Requisitos

1. Las tres apps Angular levantadas (o vía Docker):
   - `clientes-app` → `http://localhost:80`
   - `intranet-app` → `http://localhost:4202`
   - `driver-app`   → `http://localhost:4201`

2. Playwright instalado (ejecutar **una sola vez** tras compilar):

```powershell
cd microservicios/Nexopostal/Nexopostal.Tests.E2E
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
```

## Variables de entorno (opcionales)

| Variable               | Por defecto                    | Descripción                          |
|------------------------|--------------------------------|--------------------------------------|
| `E2E_CLIENTES_URL`     | `http://localhost:80`          | URL base de clientes-app             |
| `E2E_INTRANET_URL`     | `http://localhost:4202`        | URL base de intranet-app             |
| `E2E_DRIVER_URL`       | `http://localhost:4201`        | URL base de driver-app               |
| `E2E_DRIVER_EMAIL`     | `repartidor@nexopostal.es`     | Email de repartidor para tests       |
| `E2E_DRIVER_PASSWORD`  | `Repartidor1!`                 | Contraseña de repartidor             |
| `E2E_ADMIN_EMAIL`      | `admin@nexopostal.es`          | Email de admin para tests            |
| `E2E_ADMIN_PASSWORD`   | `Admin1234!`                   | Contraseña de admin                  |
| `E2E_OPERARIO_EMAIL`   | `operario@nexopostal.es`       | Email de operario para tests         |
| `E2E_OPERARIO_PASSWORD`| `Operario1!`                   | Contraseña de operario               |
| `E2E_CLIENTE_EMAIL`    | `cliente@nexopostal.es`        | Email de cliente para tests          |
| `E2E_CLIENTE_PASSWORD` | `Cliente1234!`                 | Contraseña de cliente                |

## Ejecución

```powershell
# Todos los E2E
dotnet test Nexopostal.Tests.E2E/Nexopostal.Tests.E2E.csproj

# Solo tests de autenticación
dotnet test --filter "Category=Auth"

# Solo tests de driver-app
dotnet test --filter "Category=Driver"

# Solo tests públicos (sin credenciales)
dotnet test --filter "Category=Home"

# Con navegador visible (headful)
$env:HEADED = "1"
dotnet test Nexopostal.Tests.E2E/Nexopostal.Tests.E2E.csproj
```

## Artefactos generados

Tras ejecutar los tests se crean:
- `TestScreenshots/<NombreTest>/` — PNG por cada paso (`data-testid` del paso)
- `TestVideos/` — Vídeo MP4 de cada test (configurado en `ContextOptions`)

## data-testid añadidos a las apps

### driver-app
| Elemento | data-testid |
|---|---|
| Input email login | `email-input` |
| Input password login | `password-input` |
| Botón submit login | `submit-button` |
| Mensaje de error login | `error-message` |
| Contenedor dashboard | `driver-dashboard` |
| Título dashboard | `dashboard-title` |
| Tarjeta "Ruta activa" | `card-ruta` |
| Tarjeta "Escanear" | `card-escaneo` |

### intranet-app
| Elemento | data-testid |
|---|---|
| Input email login | `email-input` |
| Input password login | `password-input` |
| Botón submit login | `submit-button` |
| Mensaje de error login | `error-message` |
| Contenedor dashboard | `intranet-dashboard` |
| Título dashboard | `dashboard-title` |

### clientes-app
| Elemento | data-testid |
|---|---|
| Botón login en navbar | `navbar-login-btn` |
| Botón registro en navbar | `navbar-register-btn` |
| Input email modal login | `email-input` |
| Input password modal login | `password-input` |
| Botón submit modal login | `submit-button` |
| Mensaje error modal login | `error-message` |
| Input tracking en hero | `tracking-input` |
| Botón localizar tracking | `tracking-submit` |
