using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;

namespace Nexopostal.Ciudadano.Application.Perfil;
public sealed record ObtenerPerfilQuery(string UserId) : IQuery<PerfilDto>;
public sealed record GuardarPerfilCommand(string UserId, ActualizarPerfilDto Dto) : ICommand<PerfilGuardado>;
public sealed record ObtenerDireccionesQuery(string UserId) : IQuery<List<DireccionFavoritaDto>>;
public sealed record AgregarDireccionCommand(string UserId, CrearDireccionFavoritaDto Dto) : ICommand<DireccionFavoritaDto>;
public sealed record ActualizarDireccionCommand(string UserId, int Id, CrearDireccionFavoritaDto Dto) : ICommand<DireccionFavoritaDto?>;
public sealed record EliminarDireccionCommand(string UserId, int Id) : ICommand<bool>;
public sealed record PerfilGuardado(PerfilDto Perfil, bool Creado);
public sealed class PerfilHandlers : IRequestHandler<ObtenerPerfilQuery, PerfilDto>, IRequestHandler<GuardarPerfilCommand, PerfilGuardado>, IRequestHandler<ObtenerDireccionesQuery, List<DireccionFavoritaDto>>, IRequestHandler<AgregarDireccionCommand, DireccionFavoritaDto>, IRequestHandler<ActualizarDireccionCommand, DireccionFavoritaDto?>, IRequestHandler<EliminarDireccionCommand, bool>
{
    private readonly IClientePerfilRepository _perfilRepo;
    private readonly ILogger<PerfilHandlers> _logger;
    public PerfilHandlers(IClientePerfilRepository perfilRepo, ILogger<PerfilHandlers> logger)
    {
        _perfilRepo = perfilRepo;
        _logger = logger;
    }

    public async Task<PerfilDto> Handle(ObtenerPerfilQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = request.UserId;
        var perfil = await _perfilRepo.GetByUserIdAsync(userId);
        // Mantener el contrato de perfil vacío cuando todavía no se ha completado.
        var resultado = new PerfilDto
        {
            IdentityUserId = perfil?.IdentityUserId ?? userId,
            DNI = perfil?.DNI,
            Telefono = perfil?.Telefono,
            DireccionPredeterminada = perfil?.DireccionPredeterminada,
            FechaCreacion = perfil?.FechaCreacion ?? DateTime.UtcNow
        };
        return resultado;
    }

    public async Task<PerfilGuardado> Handle(GuardarPerfilCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = request.UserId;
        var dto = request.Dto;
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
            return new PerfilGuardado(resultado, true);
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
            return new PerfilGuardado(resultado, false);
        }
    }

    public async Task<List<DireccionFavoritaDto>> Handle(ObtenerDireccionesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = request.UserId;
        var perfil = await _perfilRepo.GetByUserIdAsync(userId);
        if (perfil == null)
            return new List<DireccionFavoritaDto>(); // Devuelve lista vacía si no tiene perfil
        var direcciones = perfil.Agenda.Select(d => new DireccionFavoritaDto { Id = d.Id, Alias = d.Alias, NombreDestinatario = d.NombreDestinatario, Direccion = d.Direccion, CodigoPostal = d.CodigoPostal, Ciudad = d.Ciudad, Provincia = d.Provincia, Telefono = d.Telefono }).ToList();
        return direcciones;
    }

    public async Task<DireccionFavoritaDto> Handle(AgregarDireccionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = request.UserId;
        var dto = request.Dto;
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
        return resultado;
    }

    public async Task<DireccionFavoritaDto?> Handle(ActualizarDireccionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = request.UserId;
        var dto = request.Dto;
        var id = request.Id;
        var perfil = await _perfilRepo.GetByUserIdAsync(userId);
        if (perfil == null)
            return null;
        var direccion = await _perfilRepo.GetDireccionByIdAsync(id, perfil.Id);
        if (direccion == null)
            return null;
        direccion.Alias = dto.Alias;
        direccion.NombreDestinatario = dto.NombreDestinatario;
        direccion.Direccion = dto.Direccion;
        direccion.CodigoPostal = dto.CodigoPostal;
        direccion.Ciudad = dto.Ciudad;
        direccion.Provincia = dto.Provincia;
        direccion.Telefono = dto.Telefono;
        await _perfilRepo.UpdateDireccionAsync(direccion);
        _logger.LogInformation("Dirección favorita actualizada: {Alias} (ID {Id}) para usuario {UserId}", dto.Alias, id, userId);
        return new DireccionFavoritaDto
        {
            Id = direccion.Id,
            Alias = direccion.Alias,
            NombreDestinatario = direccion.NombreDestinatario,
            Direccion = direccion.Direccion,
            CodigoPostal = direccion.CodigoPostal,
            Ciudad = direccion.Ciudad,
            Provincia = direccion.Provincia,
            Telefono = direccion.Telefono
        };
    }

    public async Task<bool> Handle(EliminarDireccionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = request.UserId;
        var id = request.Id;
        // Verificar que la dirección pertenece al usuario
        var perfil = await _perfilRepo.GetByUserIdAsync(userId);
        if (perfil == null)
            return false;
        var eliminada = await _perfilRepo.DeleteDireccionAsync(id, perfil.Id);
        if (!eliminada)
            return false;
        _logger.LogInformation("Dirección favorita eliminada: ID {Id} para usuario {UserId}", id, userId);
        return true;
    }
}
