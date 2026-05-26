using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Nexopostal.Tests.E2E;

/// <summary>
/// Clase base para todos los tests E2E de NexoPostal.
/// Hereda de PageTest (Playwright NUnit) y proporciona:
/// - URLs base de cada aplicación vía variables de entorno
/// - Captura de pantalla en cada paso relevante
/// - Configuración de idioma y zona horaria española
/// </summary>
public abstract class E2ETestBase : PageTest
{
    // ── URLs de las tres apps ──────────────────────────────────────────────────
    protected string ClientesBaseUrl =>
        Environment.GetEnvironmentVariable("E2E_CLIENTES_URL") ?? "http://localhost:80";

    protected string IntranetBaseUrl =>
        Environment.GetEnvironmentVariable("E2E_INTRANET_URL") ?? "http://localhost:8202";

    protected string DriverBaseUrl =>
        Environment.GetEnvironmentVariable("E2E_DRIVER_URL") ?? "http://localhost:8201";

    // ── Configuración del navegador ────────────────────────────────────────────
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            Locale = "es-ES",
            TimezoneId = "Europe/Madrid",
            RecordVideoDir = "TestVideos",
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        };
    }

    // ── Helper: captura de pantalla por paso ──────────────────────────────────
    protected async Task CaptureScreenshotAsync(string stepName)
    {
        var dir = Path.Combine("TestScreenshots", TestContext.CurrentContext.Test.Name);
        Directory.CreateDirectory(dir);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(dir, $"{stepName}.png"),
            FullPage = true
        });
    }
}
