using Nexopostal.Intranet.DTOs;
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
}

public class OficinaPostalService : IOficinaPostalService
{
    private readonly OficinasJsonService _oficinasJson;
    private readonly IClasificacionService _clasificacionService;
    private readonly IOperarioOficinaRepository _operarioOficinaRepo;
    private readonly ILogger<OficinaPostalService> _logger;

    public OficinaPostalService(
        OficinasJsonService oficinasJson,
        IClasificacionService clasificacionService,
        IOperarioOficinaRepository operarioOficinaRepo,
        ILogger<OficinaPostalService> logger)
    {
        _oficinasJson = oficinasJson;
        _clasificacionService = clasificacionService;
        _operarioOficinaRepo = operarioOficinaRepo;
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
}
