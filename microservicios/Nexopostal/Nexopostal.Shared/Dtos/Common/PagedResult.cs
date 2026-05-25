namespace Nexopostal.Shared.Dtos.Common;

/// <summary>
/// Contenedor genérico de paginación para respuestas de listado.
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

/// <summary>
/// Filtro de paginación base reutilizable en queries de listado.
/// </summary>
public record PageFilter(int Page = 0, int Size = 10, string? SortBy = null, string? Direction = "asc");
