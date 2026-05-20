using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio para gestionar oficinas como nodos logísticos.
/// 
/// Las oficinas se cargan desde el JSON estático (Data/oficinas.json),
/// NO desde la base de datos. Este servicio combina los datos del JSON
/// con la resolución de CTA (BD) para el flujo logístico automático.
/// 
/// Flujo de resolución para un envío con CP 28919:
///   1. Buscar oficina más cercana al CP 28919 en el JSON
///   2. Buscar CTA de destino para ese CP en la BD (RutasCta)
///   3. Devolver oficina + CTA combinados
/// </summary>
public interface IOficinaPostalService
{
    /// <summary>Obtiene todas las oficinas del JSON</summary>
    List<OficinaJsonDto> ObtenerTodas();

    /// <summary>Busca oficinas por código postal</summary>
    List<OficinaJsonDto> BuscarPorCodigoPostal(string codigoPostal);

    /// <summary>Busca oficinas por texto libre</summary>
    List<OficinaJsonDto> BuscarPorTexto(string query);

    /// <summary>Obtiene una oficina por su ID del JSON</summary>
    OficinaJsonDto? ObtenerPorId(int id);

    /// <summary>
    /// Resuelve la oficina más cercana + CTA para un código postal.
    /// Este es el método clave del flujo logístico automático:
    ///   CP 28919 → Oficina "NexoPostal Leganés" + CTA-MAD
    /// </summary>
    Task<ResolverOficinaCtaResponseDto?> ResolverOficinaPorCp(string codigoPostal);

    /// <summary>Obtiene los operarios asignados a una oficina</summary>
    Task<List<OperarioOficinaResumenDto>> ObtenerOperariosOficina(int oficinaJsonId);

    /// <summary>Obtiene las oficinas cuyo prefijo de CP coincide con alguna ruta del CTA dado.</summary>
    Task<List<OficinaJsonDto>> ObtenerOficinasPorCta(int ctaId);

    /// <summary>Obtiene la oficina asignada activa al operario autenticado.</summary>
    Task<MiOficinaInfoDto?> ObtenerMiOficina(string identityUserId);

    /// <summary>Obtiene la asignación de oficina (activa o no) de un usuario, vista admin.</summary>
    Task<MiOficinaInfoDto?> ObtenerOficinaAdmin(string identityUserId);

    /// <summary>Crea o cambia la oficina asignada a un operario (acción admin).</summary>
    Task<(bool Ok, string? Error, MiOficinaInfoDto? Resultado)> ActualizarOficinaAdmin(string identityUserId, AdminActualizarOficinaDto dto);
}

public class OficinaPostalService : IOficinaPostalService
{
    private readonly OficinasJsonService _oficinasJson;
    private readonly IClasificacionService _clasificacionService;
    private readonly IOperarioOficinaRepository _operarioOficinaRepo;
    private readonly IRutaCtaRepository _rutaRepo;
    private readonly ILogger<OficinaPostalService> _logger;

    public OficinaPostalService(
        OficinasJsonService oficinasJson,
        IClasificacionService clasificacionService,
        IOperarioOficinaRepository operarioOficinaRepo,
        IRutaCtaRepository rutaRepo,
        ILogger<OficinaPostalService> logger)
    {
        _oficinasJson = oficinasJson;
        _clasificacionService = clasificacionService;
        _operarioOficinaRepo = operarioOficinaRepo;
        _rutaRepo = rutaRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public List<OficinaJsonDto> ObtenerTodas()
    {
        return _oficinasJson.ObtenerTodas();
    }

    /// <inheritdoc />
    public List<OficinaJsonDto> BuscarPorCodigoPostal(string codigoPostal)
    {
        return _oficinasJson.BuscarPorCodigoPostal(codigoPostal);
    }

    /// <inheritdoc />
    public List<OficinaJsonDto> BuscarPorTexto(string query)
    {
        return _oficinasJson.BuscarPorTexto(query);
    }

    /// <inheritdoc />
    public OficinaJsonDto? ObtenerPorId(int id)
    {
        return _oficinasJson.ObtenerPorId(id);
    }

    /// <inheritdoc />
    public async Task<ResolverOficinaCtaResponseDto?> ResolverOficinaPorCp(string codigoPostal)
    {
        // 1. Buscar la oficina más cercana al CP en el JSON
        var oficina = _oficinasJson.ResolverOficinaMasCercana(codigoPostal);
        if (oficina == null)
        {
            _logger.LogWarning(
                "No se encontró oficina para el CP {CP} en el JSON",
                codigoPostal);
            return null;
        }

        // 2. Resolver el CTA que gestiona esta zona por prefijo de CP
        var ctaInfo = await _clasificacionService.ResolverCtaDestino(codigoPostal);
        if (ctaInfo == null)
        {
            _logger.LogWarning(
                "No se encontró CTA para el CP {CP}. Oficina encontrada: {Oficina}",
                codigoPostal, oficina.Nombre);
            return null;
        }

        _logger.LogInformation(
            "CP {CP} → Oficina: {Oficina} (ID {OfId}) → CTA: {Cta}",
            codigoPostal, oficina.Nombre, oficina.Id, ctaInfo.CtaCodigo);

        return new ResolverOficinaCtaResponseDto
        {
            OficinaId = oficina.Id,
            OficinaNombre = oficina.Nombre,
            OficinaCodigoPostal = oficina.CodigoPostal,
            OficinaCiudad = oficina.Ciudad,
            OficinaDireccion = oficina.Direccion,
            CtaId = ctaInfo.CtaId,
            CtaCodigo = ctaInfo.CtaCodigo,
            CtaNombre = ctaInfo.CtaNombre,
            AreaZonal = ctaInfo.Area
        };
    }

    /// <inheritdoc />
    public async Task<List<OperarioOficinaResumenDto>> ObtenerOperariosOficina(int oficinaJsonId)
    {
        var operarios = await _operarioOficinaRepo.GetByOficinaAsync(oficinaJsonId);
        return operarios.Select(o => new OperarioOficinaResumenDto
        {
            Id = o.Id,
            NombreCompleto = o.NombreCompleto,
            CodigoEmpleado = o.CodigoEmpleado,
            Rol = o.Rol.ToString(),
            Activo = o.Activo,
            OficinaJsonId = o.OficinaJsonId,
            OficinaNombre = o.OficinaNombre
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<List<OficinaJsonDto>> ObtenerOficinasPorCta(int ctaId)
    {
        var rutas = await _rutaRepo.GetByCtaIdAsync(ctaId);
        if (rutas.Count == 0) return new List<OficinaJsonDto>();

        var prefijos = rutas
            .Select(r => r.PrefijoCp)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .ToList();

        return _oficinasJson.ObtenerTodas()
            .Where(o => prefijos.Any(p => o.CodigoPostal.StartsWith(p)))
            .OrderBy(o => o.CodigoPostal)
            .ThenBy(o => o.Nombre)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<MiOficinaInfoDto?> ObtenerMiOficina(string identityUserId)
    {
        var operario = await _operarioOficinaRepo.GetByIdentityUserIdAsync(identityUserId);
        return operario == null ? null : MapearMiOficina(operario);
    }

    /// <inheritdoc />
    public async Task<MiOficinaInfoDto?> ObtenerOficinaAdmin(string identityUserId)
    {
        var operario = await _operarioOficinaRepo.GetByIdentityUserIdAnyAsync(identityUserId);
        return operario == null ? null : MapearMiOficina(operario);
    }

    /// <inheritdoc />
    public async Task<(bool Ok, string? Error, MiOficinaInfoDto? Resultado)> ActualizarOficinaAdmin(string identityUserId, AdminActualizarOficinaDto dto)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
            return (false, "IdentityUserId no válido.", null);

        var oficina = _oficinasJson.ObtenerPorId(dto.NuevoOficinaJsonId);
        if (oficina == null)
            return (false, $"Oficina con ID {dto.NuevoOficinaJsonId} no encontrada en el catálogo.", null);

        var existente = await _operarioOficinaRepo.GetByIdentityUserIdAnyAsync(identityUserId);

        if (existente == null)
        {
            if (string.IsNullOrWhiteSpace(dto.NombreCompleto)
                || string.IsNullOrWhiteSpace(dto.CodigoEmpleado))
            {
                return (false,
                    "El usuario no tiene asignación previa de oficina. Para crear la primera se requieren NombreCompleto y CodigoEmpleado.",
                    null);
            }

            var rolOp = RolOperario.OperarioOficina;
            if (!string.IsNullOrWhiteSpace(dto.Rol)
                && !Enum.TryParse<RolOperario>(dto.Rol, true, out rolOp))
            {
                return (false, $"Rol operativo no válido: {dto.Rol}.", null);
            }

            var nueva = new OperarioOficina
            {
                IdentityUserId = identityUserId,
                NombreCompleto = dto.NombreCompleto!,
                CodigoEmpleado = dto.CodigoEmpleado!,
                Rol = rolOp,
                OficinaJsonId = oficina.Id,
                OficinaNombre = oficina.Nombre,
                Activo = true,
                FechaAsignacion = DateTime.UtcNow
            };
            await _operarioOficinaRepo.CreateAsync(nueva);

            _logger.LogInformation(
                "Admin asignó por primera vez la oficina {Oficina} ({OficinaId}) al usuario {IdentityUserId}",
                oficina.Nombre, oficina.Id, identityUserId);

            return (true, null, MapearMiOficina(nueva));
        }

        existente.OficinaJsonId = oficina.Id;
        existente.OficinaNombre = oficina.Nombre;
        existente.Activo = true;
        existente.FechaAsignacion = DateTime.UtcNow;
        await _operarioOficinaRepo.UpdateAsync(existente);

        _logger.LogInformation(
            "Admin cambió la oficina del usuario {IdentityUserId} a {Oficina} ({OficinaId})",
            identityUserId, oficina.Nombre, oficina.Id);

        return (true, null, MapearMiOficina(existente));
    }

    private MiOficinaInfoDto MapearMiOficina(OperarioOficina operario)
    {
        var oficina = _oficinasJson.ObtenerPorId(operario.OficinaJsonId);
        return new MiOficinaInfoDto
        {
            OficinaJsonId = operario.OficinaJsonId,
            OficinaNombre = operario.OficinaNombre,
            CodigoPostal = oficina?.CodigoPostal ?? string.Empty,
            Ciudad = oficina?.Ciudad ?? string.Empty,
            Direccion = oficina?.Direccion ?? string.Empty,
            Rol = operario.Rol.ToString(),
            Activo = operario.Activo,
            FechaAsignacion = operario.FechaAsignacion
        };
    }
}
