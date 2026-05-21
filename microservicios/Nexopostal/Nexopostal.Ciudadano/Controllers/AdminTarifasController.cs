using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Ciudadano.Data;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Nexopostal.Ciudadano.Controllers;

/// <summary>
/// CRUD administrativo de las bandas de tarifas editables (precios base sin IVA).
/// Solo accesible por usuarios con rol Admin. Tras cualquier escritura se invalida la caché del servicio.
/// </summary>
[ApiController]
[Route("api/admin-tarifas")]
[Authorize(Roles = "Admin")]
public class AdminTarifasController : ControllerBase
{
    private readonly CiudadanoDbContext _db;
    private readonly ITarifasService _tarifas;
    private readonly ILogger<AdminTarifasController> _logger;

    public AdminTarifasController(CiudadanoDbContext db, ITarifasService tarifas, ILogger<AdminTarifasController> logger)
    {
        _db = db;
        _tarifas = tarifas;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TarifaBandaDto>>> Listar()
    {
        var bandas = await _db.TarifasBandas
            .AsNoTracking()
            .OrderBy(b => b.Serie)
            .ThenBy(b => b.OrdenBanda)
            .ToListAsync();

        return Ok(bandas.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TarifaBandaDto>> Obtener(int id)
    {
        var banda = await _db.TarifasBandas.FindAsync(id);
        if (banda is null) return NotFound();
        return Ok(ToDto(banda));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TarifaBandaDto>> Editar(int id, [FromBody] EditarTarifaBandaDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var banda = await _db.TarifasBandas.FindAsync(id);
        if (banda is null) return NotFound();

        banda.PrecioBase = dto.PrecioBase;
        banda.FechaModificacion = DateTime.UtcNow;
        banda.ModificadoPorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                     ?? User.FindFirst("sub")?.Value
                                     ?? User.FindFirst("uid")?.Value;

        await _db.SaveChangesAsync();
        _tarifas.Invalidar();

        _logger.LogInformation("Tarifa banda {Id} ({Serie}/{Orden}) actualizada a {Precio}€ por {User}",
            banda.Id, banda.Serie, banda.OrdenBanda, banda.PrecioBase, banda.ModificadoPorUserId);

        return Ok(ToDto(banda));
    }

    /// <summary>
    /// Actualiza varias bandas de una sola vez (operación de guardar tabla completa).
    /// </summary>
    [HttpPut("bulk")]
    public async Task<ActionResult<IEnumerable<TarifaBandaDto>>> EditarBulk([FromBody] List<EditarTarifaBandaBulkItemDto> items)
    {
        if (items is null || items.Count == 0) return BadRequest(new { error = "Sin items" });

        var ids = items.Select(i => i.Id).ToList();
        var bandas = await _db.TarifasBandas.Where(b => ids.Contains(b.Id)).ToListAsync();
        if (bandas.Count != items.Count) return BadRequest(new { error = "Algunos ids no existen" });

        var ahora = DateTime.UtcNow;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("uid")?.Value;

        foreach (var banda in bandas)
        {
            var item = items.First(i => i.Id == banda.Id);
            if (item.PrecioBase <= 0) return BadRequest(new { error = $"Precio inválido para id {banda.Id}" });
            banda.PrecioBase = item.PrecioBase;
            banda.FechaModificacion = ahora;
            banda.ModificadoPorUserId = userId;
        }

        await _db.SaveChangesAsync();
        _tarifas.Invalidar();

        _logger.LogInformation("Edición bulk de {Count} tarifas por {User}", bandas.Count, userId);

        return Ok(bandas.OrderBy(b => b.Serie).ThenBy(b => b.OrdenBanda).Select(ToDto));
    }

    /// <summary>
    /// Restaura los valores hardcodeados originales (defaults).
    /// </summary>
    [HttpPost("reset-defaults")]
    public async Task<ActionResult<IEnumerable<TarifaBandaDto>>> Reset()
    {
        var defaults = new (TarifaSerie Serie, int Orden, decimal Precio)[]
        {
            (TarifaSerie.LocalEstandar, 0, 4.50m), (TarifaSerie.LocalEstandar, 1, 5.25m), (TarifaSerie.LocalEstandar, 2, 6.95m), (TarifaSerie.LocalEstandar, 3, 9.95m), (TarifaSerie.LocalEstandar, 4, 14.95m), (TarifaSerie.LocalEstandar, 5, 19.95m),
            (TarifaSerie.LocalPremium, 0, 6.50m), (TarifaSerie.LocalPremium, 1, 7.75m), (TarifaSerie.LocalPremium, 2, 10.50m), (TarifaSerie.LocalPremium, 3, 14.95m), (TarifaSerie.LocalPremium, 4, 21.95m), (TarifaSerie.LocalPremium, 5, 29.95m),
            (TarifaSerie.PeninsulaEstandar, 0, 5.95m), (TarifaSerie.PeninsulaEstandar, 1, 6.95m), (TarifaSerie.PeninsulaEstandar, 2, 8.95m), (TarifaSerie.PeninsulaEstandar, 3, 12.95m), (TarifaSerie.PeninsulaEstandar, 4, 18.95m), (TarifaSerie.PeninsulaEstandar, 5, 25.95m),
            (TarifaSerie.PeninsulaPremium, 0, 8.95m), (TarifaSerie.PeninsulaPremium, 1, 10.50m), (TarifaSerie.PeninsulaPremium, 2, 13.95m), (TarifaSerie.PeninsulaPremium, 3, 19.95m), (TarifaSerie.PeninsulaPremium, 4, 28.95m), (TarifaSerie.PeninsulaPremium, 5, 38.95m)
        };

        var bandas = await _db.TarifasBandas.ToListAsync();
        var ahora = DateTime.UtcNow;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        foreach (var (serie, orden, precio) in defaults)
        {
            var banda = bandas.FirstOrDefault(b => b.Serie == serie && b.OrdenBanda == orden);
            if (banda is null) continue;
            banda.PrecioBase = precio;
            banda.FechaModificacion = ahora;
            banda.ModificadoPorUserId = userId;
        }

        await _db.SaveChangesAsync();
        _tarifas.Invalidar();

        _logger.LogInformation("Tarifas restablecidas a defaults por {User}", userId);

        return Ok(bandas.OrderBy(b => b.Serie).ThenBy(b => b.OrdenBanda).Select(ToDto));
    }

    private static TarifaBandaDto ToDto(TarifaBanda b) => new()
    {
        Id = b.Id,
        Serie = b.Serie.ToString(),
        OrdenBanda = b.OrdenBanda,
        PesoHastaKg = b.PesoHastaKg,
        PrecioBase = b.PrecioBase,
        FechaModificacion = b.FechaModificacion,
        ModificadoPorUserId = b.ModificadoPorUserId
    };
}

public class TarifaBandaDto
{
    public int Id { get; set; }
    public string Serie { get; set; } = string.Empty;
    public int OrdenBanda { get; set; }
    public decimal PesoHastaKg { get; set; }
    public decimal PrecioBase { get; set; }
    public DateTime FechaModificacion { get; set; }
    public string? ModificadoPorUserId { get; set; }
}

public class EditarTarifaBandaDto
{
    [Range(0.01, 9999.99, ErrorMessage = "Precio debe estar entre 0.01 y 9999.99")]
    public decimal PrecioBase { get; set; }
}

public class EditarTarifaBandaBulkItemDto
{
    public int Id { get; set; }
    [Range(0.01, 9999.99)]
    public decimal PrecioBase { get; set; }
}
