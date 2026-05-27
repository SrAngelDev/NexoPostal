using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Intranet.SeguimientoInterno;

/// <summary>
/// MÓDULO: SEGUIMIENTO INTERNO — Intranet App
///
/// OBJETIVO: Validar la página de seguimiento interno de envíos:
///   - Acceso sin sesión → redirige a /login (authGuard)
///   - Operario autenticado puede acceder y ver la página completa
///   - Las pestañas "Buscar envío" y "Listado de envíos" están presentes
///
/// NOTA: Requieren variables de entorno E2E_OPERARIO_EMAIL / E2E_OPERARIO_PASSWORD.
///       Por defecto usa las credenciales del seed de desarrollo.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Intranet")]
[Category("SeguimientoInterno")]
public class SeguimientoInternoTests : E2ETestBase
{
    private string OperarioEmail =>
        Environment.GetEnvironmentVariable("E2E_OPERARIO_EMAIL") ?? "operario@nexopostal.es";
    private string OperarioPassword =>
        Environment.GetEnvironmentVariable("E2E_OPERARIO_PASSWORD") ?? "Operario123!";

    private async Task LoginAsOperarioAsync()
    {
        await Page.GotoAsync($"{IntranetBaseUrl}/login");
        await Page.TestId("email-input").FillAsync(OperarioEmail);
        await Page.TestId("password-input").FillAsync(OperarioPassword);
        await Page.TestId("submit-button").ClickAsync();
        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 10000 });
    }

    [Test]
    [Description("Acceder a /seguimiento-interno sin sesión → redirige al login (authGuard)")]
    public async Task SeguimientoRoute_Unauthenticated_RedirectsToLogin()
    {
        await Page.GotoAsync($"{IntranetBaseUrl}/seguimiento-interno");
        await CaptureScreenshotAsync("01-seguimiento-unauthenticated");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });
    }

    [Test]
    [Description("Operario autenticado puede ver la página de seguimiento interno")]
    public async Task SeguimientoPage_ShouldLoadAsOperario()
    {
        await LoginAsOperarioAsync();
        await Page.GotoAsync($"{IntranetBaseUrl}/seguimiento-interno");
        await CaptureScreenshotAsync("02-seguimiento-page-loaded");

        // No debe redirigir a login
        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });

        await Expect(Page.TestId("seguimiento-page")).ToBeAttachedAsync(new() { Timeout = 8000 });
    }

    [Test]
    [Description("Las pestañas 'Buscar envío' y 'Listado de envíos' están visibles")]
    public async Task SeguimientoPage_HasSearchTabs()
    {
        await LoginAsOperarioAsync();
        await Page.GotoAsync($"{IntranetBaseUrl}/seguimiento-interno");
        await CaptureScreenshotAsync("02-checking-tabs");

        var tabs = Page.TestId("tabs-section");
        await Expect(tabs).ToBeVisibleAsync(new() { Timeout = 8000 });

        await Expect(tabs.Locator("text=Buscar envío")).ToBeVisibleAsync();
        await Expect(tabs.Locator("text=Listado de envíos")).ToBeVisibleAsync();

        await CaptureScreenshotAsync("03-tabs-visible");
    }
}
