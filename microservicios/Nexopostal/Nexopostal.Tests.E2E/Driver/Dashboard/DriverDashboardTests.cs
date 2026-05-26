using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Driver.Dashboard;

/// <summary>
/// MÓDULO: DASHBOARD — Driver App (Repartidor)
///
/// OBJETIVO: Validar que tras el login el repartidor ve su panel correctamente:
///   - Dashboard tiene las tarjetas de acción esperadas (Ruta activa, Escanear)
///   - Acceder a /ruta sin autenticar redirige a /login (guard)
///   - Acceder a /escaneo sin autenticar redirige a /login (guard)
///
/// PREREQUISITO: Credenciales de repartidor válidas vía variables de entorno:
///   E2E_DRIVER_EMAIL / E2E_DRIVER_PASSWORD
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Driver")]
[Category("Dashboard")]
public class DriverDashboardTests : E2ETestBase
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private string RepartidorEmail =>
        Environment.GetEnvironmentVariable("E2E_DRIVER_EMAIL") ?? "repartidor@nexopostal.es";

    private string RepartidorPassword =>
        Environment.GetEnvironmentVariable("E2E_DRIVER_PASSWORD") ?? "Repartidor123!";

    private async Task LoginAsRepartidorAsync()
    {
        await Page.GotoAsync($"{DriverBaseUrl}/login");
        await Page.TestId("email-input").FillAsync(RepartidorEmail);
        await Page.TestId("password-input").FillAsync(RepartidorPassword);
        await Page.TestId("submit-button").ClickAsync();
        // Esperamos a que salga de /login
        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 10000 });
    }

    // ── Tests de guardas (sin autenticar) ──────────────────────────────────────

    [Test]
    [Description("Acceder a /ruta sin sesión → redirige al login")]
    public async Task ProtectedRoute_Ruta_RedirectsToLoginWhenUnauthenticated()
    {
        await Page.GotoAsync($"{DriverBaseUrl}/ruta");
        await CaptureScreenshotAsync("01-ruta-unauthenticated");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });
    }

    [Test]
    [Description("Acceder a /escaneo sin sesión → redirige al login")]
    public async Task ProtectedRoute_Escaneo_RedirectsToLoginWhenUnauthenticated()
    {
        await Page.GotoAsync($"{DriverBaseUrl}/escaneo");
        await CaptureScreenshotAsync("02-escaneo-unauthenticated");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });
    }

    // ── Tests con login ────────────────────────────────────────────────────────

    [Test]
    [Description("Tras login, el dashboard de repartidor muestra las tarjetas de acción")]
    public async Task AfterLogin_Dashboard_ShouldShowActionCards()
    {
        await LoginAsRepartidorAsync();
        await CaptureScreenshotAsync("03-dashboard-loaded");

        await Expect(Page.TestId("driver-dashboard")).ToBeVisibleAsync(new() { Timeout = 8000 });
        await Expect(Page.TestId("dashboard-title")).ToContainTextAsync("Panel de Reparto", new() { IgnoreCase = true });
        await Expect(Page.TestId("card-ruta")).ToBeVisibleAsync();
        await Expect(Page.TestId("card-escaneo")).ToBeVisibleAsync();
    }

    [Test]
    [Description("Clic en tarjeta 'Ruta activa' navega a /ruta")]
    public async Task ClickRutaCard_ShouldNavigateToRutaPage()
    {
        await LoginAsRepartidorAsync();
        await Expect(Page.TestId("card-ruta")).ToBeVisibleAsync(new() { Timeout = 8000 });

        await Page.TestId("card-ruta").ClickAsync();
        await CaptureScreenshotAsync("04-ruta-page-loaded");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/ruta.*"),
            new() { Timeout = 8000 });
    }

    [Test]
    [Description("Clic en tarjeta 'Escanear paquetes' navega a /escaneo")]
    public async Task ClickEscaneoCard_ShouldNavigateToEscaneoPage()
    {
        await LoginAsRepartidorAsync();
        await Expect(Page.TestId("card-escaneo")).ToBeVisibleAsync(new() { Timeout = 8000 });

        await Page.TestId("card-escaneo").ClickAsync();
        await CaptureScreenshotAsync("05-escaneo-page-loaded");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/escaneo.*"),
            new() { Timeout = 8000 });
    }
}
