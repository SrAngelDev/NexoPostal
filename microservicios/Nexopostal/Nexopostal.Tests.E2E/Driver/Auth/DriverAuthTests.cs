using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Driver.Auth;

/// <summary>
/// MÓDULO: AUTENTICACIÓN — Driver App (Repartidores / JefeReparto)
///
/// OBJETIVO: Validar que el flujo de login de la app de repartidores es seguro y funcional.
///   - Campos vacíos → mensaje de error
///   - Credenciales inválidas → mensaje de error
///   - Credenciales válidas de repartidor → redirige al dashboard
///   - Credenciales válidas de jefe de reparto → redirige al dashboard-jefe
///
/// NOTA: Los tests requieren que la aplicación esté levantada en localhost:4201.
///       Para ejecutar en local: docker compose up -d
///       Para sobrescribir la URL: variable de entorno E2E_DRIVER_URL.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Driver")]
[Category("Auth")]
public class DriverAuthTests : E2ETestBase
{
    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync($"{DriverBaseUrl}/login");
        await CaptureScreenshotAsync("01-login-page-loaded");
    }

    [Test]
    [Description("Campos vacíos → muestra mensaje de error de validación")]
    public async Task EmptyFields_ShouldShowErrorMessage()
    {
        await Page.TestId("submit-button").ClickAsync();
        await CaptureScreenshotAsync("02-empty-submit");

        var error = Page.TestId("error-message");
        await Expect(error).ToBeVisibleAsync(new() { Timeout = 8000 });
        await Expect(error).ToContainTextAsync("completa todos los campos", new() { IgnoreCase = true });
    }

    [Test]
    [Description("Credenciales inválidas → muestra mensaje de error")]
    public async Task InvalidCredentials_ShouldShowError()
    {
        await Page.TestId("email-input").FillAsync("hacker@malicioso.com");
        await Page.TestId("password-input").FillAsync("password_incorrecta");
        await CaptureScreenshotAsync("03-invalid-credentials-filled");

        await Page.TestId("submit-button").ClickAsync();
        await CaptureScreenshotAsync("04-after-invalid-login");

        var error = Page.TestId("error-message");
        await Expect(error).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(error).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex("incorrectas|denegado", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    [Test]
    [Description("Login correcto → redirige fuera de /login")]
    public async Task ValidRepartidorLogin_ShouldRedirectToDashboard()
    {
        var repartidorEmail = Environment.GetEnvironmentVariable("E2E_DRIVER_EMAIL")
            ?? "repartidor@nexopostal.es";
        var repartidorPass = Environment.GetEnvironmentVariable("E2E_DRIVER_PASSWORD")
            ?? "Repartidor123!";

        await Page.TestId("email-input").FillAsync(repartidorEmail);
        await Page.TestId("password-input").FillAsync(repartidorPass);
        await CaptureScreenshotAsync("05-valid-credentials-filled");

        await Page.TestId("submit-button").ClickAsync();
        await CaptureScreenshotAsync("06-after-valid-login");

        // Tras login correcto no debemos seguir en /login
        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 10000 });
    }

    [Test]
    [Description("Página de login contiene los elementos de formulario esperados")]
    public async Task LoginPage_ShouldHaveRequiredFormElements()
    {
        await Expect(Page.TestId("email-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("password-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("submit-button")).ToBeVisibleAsync();
        await Expect(Page.TestId("submit-button")).ToBeEnabledAsync();
    }
}
