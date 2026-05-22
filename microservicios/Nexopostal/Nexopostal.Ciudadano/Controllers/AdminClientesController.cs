using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;

namespace Nexopostal.Ciudadano.Controllers;

/// <summary>
/// Endpoints administrativos de clientes para el panel de admin.
/// Devuelve perfil + agenda + envíos recientes asociados a un usuario.
/// </summary>
[ApiController]
[Route("api/admin-clientes")]
[Authorize(Roles = "Admin")]
public class AdminClientesController : ControllerBase
{
    private readonly IClientePerfilRepository _perfilRepo;
    private readonly IEnvioRepository _envioRepo;

    public AdminClientesController(IClientePerfilRepository perfilRepo, IEnvioRepository envioRepo)
    {
        _perfilRepo = perfilRepo;
        _envioRepo = envioRepo;
    }

    /// <summary>Perfil completo agregado de un cliente (perfil + agenda + últimos envíos).</summary>
    [HttpGet("{identityUserId}/perfil-completo")]
    public async Task<IActionResult> PerfilCompleto(string identityUserId, [FromQuery] int maxEnvios = 50)
    {
        if (maxEnvios <= 0 || maxEnvios > 500) maxEnvios = 50;

        var perfil = await _perfilRepo.GetByUserIdAsync(identityUserId);
        var envios = await _envioRepo.GetByUserAsync(identityUserId);

        return Ok(new
        {
            identityUserId,
            perfil = perfil == null ? null : new
            {
                perfil.Id,
                perfil.IdentityUserId,
                perfil.DNI,
                perfil.Telefono,
                perfil.DireccionPredeterminada,
                perfil.FechaCreacion,
                agenda = perfil.Agenda.Select(d => new
                {
                    d.Id,
                    d.Alias,
                    d.NombreDestinatario,
                    d.Direccion,
                    d.CodigoPostal,
                    d.Ciudad,
                    d.Provincia,
                    d.Telefono
                })
            },
            estadisticas = new
            {
                totalEnvios = envios.Count,
                pagados = envios.Count(e => e.Pagado),
                entregados = envios.Count(e => e.EstadoActual == EstadoEnvio.Entregado),
                incidencias = envios.Count(e => e.EstadoActual == EstadoEnvio.Incidencia),
                gastoTotal = envios.Where(e => e.Pagado).Sum(e => e.CosteCalculado)
            },
            envios = envios.Take(maxEnvios).Select(e => new
            {
                e.NumeroSeguimiento,
                e.NumeroExpedicion,
                e.FechaCreacion,
                e.EstadoActual,
                e.EstadoInternoActual,
                e.Pagado,
                e.Origen,
                e.Destino,
                e.CodigoPostalDestino,
                e.NombreDestinatario,
                e.CosteCalculado,
                e.TipoTarifa
            })
        });
    }
}
