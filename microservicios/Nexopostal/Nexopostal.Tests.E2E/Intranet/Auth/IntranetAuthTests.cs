using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Intranet.Auth;

/// <summary>
/// MÓDULO: AUTENTICACIÓN — Intranet App (OperarioOficina / OperarioCTA / Supervisor / Admin)
///
/// OBJETIVO: Validar que el flujo de login de la intranet es seguro y funcional.
///   - Campos vacíos → mensaje de error
///   - Credenciales inválidas → mensaje de error
///   - Credenciales de Admin válidas → redirige al panel admin (/admin)
///   - Elementos de formulario presentes y accesibles
///
/// NOTA: Los tests requieren la intranet en localhost:4202.
///       Variable de entorno E2E_INTRANET_URL para sobreescribir.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Intranet")]
[Category("Auth")]
public class IntranetAuthTests : E2ETestBase
{
    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync($"{IntranetBaseUrl}/login");
        await CaptureScreenshotAsync("01-intranet-login-loaded");
    }

    [Test]
    [Description("Campos vacíos → error de validación")]
    public async Task EmptyFields_ShouldShowErrorMessage()
    {
        await Page.TestId("submit-button").ClickAsync();
        await CaptureScreenshotAsync("02-empty-submit");

        var error = Page.TestId("error-message");
        await Expect(error).ToBeVisibleAsync(new() { Timeout = 8000 });
        await Expect(error).ToContainTextAsync("completa todos los campos", new() { IgnoreCase = true });
    }

    [Test]
    [Description("Credenciales inválidas → mensaje de error")]
    public async Task InvalidCredentials_ShouldShowError()
    {
        await Page.TestId("email-input").FillAsync("intruso@malicioso.com");
        await Page.TestId("password-input").FillAsync("password_mala");
        await CaptureScreenshotAsync("03-invalid-credentials");

        await Page.TestId("submit-button").ClickAsync();
        await CaptureScreenshotAsync("04-after-invalid-login");

        var error = Page.TestId("error-message");
        await Expect(error).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(error).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex("incorrectas|denegado", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    [Test]
    [Description("Login de Admin → redirige fuera de /login y muestra panel admin")]
    public async Task AdminLogin_ShouldRedirectToAdminPanel()
    {
        var adminEmail = Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL")
            ?? "admin@nexopostal.es";
        var adminPass = Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD")
            ?? "Admin123!";

        await Page.TestId("email-input").FillAsync(adminEmail);
        await Page.TestId("password-input").FillAsync(adminPass);
        await CaptureScreenshotAsync("05-admin-credentials-filled");

        await Page.TestId("submit-button").ClickAsync();
        await CaptureScreenshotAsync("06-after-admin-login");

        // No debe seguir en /login
        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 10000 });

        // Admin redirige a /admin
        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/admin.*"),
            new() { Timeout = 5000 });
    }

    [Test]
    [Description("Página de login contiene los elementos de formulario esperados")]
    public async Task LoginPage_ShouldHaveRequiredFormElements()
    {
        await Expect(Page.TestId("email-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("password-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("submit-button")).ToBeVisibleAsync();
        await Expect(Page.TestId("submit-button")).ToBeEnabledAsync();

        // Contiene branding de intranet
        await Expect(Page.Locator(".logo-subtitle")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex("Gesti\u00f3n Interna|intranet", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
}
