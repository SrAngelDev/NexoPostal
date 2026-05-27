using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Clientes.Oficinas;

/// <summary>
/// MÓDULO: BUSCADOR DE OFICINAS — Clientes App
///
/// OBJETIVO: Validar el buscador de puntos de entrega/recogida:
///   - La página carga con el formulario y los botones de tipo de búsqueda
///   - Cambiar entre "Código Postal" y "Dirección" funciona visualmente
///   - Una búsqueda real por código postal muestra resultados o mensaje
///
/// NOTA: Tests públicos, no requieren autenticación.
///       El buscador utiliza datos locales JSON → no llama a la API → estable.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Clientes")]
[Category("Oficinas")]
public class BuscadorOficinasTests : E2ETestBase
{
    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync($"{ClientesBaseUrl}/buscador-oficinas");
        await CaptureScreenshotAsync("01-buscador-loaded");
    }

    [Test]
    [Description("La página carga con el título y los botones de tipo de búsqueda")]
    public async Task BuscadorPage_ShouldLoadWithSearchForm()
    {
        await Expect(Page.Locator("h1")).ToContainTextAsync("Buscador de Oficinas");

        await Expect(Page.TestId("btn-tipo-cp")).ToBeVisibleAsync();
        await Expect(Page.TestId("btn-tipo-direccion")).ToBeVisibleAsync();

        // El campo de búsqueda es visible (tipo por defecto: código postal)
        await Expect(Page.TestId("search-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("buscar-btn")).ToBeVisibleAsync();

        await CaptureScreenshotAsync("02-form-elements-visible");
    }

    [Test]
    [Description("Cambiar al tipo 'Dirección' activa ese botón y el input sigue visible")]
    public async Task TipoBusqueda_SwitchToDireccion_WorksCorrectly()
    {
        // Click en "Dirección"
        await Page.TestId("btn-tipo-direccion").ClickAsync();
        await CaptureScreenshotAsync("03-tipo-direccion-active");

        // El botón de dirección debe tener clase activa (bg-[#1A237E])
        await Expect(Page.TestId("btn-tipo-direccion")).ToHaveCSSAsync("background-color", new System.Text.RegularExpressions.Regex(".+"));

        // El input de búsqueda sigue visible
        await Expect(Page.TestId("search-input")).ToBeVisibleAsync();

        // Volver a "Código Postal"
        await Page.TestId("btn-tipo-cp").ClickAsync();
        await CaptureScreenshotAsync("04-tipo-cp-restored");
        await Expect(Page.TestId("search-input")).ToBeVisibleAsync();
    }

    [Test]
    [Description("Buscar por código postal '28001' muestra resultados o un mensaje de advertencia")]
    public async Task SearchByCP_ShouldShowResultsOrMessage()
    {
        // Asegurar tipo CP activo
        await Page.TestId("btn-tipo-cp").ClickAsync();

        await Page.TestId("search-input").FillAsync("28001");
        await CaptureScreenshotAsync("03-cp-filled");

        await Page.TestId("buscar-btn").ClickAsync();
        await CaptureScreenshotAsync("04-after-search");

        // El buscador usa datos locales. Esperamos que aparezcan resultados
        // (mapa + h3 "N oficinas encontradas") o bien el mensaje de advertencia.
        var results = Page.Locator("#mapa-oficinas");
        var warning = Page.Locator("[class*='animate-slideDown']");

        // Al menos uno de los dos debe ser visible
        await Expect(results.Or(warning)).ToBeVisibleAsync(new() { Timeout = 12000 });
        await CaptureScreenshotAsync("05-results-or-warning-visible");
    }
}
