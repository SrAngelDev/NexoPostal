using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Clientes.Calculadora;

/// <summary>
/// MÓDULO: CALCULADORA DE TARIFAS — Clientes App
///
/// OBJETIVO: Validar la calculadora de precios pública:
///   - La página carga con todos los campos del formulario
///   - Hacer click en "Calcular" sin datos muestra un aviso de validación
///   - Con datos válidos se obtienen los resultados de tarifa
///   - El botón "Limpiar" resetea el formulario
///
/// NOTA: Tests públicos, no requieren autenticación.
///       El buscador usa datos locales; la calculadora llama al microservicio de tarifas.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Clientes")]
[Category("Calculadora")]
public class CalculadoraTarifasTests : E2ETestBase
{
    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync($"{ClientesBaseUrl}/calculadora-tarifas");
        await CaptureScreenshotAsync("01-calculadora-loaded");
    }

    [Test]
    [Description("La página carga con el formulario completo visible")]
    public async Task CalculadoraPage_ShouldLoadWithForm()
    {
        await Expect(Page.Locator("h1")).ToContainTextAsync("Calculadora de Tarifas");

        await Expect(Page.TestId("cp-origen-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("cp-destino-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("peso-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("largo-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("ancho-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("alto-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("calcular-btn")).ToBeVisibleAsync();
        await Expect(Page.TestId("limpiar-btn")).ToBeVisibleAsync();

        await CaptureScreenshotAsync("02-form-elements-visible");
    }

    [Test]
    [Description("Click en 'Calcular' con campos vacíos muestra notificación de aviso")]
    public async Task CalcularTarifa_EmptyFields_ShouldShowNotification()
    {
        await Expect(Page.TestId("calcular-btn")).ToBeVisibleAsync(new() { Timeout = 5000 });
        await Page.TestId("calcular-btn").ClickAsync();
        await CaptureScreenshotAsync("03-empty-calculate-click");

        // El NotificacionService inyecta un div con role="alert" en el DOM
        var alerta = Page.Locator("[role='alert']").First;
        await Expect(alerta).ToBeVisibleAsync(new() { Timeout = 8000 });

        await CaptureScreenshotAsync("04-validation-notification-visible");
    }

    [Test]
    [Description("Con datos válidos el cálculo devuelve resultados de tarifa")]
    public async Task CalcularTarifa_ValidData_ShouldShowResults()
    {
        await Page.TestId("cp-origen-input").FillAsync("28001");
        await Page.TestId("cp-destino-input").FillAsync("08001");
        await Page.TestId("peso-input").FillAsync("1");
        await Page.TestId("largo-input").FillAsync("30");
        await Page.TestId("ancho-input").FillAsync("20");
        await Page.TestId("alto-input").FillAsync("15");
        await CaptureScreenshotAsync("03-form-filled");

        await Page.TestId("calcular-btn").ClickAsync();
        await CaptureScreenshotAsync("04-after-calculate");

        // El contenedor de resultados aparece cuando tarifaCalculada() no es null
        var resultado = Page.TestId("tarifa-resultado");
        await Expect(resultado).ToBeVisibleAsync(new() { Timeout = 15000 });
        await CaptureScreenshotAsync("05-results-visible");
    }

    [Test]
    [Description("El botón 'Limpiar' vacía todos los inputs y oculta resultados")]
    public async Task LimpiarBtn_ShouldClearAllInputs()
    {
        // Rellenar los campos
        await Page.TestId("cp-origen-input").FillAsync("28001");
        await Page.TestId("cp-destino-input").FillAsync("08001");
        await Page.TestId("peso-input").FillAsync("2");
        await Page.TestId("largo-input").FillAsync("30");
        await Page.TestId("ancho-input").FillAsync("20");
        await Page.TestId("alto-input").FillAsync("15");
        await CaptureScreenshotAsync("03-form-filled-before-clear");

        // Limpiar
        await Page.TestId("limpiar-btn").ClickAsync();
        await CaptureScreenshotAsync("04-after-clear");

        // Los inputs deben estar vacíos
        await Expect(Page.TestId("cp-origen-input")).ToHaveValueAsync("");
        await Expect(Page.TestId("cp-destino-input")).ToHaveValueAsync("");
        // El bloque de resultados no debe estar visible
        await Expect(Page.TestId("tarifa-resultado")).Not.ToBeVisibleAsync();
    }
}
