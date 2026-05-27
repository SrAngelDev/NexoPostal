using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Driver.Escaneo;

/// <summary>
/// MÓDULO: PÁGINA DE ESCANEO — Driver App
///
/// OBJETIVO: Validar la página de escaneo de entregas del repartidor:
///   - Acceso sin sesión → redirige a /login (repartidorGuard)
///   - Con sesión de repartidor → la página carga y muestra el escáner
///   - La sección del escáner de código de barras está presente
///
/// NOTA: El componente BarcodeScannerComponent necesita permisos de cámara;
///       en modo headless no activa la cámara real, pero el contenedor es visible.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Driver")]
[Category("Escaneo")]
public class DriverEscaneoTests : E2ETestBase
{
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
        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 10000 });
    }

    [Test]
    [Description("Acceder a /escaneo sin sesión → redirige al login (repartidorGuard)")]
    public async Task EscaneoRoute_Unauthenticated_RedirectsToLogin()
    {
        await Page.GotoAsync($"{DriverBaseUrl}/escaneo");
        await CaptureScreenshotAsync("01-escaneo-unauthenticated");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });
    }

    [Test]
    [Description("Repartidor autenticado → la página de escaneo carga correctamente")]
    public async Task EscaneoPage_ShouldLoadAfterLogin()
    {
        await LoginAsRepartidorAsync();
        await Page.GotoAsync($"{DriverBaseUrl}/escaneo");
        await CaptureScreenshotAsync("02-escaneo-page-loaded");

        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });

        await Expect(Page.TestId("escaneo-page")).ToBeAttachedAsync(new() { Timeout = 8000 });
    }

    [Test]
    [Description("La sección del escáner de código de barras está presente en la página")]
    public async Task EscaneoPage_HasScannerSection()
    {
        await LoginAsRepartidorAsync();
        await Page.GotoAsync($"{DriverBaseUrl}/escaneo");
        await CaptureScreenshotAsync("02-escaneo-checking-scanner");

        var scanner = Page.TestId("scanner-section");
        await Expect(scanner).ToBeAttachedAsync(new() { Timeout = 8000 });
        await CaptureScreenshotAsync("03-scanner-section-present");
    }
}
