using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Driver.Ruta;

/// <summary>
/// MÓDULO: PÁGINA DE RUTA — Driver App
///
/// OBJETIVO: Validar el flujo de la página de ruta del repartidor:
///   - Acceso sin sesión → redirige a /login (repartidorGuard)
///   - Con sesión de repartidor → la página carga correctamente
///   - Si no hay ruta asignada → muestra el estado vacío
///   - El navbar muestra el título correcto
///
/// NOTA: El seed de desarrollo no genera rutas de reparto para el repartidor demo,
///       por lo que el estado "sin ruta asignada" es estable en el entorno de test.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Driver")]
[Category("Ruta")]
public class DriverRutaTests : E2ETestBase
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
    [Description("Acceder a /ruta sin sesión → redirige al login (repartidorGuard)")]
    public async Task RutaRoute_Unauthenticated_RedirectsToLogin()
    {
        await Page.GotoAsync($"{DriverBaseUrl}/ruta");
        await CaptureScreenshotAsync("01-ruta-unauthenticated");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });
    }

    [Test]
    [Description("Repartidor autenticado puede acceder a /ruta sin redirección")]
    public async Task RutaPage_ShouldLoadAfterLogin()
    {
        await LoginAsRepartidorAsync();
        await Page.GotoAsync($"{DriverBaseUrl}/ruta");
        await CaptureScreenshotAsync("02-ruta-page-loaded");

        // No debe redirigir a login
        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });

        // El contenedor principal de la ruta debe estar presente
        await Expect(Page.TestId("ruta-page")).ToBeAttachedAsync(new() { Timeout = 8000 });
    }

    [Test]
    [Description("Sin ruta asignada → muestra el estado vacío con mensaje correspondiente")]
    public async Task RutaPage_ShowsEmptyStateWhenNoRoute()
    {
        await LoginAsRepartidorAsync();
        await Page.GotoAsync($"{DriverBaseUrl}/ruta");
        await CaptureScreenshotAsync("03-ruta-checking-empty");

        // El repartidor demo no tiene ruta asignada → aparece la sección empty-card
        var emptyState = Page.TestId("ruta-empty");
        await Expect(emptyState).ToBeVisibleAsync(new() { Timeout = 12000 });
        await Expect(emptyState).ToContainTextAsync("Sin ruta asignada", new() { IgnoreCase = true });

        await CaptureScreenshotAsync("04-ruta-empty-state-visible");
    }

    [Test]
    [Description("El navbar de la página de ruta muestra el título 'Ruta de reparto'")]
    public async Task RutaPage_HasNavbarTitle()
    {
        await LoginAsRepartidorAsync();
        await Page.GotoAsync($"{DriverBaseUrl}/ruta");
        await CaptureScreenshotAsync("02-ruta-with-navbar");

        await Expect(Page.Locator("text=Ruta de reparto")).ToBeVisibleAsync(new() { Timeout = 8000 });
    }
}
