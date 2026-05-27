<#
.SYNOPSIS
    Orquesta los tres niveles de tests de NexoPostal: unitarios, integración y E2E.

.DESCRIPTION
    Ejecuta la suite completa en el orden correcto:
      1. Tests unitarios .NET  (xUnit + Moq, sin dependencias externas)
      2. Tests de integración .NET (xUnit + TestContainers → PostgreSQL efímero)
      3. Tests E2E (Playwright + NUnit, requiere stack Docker levantado)

    Con -Coverage activo, Coverlet recoge datos y ReportGenerator genera un
    informe HTML en TestResults\CoverageReport\. Con -OpenReport se abre
    automáticamente en el navegador al finalizar.

    Si no se pasa ningún switch, ejecuta todos los niveles.
    El script devuelve exit 0 si todo pasa, exit 1 si algún nivel falla.

.PARAMETER Unit
    Ejecuta solo los tests unitarios .NET (xUnit + Moq).

.PARAMETER Integration
    Ejecuta solo los tests de integración .NET. Requiere Docker (TestContainers).

.PARAMETER E2E
    Ejecuta solo los tests E2E (delega en Run-E2ETests.ps1).

.PARAMETER Coverage
    Activa la recogida de cobertura de código con Coverlet (XPlat Code Coverage).
    Genera un informe HTML en TestResults\CoverageReport\ al finalizar los tests.
    Requiere que dotnet-reportgenerator-globaltool esté instalado (se instala si no existe).

.PARAMETER OpenReport
    Abre automáticamente el informe HTML de cobertura en el navegador al terminar.
    Implica -Coverage.

.PARAMETER Build
    (E2E) Reconstruye imágenes Docker antes de levantar el stack.

.PARAMETER StopAfter
    (E2E) Ejecuta docker compose down al finalizar los tests E2E.

.PARAMETER E2EFilter
    (E2E) Filtro NUnit (ej: "Category=Auth", "Category=Driver").

.PARAMETER Headful
    (E2E) Ejecuta el navegador en modo visible para ver los tests en tiempo real.
    Implica ejecución secuencial y SlowMo (ver -SlowMo). Activo por defecto al lanzar sin parámetros.

.PARAMETER SlowMo
    (E2E) Milisegundos de pausa entre acciones en modo headful. Por defecto 500 ms.

.PARAMETER InstallBrowsers
    (E2E) Instala/actualiza los navegadores de Playwright antes de ejecutar.

.EXAMPLE
    .\Run-AllTests.ps1
    # Todo: unitarios + integración + E2E (navegador visible) + cobertura + abre informe.

.EXAMPLE
    .\Run-AllTests.ps1 -Unit
    # Solo unitarios .NET (sin cobertura ni E2E).

.EXAMPLE
    .\Run-AllTests.ps1 -Unit -Coverage
    # Unitarios .NET con informe de cobertura.

.EXAMPLE
    .\Run-AllTests.ps1 -Unit -Integration -Coverage -OpenReport
    # Tests .NET completos con cobertura y apertura automática del informe.

.EXAMPLE
    .\Run-AllTests.ps1 -Integration
    # Solo tests de integración (necesita Docker en PATH).

.EXAMPLE
    .\Run-AllTests.ps1 -Unit -Integration
    # Tests .NET completos (unitarios + integración).

.EXAMPLE
    .\Run-AllTests.ps1 -E2E -Build -StopAfter -E2EFilter "Category=Auth"
    # Solo E2E de Auth, con rebuild Docker y limpieza posterior.
#>

[CmdletBinding()]
param(
    [switch]$Unit,
    [switch]$Integration,
    [switch]$E2E,
    [switch]$Coverage,
    [switch]$OpenReport,
    [switch]$Build,
    [switch]$StopAfter,
    [string]$E2EFilter       = "",
    [switch]$InstallBrowsers,
    [switch]$Headful,
    [int]$SlowMo             = 500
)

# -OpenReport implica -Coverage
if ($OpenReport.IsPresent) { $Coverage = [switch]$true }

$ErrorActionPreference = "Stop"

# ── Rutas ──────────────────────────────────────────────────────────────────────
$ScriptRoot        = $PSScriptRoot
$TestsProjectDir   = Join-Path $ScriptRoot "microservicios\Nexopostal\Nexopostal.Tests"
$TestsProject      = Join-Path $TestsProjectDir "Nexopostal.Tests.csproj"
$RunSettings       = Join-Path $TestsProjectDir "coverage.runsettings"
$TestResultsDir    = Join-Path $TestsProjectDir "TestResults"
$CoverageReportDir = Join-Path $TestResultsDir  "CoverageReport"
$E2EScript         = Join-Path $ScriptRoot "Run-E2ETests.ps1"

# ── ¿Qué ejecutar? ─────────────────────────────────────────────────────────────
$runAll         = -not $Unit.IsPresent -and -not $Integration.IsPresent -and -not $E2E.IsPresent
$RunUnit        = $Unit.IsPresent        -or $runAll
$RunIntegration = $Integration.IsPresent -or $runAll
$RunE2E         = $E2E.IsPresent         -or $runAll

# Sin parámetros → activar cobertura, apertura automática del informe y E2E en modo visible
if ($runAll) {
    if (-not $Coverage.IsPresent  -and -not $OpenReport.IsPresent) { $Coverage   = [switch]$true }
    if (-not $OpenReport.IsPresent)                                 { $OpenReport = [switch]$true }
    if (-not $Headful.IsPresent)                                    { $Headful    = [switch]$true }
}

# ── Helpers de salida (mismo estilo que Run-E2ETests.ps1) ─────────────────────
function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Yellow
    Write-Host ("=" * 60) -ForegroundColor Cyan
}
function Write-Step { param([string]$Msg) Write-Host "[PASO]  $Msg" -ForegroundColor Green  }
function Write-Info { param([string]$Msg) Write-Host "[INFO]  $Msg" -ForegroundColor White  }
function Write-Warn { param([string]$Msg) Write-Host "[AVISO] $Msg" -ForegroundColor Yellow }
function Write-Err  { param([string]$Msg) Write-Host "[ERROR] $Msg" -ForegroundColor Red    }
function Write-Pass { param([string]$Msg) Write-Host "[OK]    $Msg" -ForegroundColor Green  }
function Write-Fail { param([string]$Msg) Write-Host "[FAIL]  $Msg" -ForegroundColor Red    }

# ── Seguimiento de resultados ──────────────────────────────────────────────────
$Results = [System.Collections.Generic.List[PSCustomObject]]::new()
function Add-Result {
    param([string]$Name, [bool]$Passed)
    $Results.Add([PSCustomObject]@{ Name = $Name; Passed = $Passed })
}

# ── Verificaciones previas ─────────────────────────────────────────────────────
Write-Header "NexoPostal — Test Runner Completo"
Write-Info "Raíz del proyecto : $ScriptRoot"
Write-Info "Niveles activos   : $(if($RunUnit){'Unitarios '}else{''})$(if($RunIntegration){'Integración '}else{''})$(if($RunE2E){'E2E'}else{''})"
Write-Info "Cobertura         : $(if($Coverage){'SÍ (Coverlet + ReportGenerator)'}else{'No'})"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Err "dotnet CLI no está en el PATH. Instálalo y vuelve a intentarlo."
    exit 1
}

# ── Preparar directorio de resultados ──────────────────────────────────────────
if ($Coverage) {
    if (Test-Path $TestResultsDir) {
        Write-Step "Limpiando resultados anteriores en TestResults..."
        Remove-Item $TestResultsDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $TestResultsDir -Force | Out-Null
}

# ── Argumentos extra de cobertura para dotnet test ─────────────────────────────
$CoverageArgs = @()
if ($Coverage) {
    $CoverageArgs = @(
        "--collect:XPlat Code Coverage"
        "--settings", $RunSettings
        "--results-directory", $TestResultsDir
    )
}

# ══════════════════════════════════════════════════════════════════════════════
#  1. TESTS UNITARIOS .NET
# ══════════════════════════════════════════════════════════════════════════════
if ($RunUnit) {
    Write-Header "1. Tests Unitarios .NET (xUnit + Moq)"
    Write-Info "Proyecto : $TestsProject"
    Write-Info "Filtro   : excluye clases cuyo nombre contiene 'IntegrationTests'"

    $unitPassed = $false
    try {
        dotnet test $TestsProject `
            --filter "FullyQualifiedName!~IntegrationTests" `
            --nologo --verbosity normal @CoverageArgs
        if ($LASTEXITCODE -eq 0) {
            $unitPassed = $true
            Write-Pass "Tests unitarios .NET superados"
        } else {
            Write-Fail "Tests unitarios .NET fallaron (exit $LASTEXITCODE)"
        }
    } catch {
        Write-Fail "Excepción ejecutando tests unitarios: $_"
    }
    Add-Result "Unitarios .NET" $unitPassed
}

# ══════════════════════════════════════════════════════════════════════════════
#  2. TESTS DE INTEGRACIÓN .NET  (TestContainers → PostgreSQL efímero)
# ══════════════════════════════════════════════════════════════════════════════
if ($RunIntegration) {
    Write-Header "2. Tests de Integración .NET (xUnit + TestContainers)"
    Write-Info "Proyecto : $TestsProject"
    Write-Info "Filtro   : clases cuyo nombre contiene 'IntegrationTests'"
    Write-Warn "Cada factory levanta un contenedor PostgreSQL efímero (Docker requerido)."

    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Err "Docker no está en el PATH. Los tests de integración lo requieren."
        Add-Result "Integración .NET" $false
    } else {
        $integPassed = $false
        try {
            dotnet test $TestsProject `
                --filter "FullyQualifiedName~IntegrationTests" `
                --nologo --verbosity normal @CoverageArgs
            if ($LASTEXITCODE -eq 0) {
                $integPassed = $true
                Write-Pass "Tests de integración .NET superados"
            } else {
                Write-Fail "Tests de integración .NET fallaron (exit $LASTEXITCODE)"
            }
        } catch {
            Write-Fail "Excepción ejecutando tests de integración: $_"
        }
        Add-Result "Integración .NET" $integPassed
    }
}

# ══════════════════════════════════════════════════════════════════════════════
#  4. TESTS E2E  (Playwright + NUnit, stack Docker completo)
# ══════════════════════════════════════════════════════════════════════════════
if ($RunE2E) {
    Write-Header "3. Tests E2E (Playwright — stack Docker completo)"

    if (-not (Test-Path $E2EScript)) {
        Write-Err "Script E2E no encontrado: $E2EScript"
        Add-Result "E2E" $false
    } elseif (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Err "Docker no está en el PATH. Los tests E2E requieren docker compose."
        Add-Result "E2E" $false
    } else {
        # Construir los argumentos a pasar a Run-E2ETests.ps1 (hashtable → splatting tipado)
        $e2eArgs = @{}
        if ($Build)           { $e2eArgs['Build']           = $true       }
        if ($StopAfter)       { $e2eArgs['StopAfter']       = $true       }
        if ($E2EFilter)       { $e2eArgs['Filter']          = $E2EFilter  }
        if ($InstallBrowsers) { $e2eArgs['InstallBrowsers'] = $true       }
        if ($Headful)         { $e2eArgs['Headful']         = $true
                                $e2eArgs['SlowMo']          = $SlowMo     }

        $e2ePassed = $false
        try {
            & $E2EScript @e2eArgs
            if ($LASTEXITCODE -eq 0) {
                $e2ePassed = $true
                Write-Pass "Tests E2E superados"
            } else {
                Write-Fail "Tests E2E fallaron (exit $LASTEXITCODE)"
            }
        } catch {
            Write-Fail "Excepción ejecutando tests E2E: $_"
        }
        Add-Result "E2E" $e2ePassed
    }
}

# ══════════════════════════════════════════════════════════════════════════════
#  INFORME DE COBERTURA  (Coverlet XML → ReportGenerator HTML)
# ══════════════════════════════════════════════════════════════════════════════
if ($Coverage) {
    Write-Header "Generando informe de cobertura"

    # Instalar ReportGenerator si no está disponible como herramienta global
    if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
        Write-Step "Instalando dotnet-reportgenerator-globaltool..."
        dotnet tool install -g dotnet-reportgenerator-globaltool
        if ($LASTEXITCODE -ne 0) {
            Write-Warn "No se pudo instalar ReportGenerator. Se omite la generación del informe."
            $Coverage = [switch]$false
        }
    }

    if ($Coverage) {
        # Buscar todos los coverage.cobertura.xml generados
        $CoverageFiles = Get-ChildItem -Path $TestResultsDir -Recurse -Filter "coverage.cobertura.xml" |`
            Select-Object -ExpandProperty FullName

        if (-not $CoverageFiles) {
            Write-Warn "No se encontraron archivos coverage.cobertura.xml en $TestResultsDir. ¿Se ejecutaron tests con -Coverage?"
        } else {
            $reportsArg = $CoverageFiles -join ";"
            Write-Step "Procesando $($CoverageFiles.Count) archivo(s) de cobertura..."
            Write-Info "Destino : $CoverageReportDir"

            reportgenerator `
                "-reports:$reportsArg" `
                "-targetdir:$CoverageReportDir" `
                "-reporttypes:Html;Cobertura" `
                "-title:NexoPostal Coverage" `
                "-verbosity:Warning"

            if ($LASTEXITCODE -eq 0) {
                Write-Pass "Informe generado en: $CoverageReportDir"

                if ($OpenReport) {
                    $indexHtml = Join-Path $CoverageReportDir "index.html"
                    if (Test-Path $indexHtml) {
                        Write-Step "Abriendo informe en el navegador..."
                        Start-Process $indexHtml
                    } else {
                        Write-Warn "index.html no encontrado en $CoverageReportDir"
                    }
                }
            } else {
                Write-Fail "ReportGenerator falló (exit $LASTEXITCODE)"
            }
        }
    }
}

# ══════════════════════════════════════════════════════════════════════════════
#  RESUMEN FINAL
# ══════════════════════════════════════════════════════════════════════════════
Write-Header "Resumen de resultados"

$totalPassed = 0
$totalFailed  = 0

foreach ($r in $Results) {
    if ($r.Passed) {
        Write-Pass $r.Name
        $totalPassed++
    } else {
        Write-Fail $r.Name
        $totalFailed++
    }
}

Write-Host ""
Write-Info "─────────────────────────────────────────────────────────"
Write-Info "Superados : $totalPassed / $($Results.Count)"
if ($totalFailed -gt 0) {
    Write-Fail "Fallados  : $totalFailed / $($Results.Count)"
    Write-Host ""
    exit 1
} else {
    Write-Pass "Todos los niveles de tests superados ✓"
    Write-Host ""
    exit 0
}
