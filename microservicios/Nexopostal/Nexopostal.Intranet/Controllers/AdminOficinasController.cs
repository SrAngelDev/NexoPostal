using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Controllers;

/// <summary>
/// Gestión administrativa de oficinas postales (Admin only).
///
/// Las oficinas son nodos físicos de atención y referencia lógica desde
/// <c>OperariosOficina.OficinaJsonId</c> y <c>Repartidor.OficinaJsonId</c>.
/// </summary>
[ApiController]
[Route("api/admin-oficinas")]
[Authorize(Roles = "Admin")]
public class AdminOficinasController : ControllerBase
{
    private readonly IOficinaRepository _repo;
    private readonly OficinasJsonService _cache;
    private readonly ILogger<AdminOficinasController> _logger;

    public AdminOficinasController(
        IOficinaRepository repo,
        OficinasJsonService cache,
        ILogger<AdminOficinasController> logger)
    {
        _repo = repo;
        _cache = cache;
        _logger = logger;
    }

    // ─────────────── LISTA ───────────────
    [HttpGet]
    public async Task<ActionResult<List<OficinaPostalAdminDto>>> Listar(
        [FromQuery] bool incluirInactivas = false)
    {
        var lista = await _repo.GetAllAsync(incluirInactivas);
        var resultado = new List<OficinaPostalAdminDto>(lista.Count);
        foreach (var o in lista)
        {
            resultado.Add(await ToDtoAsync(o));
        }
        return Ok(resultado);
    }

    // ─────────────── DETALLE ───────────────
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OficinaPostalAdminDto>> Obtener(int id)
    {
        var oficina = await _repo.GetByIdAsync(id);
        if (oficina == null) return NotFound(new { mensaje = "Oficina no encontrada" });
        return Ok(await ToDtoAsync(oficina));
    }

    // ─────────────── CREAR ───────────────
    [HttpPost]
    public async Task<ActionResult<OficinaPostalAdminDto>> Crear([FromBody] CrearOficinaPostalDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var nuevoId = await _repo.NextIdAsync();
        var ahora = DateTime.UtcNow;
        var userId = GetUserId();

        var oficina = new OficinaPostal
        {
            Id = nuevoId,
            Nombre = dto.Nombre.Trim(),
            Direccion = dto.Direccion.Trim(),
            CodigoPostal = dto.CodigoPostal.Trim(),
            Ciudad = dto.Ciudad.Trim(),
            Provincia = dto.Provincia?.Trim(),
            Telefono = dto.Telefono?.Trim(),
            Horario = dto.Horario?.Trim(),
            Servicios = dto.Servicios?.Trim(),
            Latitud = dto.Latitud,
            Longitud = dto.Longitud,
            Activo = true,
            FechaAlta = ahora,
            FechaModificacion = ahora,
            ModificadoPorUserId = userId
        };

        await _repo.CreateAsync(oficina);
        _cache.Invalidar();
        _logger.LogInformation("Admin {UserId} creó oficina {Id} ({Nombre})", userId, oficina.Id, oficina.Nombre);

        return CreatedAtAction(nameof(Obtener), new { id = oficina.Id }, await ToDtoAsync(oficina));
    }

    // ─────────────── EDITAR ───────────────
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OficinaPostalAdminDto>> Actualizar(int id, [FromBody] ActualizarOficinaPostalDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var oficina = await _repo.GetByIdAsync(id);
        if (oficina == null) return NotFound(new { mensaje = "Oficina no encontrada" });

        oficina.Nombre = dto.Nombre.Trim();
        oficina.Direccion = dto.Direccion.Trim();
        oficina.CodigoPostal = dto.CodigoPostal.Trim();
        oficina.Ciudad = dto.Ciudad.Trim();
        oficina.Provincia = dto.Provincia?.Trim();
        oficina.Telefono = dto.Telefono?.Trim();
        oficina.Horario = dto.Horario?.Trim();
        oficina.Servicios = dto.Servicios?.Trim();
        oficina.Latitud = dto.Latitud;
        oficina.Longitud = dto.Longitud;
        oficina.FechaModificacion = DateTime.UtcNow;
        oficina.ModificadoPorUserId = GetUserId();

        await _repo.UpdateAsync(oficina);
        _cache.Invalidar();

        return Ok(await ToDtoAsync(oficina));
    }

    // ─────────────── DESACTIVAR ───────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var oficina = await _repo.GetByIdAsync(id);
        if (oficina == null) return NotFound(new { mensaje = "Oficina no encontrada" });

        if (!oficina.Activo)
            return BadRequest(new { mensaje = "La oficina ya está desactivada" });

        var operariosActivos = await _repo.CountOperariosActivosAsync(id);
        if (operariosActivos > 0)
        {
            return Conflict(new
            {
                mensaje = $"No se puede desactivar: hay {operariosActivos} operario(s) activos asignados a esta oficina"
            });
        }

        oficina.Activo = false;
        oficina.FechaModificacion = DateTime.UtcNow;
        oficina.ModificadoPorUserId = GetUserId();
        await _repo.UpdateAsync(oficina);
        _cache.Invalidar();

        return Ok(new { mensaje = "Oficina desactivada", id });
    }

    // ─────────────── REACTIVAR ───────────────
    [HttpPost("{id:int}/reactivar")]
    public async Task<IActionResult> Reactivar(int id)
    {
        var oficina = await _repo.GetByIdAsync(id);
        if (oficina == null) return NotFound(new { mensaje = "Oficina no encontrada" });

        if (oficina.Activo)
            return BadRequest(new { mensaje = "La oficina ya está activa" });

        oficina.Activo = true;
        oficina.FechaModificacion = DateTime.UtcNow;
        oficina.ModificadoPorUserId = GetUserId();
        await _repo.UpdateAsync(oficina);
        _cache.Invalidar();

        return Ok(new { mensaje = "Oficina reactivada", id });
    }

    // ─────────────── helpers ───────────────
    private async Task<OficinaPostalAdminDto> ToDtoAsync(OficinaPostal o) => new()
    {
        Id = o.Id,
        Nombre = o.Nombre,
        Direccion = o.Direccion,
        CodigoPostal = o.CodigoPostal,
        Ciudad = o.Ciudad,
        Provincia = o.Provincia,
        Telefono = o.Telefono,
        Horario = o.Horario,
        Servicios = o.Servicios,
        Latitud = o.Latitud,
        Longitud = o.Longitud,
        Activo = o.Activo,
        FechaAlta = o.FechaAlta,
        FechaModificacion = o.FechaModificacion,
        OperariosActivos = await _repo.CountOperariosActivosAsync(o.Id)
    };

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? User.FindFirst("nameid")?.Value;
}
