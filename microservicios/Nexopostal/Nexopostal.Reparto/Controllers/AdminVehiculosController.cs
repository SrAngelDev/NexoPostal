using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Controllers;

/// <summary>
/// Gestión administrativa de la flota de vehículos solo para administradores.
/// </summary>
[ApiController]
[Route("api/admin-vehiculos")]
[Authorize(Roles = "Admin")]
public class AdminVehiculosController : ControllerBase
{
    private readonly IVehiculoService _service;

    public AdminVehiculosController(IVehiculoService service)
    {
        _service = service;
    }

    /// <summary>
    /// Devuelve la flota con filtros opcionales de actividad, oficina o repartidor asignado.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<VehiculoDto>>> Listar(
        [FromQuery] bool incluirInactivos = false,
        [FromQuery] int? oficinaJsonId = null,
        [FromQuery] int? repartidorId = null)
    {
        var lista = await _service.ListarAsync(incluirInactivos, oficinaJsonId, repartidorId);
        return Ok(lista.Select(ToDto).ToList());
    }

    /// <summary>Recupera un vehículo concreto por su identificador.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehiculoDto>> Obtener(int id)
    {
        var v = await _service.ObtenerAsync(id);
        if (v == null) return NotFound(new { mensaje = "Vehículo no encontrado" });
        return Ok(ToDto(v));
    }

    /// <summary>Crea un nuevo vehículo dentro de la flota administrada.</summary>
    [HttpPost]
    public async Task<ActionResult<VehiculoDto>> Crear([FromBody] CrearVehiculoDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var (v, error) = await _service.CrearAsync(dto, GetUserId());
        if (error != null) return Conflict(new { mensaje = error });
        return CreatedAtAction(nameof(Obtener), new { id = v!.Id }, ToDto(v));
    }

    /// <summary>Actualiza la ficha de un vehículo ya existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<VehiculoDto>> Actualizar(int id, [FromBody] ActualizarVehiculoDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var (v, error) = await _service.ActualizarAsync(id, dto, GetUserId());
        if (error == "Vehículo no encontrado") return NotFound(new { mensaje = error });
        if (error != null) return Conflict(new { mensaje = error });
        return Ok(ToDto(v!));
    }

    /// <summary>Desactiva un vehículo sin borrarlo físicamente para conservar histórico.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var (ok, error) = await _service.DesactivarAsync(id, GetUserId());
        if (!ok && error == "Vehículo no encontrado") return NotFound(new { mensaje = error });
        if (!ok) return Conflict(new { mensaje = error });
        return Ok(new { mensaje = "Vehículo desactivado", id });
    }

    /// <summary>Recupera un vehículo desactivado cuando vuelve a estar disponible.</summary>
    [HttpPost("{id:int}/reactivar")]
    public async Task<IActionResult> Reactivar(int id)
    {
        var (ok, error) = await _service.ReactivarAsync(id, GetUserId());
        if (!ok && error == "Vehículo no encontrado") return NotFound(new { mensaje = error });
        if (!ok) return BadRequest(new { mensaje = error });
        return Ok(new { mensaje = "Vehículo reactivado", id });
    }

    /// <summary>Asigna un vehículo de flota a un repartidor concreto.</summary>
    [HttpPost("{id:int}/asignar")]
    public async Task<ActionResult<VehiculoDto>> Asignar(int id, [FromBody] AsignarVehiculoDto dto)
    {
        var (v, error) = await _service.AsignarAsync(id, dto.RepartidorId, GetUserId());
        if (error == "Vehículo no encontrado" || error == "Repartidor no encontrado")
            return NotFound(new { mensaje = error });
        if (error != null) return BadRequest(new { mensaje = error });
        return Ok(ToDto(v!));
    }

    /// <summary>
    /// Sincroniza la flota inicial a partir de los datos históricos de repartidores ya existentes.
    /// </summary>
    [HttpPost("importar-desde-repartidores")]
    public async Task<ActionResult<ImportarDesdeRepartidoresResultDto>> Importar()
    {
        var resultado = await _service.ImportarDesdeRepartidoresAsync(GetUserId());
        return Ok(resultado);
    }

    /// <summary>Transforma la entidad en el DTO que consume la API administrativa.</summary>
    private static VehiculoDto ToDto(Vehiculo v) => new()
    {
        Id = v.Id,
        Matricula = v.Matricula,
        Tipo = v.Tipo,
        Marca = v.Marca,
        Modelo = v.Modelo,
        Color = v.Color,
        AnioFabricacion = v.AnioFabricacion,
        RepartidorAsignadoId = v.RepartidorAsignadoId,
        RepartidorAsignadoNombre = v.RepartidorAsignadoNombre,
        OficinaJsonId = v.OficinaJsonId,
        Notas = v.Notas,
        Activo = v.Activo,
        FechaAlta = v.FechaAlta,
        FechaModificacion = v.FechaModificacion
    };

    /// <summary>Obtiene el identificador del usuario autenticado desde las claims habituales del JWT.</summary>
    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? User.FindFirst("nameid")?.Value;
}
