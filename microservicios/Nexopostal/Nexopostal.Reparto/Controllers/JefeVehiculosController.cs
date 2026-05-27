using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Controllers;

/// <summary>
/// Endpoint de solo lectura que permite al JefeReparto (y al Admin) listar
/// los vehículos activos de su flota, para asignarlos a repartidores desde
/// la driver-app sin tener que teclear matrícula y tipo manualmente.
///
/// GET /api/reparto/vehiculos  → devuelve vehículos activos de la oficina del jefe.
/// El Admin puede filtrar por ?oficinaJsonId=N.
/// </summary>
[ApiController]
[Route("api/reparto/vehiculos")]
[Authorize(Roles = "Admin,JefeReparto")]
public class JefeVehiculosController : ControllerBase
{
    private readonly IVehiculoService _vehiculoService;
    private readonly IRepartoService _repartoService;

    public JefeVehiculosController(IVehiculoService vehiculoService, IRepartoService repartoService)
    {
        _vehiculoService = vehiculoService;
        _repartoService = repartoService;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehiculoFlotaDto>>> Listar([FromQuery] int? oficinaJsonId = null)
    {
        // JefeReparto: forzar filtro a su propia oficina
        if (User.IsInRole("JefeReparto"))
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value
                         ?? User.FindFirst("nameid")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { mensaje = "No se pudo identificar al usuario" });

            var perfil = await _repartoService.ObtenerRepartidorPorIdentityId(userId);
            if (perfil == null)
                return Unauthorized(new { mensaje = "Perfil de jefe no encontrado" });

            oficinaJsonId = perfil.OficinaJsonId;
        }

        var lista = await _vehiculoService.ListarAsync(incluirInactivos: false, oficinaJsonId: oficinaJsonId);
        return Ok(lista.Select(v => new VehiculoFlotaDto
        {
            Id          = v.Id,
            Matricula   = v.Matricula,
            Tipo        = v.Tipo.ToString(),
            Marca       = v.Marca,
            Modelo      = v.Modelo,
            Color       = v.Color,
            RepartidorAsignadoNombre = v.RepartidorAsignadoNombre
        }).ToList());
    }
}

/// <summary>DTO liviano para el selector de flota en la driver-app.</summary>
public class VehiculoFlotaDto
{
    public int Id { get; set; }
    public string Matricula { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Color { get; set; }
    public string? RepartidorAsignadoNombre { get; set; }
}
