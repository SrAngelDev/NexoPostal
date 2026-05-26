using Microsoft.Playwright;

namespace Nexopostal.Tests.E2E.Extensions;

/// <summary>
/// Extensiones sobre IPage para localizar elementos por data-testid,
/// siguiendo el patrón de accesibilidad recomendado por Playwright.
/// </summary>
public static class LocatorExtensions
{
    /// <summary>
    /// Devuelve un locator para el elemento con el atributo data-testid indicado.
    /// Equivale a Page.Locator("[data-testid='nombre']").
    /// </summary>
    public static ILocator TestId(this IPage page, string testId)
        => page.Locator($"[data-testid='{testId}']");

    /// <summary>
    /// Versión para locators anidados: busca data-testid dentro de otro locator.
    /// </summary>
    public static ILocator TestId(this ILocator locator, string testId)
        => locator.Locator($"[data-testid='{testId}']");
}
