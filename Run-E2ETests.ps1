<#
.SYNOPSIS
    Ejecuta los tests E2E de NexoPostal contra las tres apps Angular.

.DESCRIPTION
    Levanta el stack completo con Docker Compose, espera a que las tres apps
    estén disponibles y ejecuta los tests Playwright+NUnit del proyecto
    Nexopostal.Tests.E2E. Genera capturas de pantalla y vídeos en el directorio
    de salida del proyecto de tests.

.PARAMETER Build
    Reconstruye las imágenes Docker antes de levantar los contenedores.

.PARAMETER StopAfter
    Para y elimina los contenedores al finalizar (docker compose down).

.PARAMETER Filter
    Filtro de tests de NUnit (ej: "Category=Auth", "Category=Driver", "Category=Clientes").
    Si se omite, se ejecutan todos los tests E2E.

.PARAMETER Headful
    Ejecuta el navegador en modo visible (no headless). Útil para depurar.

.PARAMETER InstallBrowsers
    Instala / actualiza los navegadores de Playwright antes de lanzar los tests.

.EXAMPLE
    .\Run-E2ETests.ps1
    # Levanta contenedores (sin rebuild) y ejecuta todos los tests.

.EXAMPLE
    .\Run-E2ETests.ps1 -Build -StopAfter
    # Rebuild completo, tests, y para los contenedores al acabar.

.EXAMPLE
    .\Run-E2ETests.ps1 -Filter "Category=Auth"
    # Solo los tests de autenticación.

.EXAMPLE
    .\Run-E2ETests.ps1 -Headful -Filter "Category=Driver"
    # Tests de driver-app con navegador visible.
#>

[CmdletBinding()]
param(
    [switch]$Build,
    [switch]$StopAfter,
    [string]$Filter = "",
    [switch]$Headful,
    [switch]$InstallBrowsers
)

$ErrorActionPreference = "Stop"

# ── Rutas ──────────────────────────────────────────────────────────────────────
$ScriptRoot    = $PSScriptRoot
$E2EProjectDir = Join-Path $ScriptRoot "microservicios\Nexopostal\Nexopostal.Tests.E2E"
$E2EProject    = Join-Path $E2EProjectDir "Nexopostal.Tests.E2E.csproj"

# ── URLs de las tres apps ──────────────────────────────────────────────────────
$ClientesUrl  = "http://localhost:80"
$IntranetUrl  = "http://localhost:8202"
$DriverUrl    = "http://localhost:8201"

# ── Colores / helpers de salida ────────────────────────────────────────────────
function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host ("=" * 55) -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Yellow
    Write-Host ("=" * 55) -ForegroundColor Cyan
}

function Write-Step  { param([string]$Msg) Write-Host "[PASO]  $Msg" -ForegroundColor Green  }
function Write-Info  { param([string]$Msg) Write-Host "[INFO]  $Msg" -ForegroundColor White  }
function Write-Warn  { param([string]$Msg) Write-Host "[AVISO] $Msg" -ForegroundColor Yellow }
function Write-Err   { param([string]$Msg) Write-Host "[ERROR] $Msg" -ForegroundColor Red    }

# ── Espera hasta que una URL devuelva HTTP 200 ─────────────────────────────────
function Wait-ForUrl {
    param(
        [string]$Url,
        [string]$Name,
        [int]$TimeoutSeconds = 120
    )
    Write-Step "Esperando $Name ($Url)..."
    $elapsed = 0
    while ($elapsed -lt $TimeoutSeconds) {
        try {
            $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3 -ErrorAction SilentlyContinue
            if ($resp.StatusCode -lt 500) {
                Write-Step "$Name disponible (HTTP $($resp.StatusCode)) ✓"
                return $true
            }
        } catch { }
        Start-Sleep -Seconds 3
        $elapsed += 3
        Write-Host "." -NoNewline -ForegroundColor DarkGray
    }
    Write-Host ""
    Write-Err "$Name no respondió tras ${TimeoutSeconds}s"
    return $false
}

# ── Verificaciones previas ─────────────────────────────────────────────────────
Write-Header "NexoPostal E2E Test Runner"
Write-Info "Directorio raíz : $ScriptRoot"
Write-Info "Proyecto E2E    : $E2EProject"

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Err "Docker no está en el PATH. Instala Docker Desktop y vuelve a intentarlo."
    exit 1
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Err "dotnet CLI no está en el PATH."
    exit 1
}

# ── 1. Levantar Docker Compose ─────────────────────────────────────────────────
Write-Header "1. Levantando stack Docker"

Push-Location $ScriptRoot
try {
    if ($Build) {
        Write-Step "Reconstruyendo imágenes (--build)..."
        docker compose up -d --build
    } else {
        Write-Step "Levantando contenedores existentes..."
        docker compose up -d
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Err "docker compose up falló (exit $LASTEXITCODE)"
        exit 1
    }
} finally {
    Pop-Location
}

# ── 2. Esperar a las tres apps ─────────────────────────────────────────────────
Write-Header "2. Verificando disponibilidad de las apps"

$clientesOk  = Wait-ForUrl -Url $ClientesUrl  -Name "clientes-app"
$intranetOk  = Wait-ForUrl -Url $IntranetUrl  -Name "intranet-app"
$driverOk    = Wait-ForUrl -Url $DriverUrl    -Name "driver-app"

if (-not ($clientesOk -and $intranetOk -and $driverOk)) {
    Write-Err "Una o más apps no están disponibles. Abortando."
    if ($StopAfter) {
        Push-Location $ScriptRoot
        docker compose down
        Pop-Location
    }
    exit 1
}

Write-Step "Las tres apps están activas ✓"

# ── 3. Compilar proyecto E2E ───────────────────────────────────────────────────
Write-Header "3. Compilando proyecto E2E"
Push-Location $E2EProjectDir
try {
    dotnet build $E2EProject -nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Err "La compilación del proyecto E2E falló."
        exit 1
    }
    Write-Step "Compilación correcta ✓"
} finally {
    Pop-Location
}

# ── 4. Instalar navegadores Playwright (opcional) ─────────────────────────────
if ($InstallBrowsers) {
    Write-Header "4. Instalando navegadores Playwright"
    Push-Location $E2EProjectDir
    try {
        $playwrightScript = Join-Path $E2EProjectDir "bin\Debug\net10.0\playwright.ps1"
        if (Test-Path $playwrightScript) {
            pwsh $playwrightScript install --with-deps chromium
        } else {
            Write-Warn "playwright.ps1 no encontrado en $playwrightScript. Omitiendo instalación."
        }
    } finally {
        Pop-Location
    }
}

# ── 5. Configurar variables de entorno para los tests ─────────────────────────
Write-Header "5. Ejecutando tests E2E"

$env:E2E_CLIENTES_URL = $ClientesUrl
$env:E2E_INTRANET_URL = $IntranetUrl
$env:E2E_DRIVER_URL   = $DriverUrl

if ($Headful) {
    $env:HEADED = "1"
    Write-Info "Modo headful activado (navegador visible)"
} else {
    Remove-Item Env:HEADED -ErrorAction SilentlyContinue
}

# Construir argumentos de dotnet test
$TestArgs = @(
    "test", $E2EProject,
    "--no-build",
    "-v", "n",
    "--logger", "console;verbosity=normal"
)

if ($Filter -ne "") {
    $TestArgs += "--filter"
    $TestArgs += $Filter
    Write-Info "Filtro aplicado: $Filter"
} else {
    Write-Info "Ejecutando todos los tests E2E"
}

Push-Location $E2EProjectDir
try {
    # ── Tests clientes-app ─────────────────────────────────────────────────────
    Write-Header "5a. Tests — Clientes App ($ClientesUrl)"
    $clientesFilter = if ($Filter) { "$Filter&Category=Clientes" } else { "Category=Clientes" }
    dotnet test $E2EProject --no-build -v n --filter $clientesFilter `
        --logger "console;verbosity=normal"
    $ResultClientes = $LASTEXITCODE

    # ── Tests driver-app ───────────────────────────────────────────────────────
    Write-Header "5b. Tests — Driver App ($DriverUrl)"
    $driverFilter = if ($Filter) { "$Filter&Category=Driver" } else { "Category=Driver" }
    dotnet test $E2EProject --no-build -v n --filter $driverFilter `
        --logger "console;verbosity=normal"
    $ResultDriver = $LASTEXITCODE

    # ── Tests intranet-app ─────────────────────────────────────────────────────
    Write-Header "5c. Tests — Intranet App ($IntranetUrl)"
    $intranetFilter = if ($Filter) { "$Filter&Category=Intranet" } else { "Category=Intranet" }
    dotnet test $E2EProject --no-build -v n --filter $intranetFilter `
        --logger "console;verbosity=normal"
    $ResultIntranet = $LASTEXITCODE

} finally {
    Pop-Location
    Remove-Item Env:HEADED -ErrorAction SilentlyContinue
}

# ── 6. Parar contenedores si se pidió ─────────────────────────────────────────
if ($StopAfter) {
    Write-Header "6. Parando contenedores"
    Push-Location $ScriptRoot
    docker compose down
    Pop-Location
    Write-Step "Contenedores detenidos ✓"
}

# ── 7. Resumen ─────────────────────────────────────────────────────────────────
Write-Header "Resumen de resultados"

$global = @()
$items = @(
    @{ Name = "clientes-app"; Result = $ResultClientes  },
    @{ Name = "driver-app  "; Result = $ResultDriver    },
    @{ Name = "intranet-app"; Result = $ResultIntranet  }
)

foreach ($item in $items) {
    $ok    = $item.Result -eq 0
    $label = if ($ok) { "✓ OK  " } else { "✗ FAIL" }
    $color = if ($ok) { "Green" } else { "Red"   }
    Write-Host "  $($item.Name) : $label" -ForegroundColor $color
    if (-not $ok) { $global += $item.Name }
}

Write-Host ""

$screenshotsDir = Join-Path $E2EProjectDir "TestScreenshots"
$videosDir      = Join-Path $E2EProjectDir "TestVideos"
if (Test-Path $screenshotsDir) { Write-Info "Capturas  : $screenshotsDir" }
if (Test-Path $videosDir)      { Write-Info "Vídeos    : $videosDir" }

Write-Host ""

if ($global.Count -gt 0) {
    Write-Err "Tests fallidos en: $($global -join ', ')"
    Write-Host ""
    exit 1
}

Write-Host "  ¡Todos los tests E2E pasaron!" -ForegroundColor Green
Write-Host ""
exit 0
