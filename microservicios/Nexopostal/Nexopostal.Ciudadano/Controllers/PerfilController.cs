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
    private readonly IClientePerfilRepository _perfilRepo;
    private readonly ILogger<PerfilController> _logger;

    public PerfilController(IClientePerfilRepository perfilRepo, ILogger<PerfilController> logger)
    {
        _perfilRepo = perfilRepo;
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

        var perfil = await _perfilRepo.GetByUserIdAsync(userId);

        // Devolver perfil vacío con 200 si no existe aún (evita 404 que el
        // Gateway convierte en 500 vía EnsureSuccessStatusCode).
        var resultado = new PerfilDto
        {
            IdentityUserId = perfil?.IdentityUserId ?? userId,
            DNI = perfil?.DNI,
            Telefono = perfil?.Telefono,
            DireccionPredeterminada = perfil?.DireccionPredeterminada,
            FechaCreacion = perfil?.FechaCreacion ?? DateTime.UtcNow
        };

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

        var perfilExistente = await _perfilRepo.GetByUserIdAsync(userId);

        if (perfilExistente == null)
        {
            // Crear nuevo perfil
            var nuevoPerfil = new ClientePerfil
            {
                IdentityUserId = userId,
                DNI = dto.DNI,
                Telefono = dto.Telefono,
                DireccionPredeterminada = dto.DireccionPredeterminada,
                FechaCreacion = DateTime.UtcNow
            };

            await _perfilRepo.CreateOrUpdateAsync(nuevoPerfil);

            _logger.LogInformation("Perfil creado para usuario {UserId}", userId);

            var resultado = new PerfilDto
            {
                IdentityUserId = nuevoPerfil.IdentityUserId,
                DNI = nuevoPerfil.DNI,
                Telefono = nuevoPerfil.Telefono,
                DireccionPredeterminada = nuevoPerfil.DireccionPredeterminada,
                FechaCreacion = nuevoPerfil.FechaCreacion
            };

            return CreatedAtAction(nameof(GetPerfil), resultado);
        }
        else
        {
            // Actualizar perfil existente
            perfilExistente.DNI = dto.DNI ?? perfilExistente.DNI;
            perfilExistente.Telefono = dto.Telefono ?? perfilExistente.Telefono;
            perfilExistente.DireccionPredeterminada = dto.DireccionPredeterminada ?? perfilExistente.DireccionPredeterminada;

            await _perfilRepo.CreateOrUpdateAsync(perfilExistente);

            _logger.LogInformation("Perfil actualizado para usuario {UserId}", userId);

            var resultado = new PerfilDto
            {
                IdentityUserId = perfilExistente.IdentityUserId,
                DNI = perfilExistente.DNI,
                Telefono = perfilExistente.Telefono,
                DireccionPredeterminada = perfilExistente.DireccionPredeterminada,
                FechaCreacion = perfilExistente.FechaCreacion
            };

            return Ok(resultado);
        }
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

        var perfil = await _perfilRepo.GetByUserIdAsync(userId);

        if (perfil == null)
            return Ok(new List<DireccionFavoritaDto>()); // Devuelve lista vacía si no tiene perfil

        var direcciones = perfil.Agenda.Select(d => new DireccionFavoritaDto
        {
            Id = d.Id,
            Alias = d.Alias,
            NombreDestinatario = d.NombreDestinatario,
            Direccion = d.Direccion,
            CodigoPostal = d.CodigoPostal,
            Ciudad = d.Ciudad,
            Provincia = d.Provincia,
            Telefono = d.Telefono
        }).ToList();

        return Ok(direcciones);
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

        // Obtener o crear perfil
        var perfil = await _perfilRepo.GetByUserIdAsync(userId);

        if (perfil == null)
        {
            // Si no tiene perfil, lo creamos automáticamente
            perfil = new ClientePerfil
            {
                IdentityUserId = userId,
                FechaCreacion = DateTime.UtcNow
            };
            perfil = await _perfilRepo.CreateOrUpdateAsync(perfil);
        }

        // Crear la dirección
        var nuevaDireccion = new DireccionFavorita
        {
            ClientePerfilId = perfil.Id,
            Alias = dto.Alias,
            NombreDestinatario = dto.NombreDestinatario,
            Direccion = dto.Direccion,
            CodigoPostal = dto.CodigoPostal,
            Ciudad = dto.Ciudad,
            Provincia = dto.Provincia,
            Telefono = dto.Telefono
        };

        await _perfilRepo.AddDireccionAsync(nuevaDireccion);

        _logger.LogInformation("Dirección favorita agregada: {Alias} para usuario {UserId}", dto.Alias, userId);

        var resultado = new DireccionFavoritaDto
        {
            Id = nuevaDireccion.Id,
            Alias = nuevaDireccion.Alias,
            NombreDestinatario = nuevaDireccion.NombreDestinatario,
            Direccion = nuevaDireccion.Direccion,
            CodigoPostal = nuevaDireccion.CodigoPostal,
            Ciudad = nuevaDireccion.Ciudad,
            Provincia = nuevaDireccion.Provincia,
            Telefono = nuevaDireccion.Telefono
        };

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

        var perfil = await _perfilRepo.GetByUserIdAsync(userId);
        if (perfil == null)
            return NotFound(new { mensaje = "Dirección no encontrada o no pertenece al usuario" });

        var direccion = await _perfilRepo.GetDireccionByIdAsync(id, perfil.Id);

        if (direccion == null)
            return NotFound(new { mensaje = "Dirección no encontrada o no pertenece al usuario" });

        direccion.Alias = dto.Alias;
        direccion.NombreDestinatario = dto.NombreDestinatario;
        direccion.Direccion = dto.Direccion;
        direccion.CodigoPostal = dto.CodigoPostal;
        direccion.Ciudad = dto.Ciudad;
        direccion.Provincia = dto.Provincia;
        direccion.Telefono = dto.Telefono;

        await _perfilRepo.UpdateDireccionAsync(direccion);

        _logger.LogInformation("Dirección favorita actualizada: {Alias} (ID {Id}) para usuario {UserId}", dto.Alias, id, userId);

        return Ok(new DireccionFavoritaDto
        {
            Id = direccion.Id,
            Alias = direccion.Alias,
            NombreDestinatario = direccion.NombreDestinatario,
            Direccion = direccion.Direccion,
            CodigoPostal = direccion.CodigoPostal,
            Ciudad = direccion.Ciudad,
            Provincia = direccion.Provincia,
            Telefono = direccion.Telefono
        });
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

        // Verificar que la dirección pertenece al usuario
        var perfil = await _perfilRepo.GetByUserIdAsync(userId);
        if (perfil == null)
            return NotFound(new { mensaje = "Dirección no encontrada o no pertenece al usuario" });

        var eliminada = await _perfilRepo.DeleteDireccionAsync(id, perfil.Id);
        if (!eliminada)
            return NotFound(new { mensaje = "Dirección no encontrada o no pertenece al usuario" });

        _logger.LogInformation("Dirección favorita eliminada: ID {Id} para usuario {UserId}", id, userId);

        return NoContent();
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
