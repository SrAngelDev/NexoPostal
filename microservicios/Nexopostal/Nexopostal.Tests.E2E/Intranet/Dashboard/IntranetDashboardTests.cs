using Microsoft.Playwright.NUnit;
using Nexopostal.Tests.E2E.Extensions;

namespace Nexopostal.Tests.E2E.Intranet.Dashboard;

/// <summary>
/// MÓDULO: DASHBOARD E INTRANET PROTEGIDA — Intranet App
///
/// OBJETIVO: Validar que las rutas protegidas redirigen correctamente y que
///           el panel admin carga con el usuario administrador.
///   - Rutas protegidas sin sesión → /login
///   - Admin login → redirige a /admin con estadísticas
///   - Operario Oficina login → dashboard con sus tarjetas específicas
///
/// PREREQUISITO: Variables de entorno:
///   E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD
///   E2E_OPERARIO_EMAIL / E2E_OPERARIO_PASSWORD
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("E2E")]
[Category("Intranet")]
[Category("Dashboard")]
public class IntranetDashboardTests : E2ETestBase
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private string AdminEmail =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@nexopostal.es";
    private string AdminPassword =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Admin123!";

    private string OperarioEmail =>
        Environment.GetEnvironmentVariable("E2E_OPERARIO_EMAIL") ?? "operario@nexopostal.es";
    private string OperarioPassword =>
        Environment.GetEnvironmentVariable("E2E_OPERARIO_PASSWORD") ?? "Operario123!";

    private async Task LoginAsAsync(string email, string password)
    {
        await Page.GotoAsync($"{IntranetBaseUrl}/login");
        await Page.TestId("email-input").FillAsync(email);
        await Page.TestId("password-input").FillAsync(password);
        await Page.TestId("submit-button").ClickAsync();
        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 10000 });
    }

    // ── Tests de guardas ───────────────────────────────────────────────────────

    [Test]
    [Description("Acceder a /admin sin sesión → redirige al login")]
    public async Task AdminRoute_RedirectsToLoginWhenUnauthenticated()
    {
        await Page.GotoAsync($"{IntranetBaseUrl}/admin");
        await CaptureScreenshotAsync("01-admin-unauthenticated");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });
    }

    [Test]
    [Description("Acceder a /gestion-usuarios sin sesión → redirige al login")]
    public async Task GestionUsuariosRoute_RedirectsToLoginWhenUnauthenticated()
    {
        await Page.GotoAsync($"{IntranetBaseUrl}/gestion-usuarios");
        await CaptureScreenshotAsync("02-gestion-usuarios-unauthenticated");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });
    }

    // ── Tests con login de Admin ───────────────────────────────────────────────

    [Test]
    [Description("Admin autenticado → ve el panel de administración en /admin")]
    public async Task AdminUser_ShouldSeeAdminPanel()
    {
        await LoginAsAsync(AdminEmail, AdminPassword);
        await CaptureScreenshotAsync("03-admin-panel-loaded");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/admin.*"),
            new() { Timeout = 8000 });

        await Expect(Page.TestId("intranet-dashboard").Or(Page.Locator("app-admin-panel")))
            .ToBeAttachedAsync(new() { Timeout = 8000 });
    }

    [Test]
    [Description("Admin autenticado puede navegar a /gestion-usuarios")]
    public async Task AdminUser_CanNavigateToGestionUsuarios()
    {
        await LoginAsAsync(AdminEmail, AdminPassword);

        await Page.GotoAsync($"{IntranetBaseUrl}/gestion-usuarios");
        await CaptureScreenshotAsync("04-gestion-usuarios-loaded");

        // No debe redirigir a login (el admin tiene acceso)
        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/login.*"),
            new() { Timeout = 8000 });

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/gestion-usuarios.*"),
            new() { Timeout = 5000 });
    }

    [Test]
    [Description("Admin autenticado puede navegar a /gestion-clientes")]
    public async Task AdminUser_CanNavigateToGestionClientes()
    {
        await LoginAsAsync(AdminEmail, AdminPassword);

        await Page.GotoAsync($"{IntranetBaseUrl}/gestion-clientes");
        await CaptureScreenshotAsync("05-gestion-clientes-loaded");

        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/gestion-clientes.*"),
            new() { Timeout = 8000 });
    }

    [Test]
    [Description("Operario Oficina autenticado → ve el dashboard de intranet (no /admin)")]
    public async Task OperarioUser_ShouldSeeDashboardNotAdminPanel()
    {
        await LoginAsAsync(OperarioEmail, OperarioPassword);
        await CaptureScreenshotAsync("06-operario-dashboard");

        // El operario no debe ir a /admin
        await Expect(Page).Not.ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/admin.*"),
            new() { Timeout = 8000 });

        // Debe estar en el dashboard
        await Expect(Page.TestId("intranet-dashboard")).ToBeVisibleAsync(new() { Timeout = 8000 });
        await Expect(Page.TestId("dashboard-title")).ToContainTextAsync("Panel de Gestión", new() { IgnoreCase = true });
    }
}
