using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Clientes.Auth;

/// <summary>
/// MÓDULO: AUTENTICACIÓN — Clientes App (portal público)
///
/// OBJETIVO: Validar el modal de login del portal de clientes.
///   - El modal se abre desde el navbar
///   - Campos vacíos → mensaje de error
///   - Credenciales inválidas → mensaje de error
///   - Credenciales válidas → el modal se cierra y el usuario queda autenticado
///
/// NOTA: El login de clientes-app es un modal flotante, no una página aparte.
///       Los tests requieren la app en localhost:80.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Clientes")]
[Category("Auth")]
public class ClientesAuthTests : E2ETestBase
{
    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync(ClientesBaseUrl);
        await CaptureScreenshotAsync("01-home-loaded");
    }

    [Test]
    [Description("El botón 'Iniciar sesión' del navbar abre el modal de login")]
    public async Task NavbarLoginButton_ShouldOpenLoginModal()
    {
        await Page.TestId("navbar-login-btn").ClickAsync();
        await CaptureScreenshotAsync("02-login-modal-opened");

        await Expect(Page.TestId("email-input")).ToBeVisibleAsync(new() { Timeout = 5000 });
        await Expect(Page.TestId("password-input")).ToBeVisibleAsync();
        await Expect(Page.TestId("submit-button")).ToBeVisibleAsync();
    }

    [Test]
    [Description("Campos vacíos en modal → mensaje de error de validación")]
    public async Task EmptyFields_ShouldShowErrorMessage()
    {
        await Page.TestId("navbar-login-btn").ClickAsync();
        await Expect(Page.TestId("submit-button")).ToBeVisibleAsync(new() { Timeout = 5000 });

        await Page.TestId("submit-button").ClickAsync();
        await CaptureScreenshotAsync("03-empty-submit");

        var error = Page.TestId("error-message");
        await Expect(error).ToBeVisibleAsync(new() { Timeout = 8000 });
        await Expect(error).ToContainTextAsync("completa todos los campos", new() { IgnoreCase = true });
    }

    [Test]
    [Description("Credenciales inválidas → error visible en el modal")]
    public async Task InvalidCredentials_ShouldShowError()
    {
        await Page.TestId("navbar-login-btn").ClickAsync();
        await Expect(Page.TestId("email-input")).ToBeVisibleAsync(new() { Timeout = 5000 });

        await Page.TestId("email-input").FillAsync("fraude@malefico.com");
        await Page.TestId("password-input").FillAsync("clave_incorrecta");
        await CaptureScreenshotAsync("04-invalid-credentials");

        await Page.TestId("submit-button").ClickAsync();
        await CaptureScreenshotAsync("05-after-invalid-login");

        var error = Page.TestId("error-message");
        await Expect(error).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    [Description("Login válido → modal se cierra y el usuario queda autenticado")]
    public async Task ValidClienteLogin_ShouldCloseModalAndAuthenticate()
    {
        var clientEmail = Environment.GetEnvironmentVariable("E2E_CLIENTE_EMAIL")
            ?? "cliente@example.com";
        var clientPass  = Environment.GetEnvironmentVariable("E2E_CLIENTE_PASSWORD")
            ?? "Cliente123!";

        await Page.TestId("navbar-login-btn").ClickAsync();
        await Expect(Page.TestId("email-input")).ToBeVisibleAsync(new() { Timeout = 5000 });

        await Page.TestId("email-input").FillAsync(clientEmail);
        await Page.TestId("password-input").FillAsync(clientPass);
        await CaptureScreenshotAsync("06-valid-credentials");

        await Page.TestId("submit-button").ClickAsync();
        await CaptureScreenshotAsync("07-after-valid-login");

        // El modal desaparece tras login correcto (el modal no debe estar visible)
        await Expect(Page.TestId("submit-button")).Not.ToBeVisibleAsync(new() { Timeout = 10000 });
    }
}
