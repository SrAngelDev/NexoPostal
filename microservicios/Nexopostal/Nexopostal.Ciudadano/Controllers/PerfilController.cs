using MediatR;
using Nexopostal.Ciudadano.Application.Perfil;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using System.Security.Claims;

namespace Nexopostal.Ciudadano.Controllers;

/// <summary>
/// Controlador para la gestión del perfil del ciudadano
/// Gestiona datos adicionales al Identity (DNI, teléfono, direcciones favoritas)
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class PerfilController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<PerfilController> _logger;

    public PerfilController(ISender sender, ILogger<PerfilController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el perfil del usuario autenticado
    /// </summary>
    /// <returns>Datos del perfil</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PerfilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPerfil()
    {

        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("Token inválido");

        var resultado = await _sender.Send(new ObtenerPerfilQuery(userId), HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(resultado);
    }

    /// <summary>
    /// Crea o actualiza el perfil del usuario
    /// Se llama después del registro en Identity
    /// </summary>
    /// <param name="dto">Datos del perfil a actualizar</param>
    /// <returns>Perfil actualizado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PerfilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PerfilDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CrearOActualizarPerfil([FromBody] ActualizarPerfilDto dto)
    {

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("Token inválido");

        var resultado = await _sender.Send(new GuardarPerfilCommand(userId, dto), HttpContext?.RequestAborted ?? CancellationToken.None);
        return resultado.Creado ? CreatedAtAction(nameof(GetPerfil), resultado.Perfil) : Ok(resultado.Perfil);
    }

    // ===== GESTIÓN DE AGENDA DE DIRECCIONES =====

    /// <summary>
    /// Obtiene las direcciones favoritas del usuario
    /// </summary>
    /// <returns>Lista de direcciones guardadas</returns>
    [HttpGet("direcciones")]
    [ProducesResponseType(typeof(IEnumerable<DireccionFavoritaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDireccionesFavoritas()
    {

        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("Token inválido");

        var resultado = await _sender.Send(new ObtenerDireccionesQuery(userId), HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(resultado);
    }

    /// <summary>
    /// Agrega una nueva dirección favorita
    /// </summary>
    /// <param name="dto">Datos de la dirección</param>
    /// <returns>Dirección creada</returns>
    [HttpPost("direcciones")]
    [ProducesResponseType(typeof(DireccionFavoritaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AgregarDireccionFavorita([FromBody] CrearDireccionFavoritaDto dto)
    {

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(k => k.Key, v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
            _logger.LogWarning("Validación fallida en AgregarDireccionFavorita: {@Errors}", errors);
            return BadRequest(ModelState);
        }

        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("Token inválido");

        var resultado = await _sender.Send(new AgregarDireccionCommand(userId, dto), HttpContext?.RequestAborted ?? CancellationToken.None);
        return CreatedAtAction(nameof(GetDireccionesFavoritas), resultado);
    }

    /// <summary>
    /// Actualiza una dirección favorita existente
    /// </summary>
    /// <param name="id">ID de la dirección a editar</param>
    /// <param name="dto">Nuevos datos de la dirección</param>
    /// <returns>Dirección actualizada</returns>
    [HttpPut("direcciones/{id}")]
    [ProducesResponseType(typeof(DireccionFavoritaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActualizarDireccionFavorita(int id, [FromBody] CrearDireccionFavoritaDto dto)
    {

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("Token inválido");

        var resultado = await _sender.Send(new ActualizarDireccionCommand(userId, id, dto), HttpContext?.RequestAborted ?? CancellationToken.None);
        return resultado is null ? NotFound(new { mensaje = "Dirección no encontrada o no pertenece al usuario" }) : Ok(resultado);
    }

    /// <summary>
    /// Elimina una dirección favorita
    /// </summary>
    /// <param name="id">ID de la dirección</param>
    /// <returns>No Content si se elimina correctamente</returns>
    [HttpDelete("direcciones/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarDireccionFavorita(int id)
    {

        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("Token inválido");

        var resultado = await _sender.Send(new EliminarDireccionCommand(userId, id), HttpContext?.RequestAborted ?? CancellationToken.None);
        return resultado ? NoContent() : NotFound(new { mensaje = "Dirección no encontrada o no pertenece al usuario" });
    }

    // ===== MÉTODO AUXILIAR =====

    /// <summary>
    /// Extrae el ID del usuario desde el token JWT
    /// Intenta con diferentes claim names (NameIdentifier, sub, uid)
    /// </summary>
    private string? GetUserIdFromToken()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value
               ?? User.FindFirst("uid")?.Value;
    }
}
