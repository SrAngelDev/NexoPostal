namespace Nexopostal.Shared.Dtos.Common;

/// <summary>
/// Contenedor genérico de paginación para respuestas de listado.
/// </summary>
public class PagedResult<T>
{
    /// <summary>Elementos de la página actual ya transformados al DTO de salida.</summary>
    public IEnumerable<T> Items { get; init; } = [];

    /// <summary>Número total de registros que cumplen el filtro, sin paginar.</summary>
    public int TotalCount { get; init; }

    /// <summary>Página solicitada por el cliente.</summary>
    public int Page { get; init; }

    /// <summary>Tamaño de página aplicado en la consulta.</summary>
    public int PageSize { get; init; }

    /// <summary>Total de páginas disponibles con los parámetros actuales.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Indica si todavía quedan más resultados después de esta página.</summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>Indica si existe una página anterior a la actual.</summary>
    public bool HasPrevious => Page > 1;
}

/// <summary>
/// Filtro de paginación base reutilizable en queries de listado.
/// </summary>
/// <param name="Page">Página pedida por el cliente.</param>
/// <param name="Size">Número de elementos por página.</param>
/// <param name="SortBy">Campo por el que se quiere ordenar, si aplica.</param>
/// <param name="Direction">Dirección de orden asc o desc.</param>
public record PageFilter(int Page = 0, int Size = 10, string? SortBy = null, string? Direction = "asc");
