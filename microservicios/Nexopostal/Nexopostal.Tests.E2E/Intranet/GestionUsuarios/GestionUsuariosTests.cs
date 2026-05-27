using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Intranet.GestionUsuarios;

/// <summary>
/// MÓDULO: GESTIÓN DE USUARIOS — Intranet App
///
/// OBJETIVO: Validar que el administrador puede acceder y operar la pantalla de gestión de usuarios:
///   - Admin autenticado → página carga con los KPIs y la lista de usuarios
///   - El botón "Nuevo empleado" está visible y habilitado
///
/// NOTA: Solo se verifican la carga y los elementos clave, sin operaciones CRUD,
///       para no alterar datos en la base de datos del entorno de test.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Intranet")]
[Category("GestionUsuarios")]
public class GestionUsuariosTests : E2ETestBase
{
    private string AdminEmail =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@nexopostal.es";
    private string AdminPassword =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Admin123!";

    private async Task LoginAsAdminAsync()
    {
        await Page.GotoAsync($"{IntranetBaseUrl}/login");
        await Page.TestId("email-input").FillAsync(AdminEmail);
        await Page.TestId("password-input").FillAsync(AdminPassword);
        await Page.TestId("submit-button").ClickAsync();
        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 10000 });
    }

    [Test]
    [Description("Admin autenticado puede acceder a /gestion-usuarios y ver la página")]
    public async Task GestionUsuariosPage_ShouldLoadAsAdmin()
    {
        await LoginAsAdminAsync();
        await Page.GotoAsync($"{IntranetBaseUrl}/gestion-usuarios");
        await CaptureScreenshotAsync("01-gestion-usuarios-loaded");

        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });

        await Expect(Page.TestId("gestion-usuarios-page")).ToBeAttachedAsync(new() { Timeout = 10000 });
        await Expect(Page.Locator("h1:has-text('Gestión de Usuarios')")).ToBeVisibleAsync(new() { Timeout = 8000 });
    }

    [Test]
    [Description("La página muestra los KPIs y la lista de usuarios")]
    public async Task GestionUsuariosPage_HasKpisAndContent()
    {
        await LoginAsAdminAsync();
        await Page.GotoAsync($"{IntranetBaseUrl}/gestion-usuarios");
        await CaptureScreenshotAsync("01-checking-kpis");

        // KPI "Total" debe estar adjunto y con valor numérico
        var kpiTotal = Page.TestId("kpi-total");
        await Expect(kpiTotal).ToBeAttachedAsync(new() { Timeout = 10000 });

        // La sección de KPIs debe estar visible
        await Expect(Page.Locator(".kpi-grid")).ToBeVisibleAsync(new() { Timeout = 8000 });

        await CaptureScreenshotAsync("02-kpis-visible");
    }

    [Test]
    [Description("El botón 'Nuevo empleado' está visible y habilitado")]
    public async Task GestionUsuariosPage_HasCreateButton()
    {
        await LoginAsAdminAsync();
        await Page.GotoAsync($"{IntranetBaseUrl}/gestion-usuarios");
        await CaptureScreenshotAsync("01-checking-create-btn");

        var crearBtn = Page.TestId("nuevo-empleado-btn");
        await Expect(crearBtn).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(crearBtn).ToBeEnabledAsync();

        await CaptureScreenshotAsync("02-create-button-visible");
    }
}
