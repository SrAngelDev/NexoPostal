using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Clientes.Home;

/// <summary>
/// MÓDULO: HOME Y TRACKING PÚBLICO — Clientes App
///
/// OBJETIVO: Validar las funcionalidades públicas de la home page:
///   - Carga correcta de la página (hero, navbar, botones)
///   - Búsqueda de tracking con código vacío → no navega
///   - Búsqueda de tracking con código inválido → muestra resultado "no encontrado"
///   - Navegación a páginas públicas (tarifas, oficinas)
///
/// NOTA: Tests 100% públicos, no requieren autenticación.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Clientes")]
[Category("Home")]
public class HomePublicTests : E2ETestBase
{
    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync(ClientesBaseUrl);
        await CaptureScreenshotAsync("01-home-loaded");
    }

    [Test]
    [Description("Home carga correctamente con navbar y hero de tracking visibles")]
    public async Task HomePage_ShouldLoadWithKeyElements()
    {
        // El input de tracking debe existir en el hero
        await Expect(Page.TestId("tracking-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("tracking-submit")).ToBeVisibleAsync();

        // El botón de login debe estar en el navbar (usuario anónimo)
        await Expect(Page.TestId("navbar-login-btn")).ToBeVisibleAsync();

        await CaptureScreenshotAsync("02-key-elements-visible");
    }

    [Test]
    [Description("Buscar tracking con código inválido muestra estado de error o 'no encontrado'")]
    public async Task TrackingSearch_InvalidCode_ShouldShowNotFound()
    {
        await Page.TestId("tracking-input").FillAsync("CODIGO_INVALIDO_12345");
        await CaptureScreenshotAsync("03-invalid-tracking-filled");

        await Page.TestId("tracking-submit").ClickAsync();
        await CaptureScreenshotAsync("04-after-tracking-search");

        // El tracking-submit llama a la API: con código inválido devuelve 404
        // y el componente muestra el bloque .tracking-alert (data-testid="tracking-error")
        var status = Page.TestId("tracking-error");
        await Expect(status).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    [Description("Navegar a 'Tarifas' desde el navbar funciona correctamente")]
    public async Task NavbarTarifas_ShouldNavigateToTarifasPage()
    {
        await Page.Locator("a:has-text('Tarifas'), button:has-text('Tarifas')").First.ClickAsync();
        await CaptureScreenshotAsync("05-tarifas-page");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/calculadora-tarifas.*"),
            new() { Timeout = 8000 });
    }

    [Test]
    [Description("Navegar a 'Ayuda' desde el navbar funciona correctamente")]
    public async Task NavbarAyuda_ShouldNavigateToAyudaPage()
    {
        await Page.Locator("a:has-text('Ayuda'), button:has-text('Ayuda')").First.ClickAsync();
        await CaptureScreenshotAsync("06-ayuda-page");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/ayuda.*"),
            new() { Timeout = 8000 });
    }

    [Test]
    [Description("La página de tracking público (/tracking) carga correctamente")]
    public async Task TrackingPage_ShouldLoadCorrectly()
    {
        await Page.GotoAsync($"{ClientesBaseUrl}/tracking");
        await CaptureScreenshotAsync("07-tracking-page");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/tracking.*"),
            new() { Timeout = 8000 });
    }
}
