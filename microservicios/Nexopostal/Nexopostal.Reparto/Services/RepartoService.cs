using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;

namespace Nexopostal.Reparto.Services;

// ============================================================
//  Interfaz del servicio de Reparto
// ============================================================
public interface IRepartoService
{
    // ─── Repartidores ───
    Task<List<RepartidorResumenDto>> ObtenerRepartidores(int? oficinaJsonId = null);
    Task<RepartidorResumenDto?> ObtenerRepartidorPorIdentityId(string identityUserId);
    Task<RepartidorResumenDto> CrearRepartidor(CrearRepartidorDto dto);

    // ─── Rutas ───
    Task<List<RutaRepartoResumenDto>> ObtenerRutas(DateOnly? fecha = null, int? repartidorId = null);
    Task<RutaRepartoDetalleDto?> ObtenerRutaPorId(int id);
    Task<RutaRepartoDetalleDto?> ObtenerRutaPorCodigo(string codigo);
    Task<RutaRepartoDetalleDto> CrearRuta(CrearRutaRepartoDto dto);
    Task<RutaRepartoDetalleDto?> IniciarRuta(int rutaId);
    Task<RutaRepartoDetalleDto?> FinalizarRuta(int rutaId, string? observaciones = null);

    // ─── Entregas ───
    Task<EntregaPaqueteDto?> AgregarEntregaARuta(int rutaId, AgregarEntregaDto dto);
    Task<EntregaPaqueteDto?> RegistrarEntrega(int entregaId, RegistrarEntregaDto dto);
    Task<List<EntregaPaqueteDto>> ObtenerEntregasPorRuta(int rutaId);
    Task<List<EntregaPaqueteDto>> ObtenerEntregasPorSeguimiento(string numeroSeguimiento);

    // ─── Dashboard ───
    Task<DashboardRepartoDto> ObtenerDashboard(int? oficinaJsonId = null);
}

// ============================================================
//  Implementación del servicio de Reparto
// ============================================================
public class RepartoService : IRepartoService
{
    private readonly IRepartidorRepository _repartidorRepo;
    private readonly IRutaRepartoRepository _rutaRepo;
    private readonly IEntregaPaqueteRepository _entregaRepo;
    private readonly ILogger<RepartoService> _logger;

    public RepartoService(
        IRepartidorRepository repartidorRepo,
        IRutaRepartoRepository rutaRepo,
        IEntregaPaqueteRepository entregaRepo,
        ILogger<RepartoService> logger)
    {
        _repartidorRepo = repartidorRepo;
        _rutaRepo = rutaRepo;
        _entregaRepo = entregaRepo;
        _logger = logger;
    }

    // ═══════════════════════════════════════════
    //  REPARTIDORES
    // ═══════════════════════════════════════════

    public async Task<List<RepartidorResumenDto>> ObtenerRepartidores(int? oficinaJsonId = null)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var repartidores = await _repartidorRepo.GetAllAsync(oficinaJsonId);

        return repartidores.Select(r => new RepartidorResumenDto
        {
            Id = r.Id,
            NombreCompleto = r.NombreCompleto,
            CodigoEmpleado = r.CodigoEmpleado,
            Telefono = r.Telefono,
            OficinaJsonId = r.OficinaJsonId,
            OficinaNombre = r.OficinaNombre,
            TipoVehiculo = r.TipoVehiculo.ToString(),
            Activo = r.Activo,
            RutasHoy = r.Rutas.Count(rt => rt.FechaReparto == hoy)
        }).ToList();
    }

    public async Task<RepartidorResumenDto?> ObtenerRepartidorPorIdentityId(string identityUserId)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var r = await _repartidorRepo.GetByIdentityUserIdAsync(identityUserId);
        if (r == null) return null;

        return new RepartidorResumenDto
        {
            Id = r.Id,
            NombreCompleto = r.NombreCompleto,
            CodigoEmpleado = r.CodigoEmpleado,
            Telefono = r.Telefono,
            OficinaJsonId = r.OficinaJsonId,
            OficinaNombre = r.OficinaNombre,
            TipoVehiculo = r.TipoVehiculo.ToString(),
            Activo = r.Activo,
            RutasHoy = r.Rutas.Count(rt => rt.FechaReparto == hoy)
        };
    }

    public async Task<RepartidorResumenDto> CrearRepartidor(CrearRepartidorDto dto)
    {
        var repartidor = new Repartidor
        {
            IdentityUserId = dto.IdentityUserId,
            NombreCompleto = dto.NombreCompleto,
            CodigoEmpleado = dto.CodigoEmpleado,
            Telefono = dto.Telefono,
            OficinaJsonId = dto.OficinaJsonId,
            OficinaNombre = dto.OficinaNombre,
            TipoVehiculo = Enum.Parse<TipoVehiculo>(dto.TipoVehiculo, ignoreCase: true),
            MatriculaVehiculo = dto.MatriculaVehiculo
        };

        await _repartidorRepo.CreateAsync(repartidor);

        _logger.LogInformation("Repartidor creado: {Codigo} - {Nombre}", repartidor.CodigoEmpleado, repartidor.NombreCompleto);

        return new RepartidorResumenDto
        {
            Id = repartidor.Id,
            NombreCompleto = repartidor.NombreCompleto,
            CodigoEmpleado = repartidor.CodigoEmpleado,
            Telefono = repartidor.Telefono,
            OficinaJsonId = repartidor.OficinaJsonId,
            OficinaNombre = repartidor.OficinaNombre,
            TipoVehiculo = repartidor.TipoVehiculo.ToString(),
            Activo = repartidor.Activo,
            RutasHoy = 0
        };
    }

    // ═══════════════════════════════════════════
    //  RUTAS DE REPARTO
    // ═══════════════════════════════════════════

    public async Task<List<RutaRepartoResumenDto>> ObtenerRutas(DateOnly? fecha = null, int? repartidorId = null)
    {
        var rutas = await _rutaRepo.GetAllAsync(fecha, repartidorId);

        return rutas.Select(r => new RutaRepartoResumenDto
        {
            Id = r.Id,
            Codigo = r.Codigo,
            FechaReparto = r.FechaReparto.ToString("yyyy-MM-dd"),
            RepartidorNombre = r.Repartidor.NombreCompleto,
            OficinaOrigenNombre = r.OficinaOrigenNombre,
            Estado = r.Estado.ToString(),
            TotalEntregas = r.Entregas.Count,
            Entregados = r.Entregas.Count(e => e.Estado == EstadoEntrega.Entregado || e.Estado == EstadoEntrega.EntregadoPuntoAlternativo),
            Pendientes = r.Entregas.Count(e => e.Estado == EstadoEntrega.Pendiente || e.Estado == EstadoEntrega.EnCamino),
            Fallidos = r.Entregas.Count(e => e.Estado == EstadoEntrega.Ausente || e.Estado == EstadoEntrega.DireccionIncorrecta || e.Estado == EstadoEntrega.Rechazado)
        }).ToList();
    }

    public async Task<RutaRepartoDetalleDto?> ObtenerRutaPorId(int id)
    {
        var ruta = await _rutaRepo.GetByIdAsync(id);
        return ruta == null ? null : MapearRutaDetalle(ruta);
    }

    public async Task<RutaRepartoDetalleDto?> ObtenerRutaPorCodigo(string codigo)
    {
        var ruta = await _rutaRepo.GetByCodigoAsync(codigo);
        return ruta == null ? null : MapearRutaDetalle(ruta);
    }

    public async Task<RutaRepartoDetalleDto> CrearRuta(CrearRutaRepartoDto dto)
    {
        var fecha = DateOnly.Parse(dto.FechaReparto);

        // Generar código secuencial: REP-20250601-001
        var rutasDelDia = await _rutaRepo.CountByFechaAsync(fecha);
        var codigo = $"REP-{fecha:yyyyMMdd}-{(rutasDelDia + 1):D3}";

        var ruta = new RutaReparto
        {
            Codigo = codigo,
            FechaReparto = fecha,
            RepartidorId = dto.RepartidorId,
            OficinaOrigenJsonId = dto.OficinaOrigenJsonId,
            OficinaOrigenNombre = dto.OficinaOrigenNombre,
            Observaciones = dto.Observaciones
        };

        await _rutaRepo.CreateAsync(ruta);

        _logger.LogInformation("Ruta de reparto creada: {Codigo}", codigo);

        // Recargar con includes
        return (await ObtenerRutaPorId(ruta.Id))!;
    }

    public async Task<RutaRepartoDetalleDto?> IniciarRuta(int rutaId)
    {
        var ruta = await _rutaRepo.GetWithEntregasAsync(rutaId);

        if (ruta == null) return null;

        if (ruta.Estado != EstadoRuta.Planificada)
        {
            _logger.LogWarning("Intento de iniciar ruta {Codigo} en estado {Estado}", ruta.Codigo, ruta.Estado);
            return null;
        }

        ruta.Estado = EstadoRuta.EnCurso;
        ruta.HoraSalida = DateTime.UtcNow;

        // Marcar entregas como "en camino"
        foreach (var entrega in ruta.Entregas.Where(e => e.Estado == EstadoEntrega.Pendiente))
        {
            entrega.Estado = EstadoEntrega.EnCamino;
        }

        await _rutaRepo.UpdateAsync(ruta);

        _logger.LogInformation("Ruta {Codigo} iniciada", ruta.Codigo);

        return await ObtenerRutaPorId(rutaId);
    }

    public async Task<RutaRepartoDetalleDto?> FinalizarRuta(int rutaId, string? observaciones = null)
    {
        var ruta = await _rutaRepo.GetWithEntregasAsync(rutaId);

        if (ruta == null) return null;

        if (ruta.Estado != EstadoRuta.EnCurso)
        {
            _logger.LogWarning("Intento de finalizar ruta {Codigo} en estado {Estado}", ruta.Codigo, ruta.Estado);
            return null;
        }

        ruta.HoraRegreso = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(observaciones))
            ruta.Observaciones = observaciones;

        // Determinar estado final
        var totalEntregas = ruta.Entregas.Count;
        var entregados = ruta.Entregas.Count(e =>
            e.Estado == EstadoEntrega.Entregado || e.Estado == EstadoEntrega.EntregadoPuntoAlternativo);

        if (entregados == totalEntregas)
            ruta.Estado = EstadoRuta.Completada;
        else if (entregados == 0)
            ruta.Estado = EstadoRuta.Cancelada;
        else
            ruta.Estado = EstadoRuta.CompletadaParcial;

        // Paquetes aún en camino → devolver a oficina
        foreach (var entrega in ruta.Entregas.Where(e => e.Estado == EstadoEntrega.EnCamino))
        {
            entrega.Estado = EstadoEntrega.DevueltoAOficina;
        }

        await _rutaRepo.UpdateAsync(ruta);

        _logger.LogInformation("Ruta {Codigo} finalizada con estado {Estado} ({Entregados}/{Total})",
            ruta.Codigo, ruta.Estado, entregados, totalEntregas);

        return await ObtenerRutaPorId(rutaId);
    }

    // ═══════════════════════════════════════════
    //  ENTREGAS
    // ═══════════════════════════════════════════

    public async Task<EntregaPaqueteDto?> AgregarEntregaARuta(int rutaId, AgregarEntregaDto dto)
    {
        var ruta = await _rutaRepo.GetWithEntregasAsync(rutaId);

        if (ruta == null) return null;

        if (ruta.Estado != EstadoRuta.Planificada)
        {
            _logger.LogWarning("No se puede agregar entregas a ruta {Codigo} en estado {Estado}", ruta.Codigo, ruta.Estado);
            return null;
        }

        var orden = ruta.Entregas.Any() ? ruta.Entregas.Max(e => e.OrdenEnRuta) + 1 : 1;

        // Contar intentos previos para este paquete
        var intentosPrevios = await _entregaRepo.CountByExpedicionAsync(dto.NumeroExpedicion);

        var entrega = new EntregaPaquete
        {
            RutaRepartoId = rutaId,
            NumeroExpedicion = dto.NumeroExpedicion,
            NumeroSeguimiento = dto.NumeroSeguimiento,
            DireccionEntrega = dto.DireccionEntrega,
            CodigoPostal = dto.CodigoPostal,
            Ciudad = dto.Ciudad,
            NombreDestinatario = dto.NombreDestinatario,
            TelefonoDestinatario = dto.TelefonoDestinatario,
            NumeroIntento = intentosPrevios + 1,
            OrdenEnRuta = orden
        };

        await _entregaRepo.CreateAsync(entrega);

        _logger.LogInformation("Entrega agregada a ruta {Codigo}: {Expedicion} (intento {Intento})",
            ruta.Codigo, dto.NumeroExpedicion, entrega.NumeroIntento);

        return MapearEntrega(entrega);
    }

    public async Task<EntregaPaqueteDto?> RegistrarEntrega(int entregaId, RegistrarEntregaDto dto)
    {
        var entrega = await _entregaRepo.GetWithRutaAsync(entregaId);

        if (entrega == null) return null;

        if (!Enum.TryParse<EstadoEntrega>(dto.Estado, ignoreCase: true, out var estado))
        {
            _logger.LogWarning("Estado de entrega inválido: {Estado}", dto.Estado);
            return null;
        }

        entrega.Estado = estado;
        entrega.FechaIntento = DateTime.UtcNow;
        entrega.ReceptorNombre = dto.ReceptorNombre;
        entrega.ReceptorDni = dto.ReceptorDni;
        entrega.Observaciones = dto.Observaciones;
        entrega.LatitudEntrega = dto.Latitud;
        entrega.LongitudEntrega = dto.Longitud;
        entrega.FirmaDigital = dto.FirmaDigital;

        await _entregaRepo.UpdateAsync(entrega);

        _logger.LogInformation("Entrega {Id} registrada como {Estado} para expedición {Expedicion}",
            entregaId, estado, entrega.NumeroExpedicion);

        return MapearEntrega(entrega);
    }

    public async Task<List<EntregaPaqueteDto>> ObtenerEntregasPorRuta(int rutaId)
    {
        var entregas = await _entregaRepo.GetByRutaAsync(rutaId);

        return entregas.Select(e => new EntregaPaqueteDto
        {
            Id = e.Id,
            NumeroExpedicion = e.NumeroExpedicion,
            NumeroSeguimiento = e.NumeroSeguimiento,
            DireccionEntrega = e.DireccionEntrega,
            CodigoPostal = e.CodigoPostal,
            Ciudad = e.Ciudad,
            NombreDestinatario = e.NombreDestinatario,
            NumeroIntento = e.NumeroIntento,
            OrdenEnRuta = e.OrdenEnRuta,
            Estado = e.Estado.ToString(),
            FechaIntento = e.FechaIntento,
            ReceptorNombre = e.ReceptorNombre,
            Observaciones = e.Observaciones
        }).ToList();
    }

    public async Task<List<EntregaPaqueteDto>> ObtenerEntregasPorSeguimiento(string numeroSeguimiento)
    {
        var entregas = await _entregaRepo.GetBySeguimientoAsync(numeroSeguimiento);

        return entregas.Select(e => new EntregaPaqueteDto
        {
            Id = e.Id,
            NumeroExpedicion = e.NumeroExpedicion,
            NumeroSeguimiento = e.NumeroSeguimiento,
            DireccionEntrega = e.DireccionEntrega,
            CodigoPostal = e.CodigoPostal,
            Ciudad = e.Ciudad,
            NombreDestinatario = e.NombreDestinatario,
            NumeroIntento = e.NumeroIntento,
            OrdenEnRuta = e.OrdenEnRuta,
            Estado = e.Estado.ToString(),
            FechaIntento = e.FechaIntento,
            ReceptorNombre = e.ReceptorNombre,
            Observaciones = e.Observaciones
        }).ToList();
    }

    // ═══════════════════════════════════════════
    //  DASHBOARD
    // ═══════════════════════════════════════════

    public async Task<DashboardRepartoDto> ObtenerDashboard(int? oficinaJsonId = null)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var rutasHoy = await _rutaRepo.GetByFechaAsync(hoy, oficinaJsonId);
        var rutaIds = rutasHoy.Select(r => r.Id).ToList();

        var entregas = await _entregaRepo.GetByRutaIdsAsync(rutaIds);

        var completadas = entregas.Count(e => e.Estado == EstadoEntrega.Entregado || e.Estado == EstadoEntrega.EntregadoPuntoAlternativo);
        var fallidas = entregas.Count(e => e.Estado == EstadoEntrega.Ausente || e.Estado == EstadoEntrega.DireccionIncorrecta || e.Estado == EstadoEntrega.Rechazado);
        var pendientes = entregas.Count(e => e.Estado == EstadoEntrega.Pendiente || e.Estado == EstadoEntrega.EnCamino);

        var repartidoresActivos = rutasHoy
            .Where(r => r.Estado == EstadoRuta.EnCurso)
            .Select(r => r.RepartidorId)
            .Distinct()
            .Count();

        var totalIntentos = completadas + fallidas;
        var tasaExito = totalIntentos > 0 ? Math.Round((double)completadas / totalIntentos * 100, 1) : 0;

        return new DashboardRepartoDto
        {
            RutasHoy = rutasHoy.Count,
            RutasEnCurso = rutasHoy.Count(r => r.Estado == EstadoRuta.EnCurso),
            EntregasPendientes = pendientes,
            EntregasCompletadas = completadas,
            EntregasFallidas = fallidas,
            RepartidoresActivos = repartidoresActivos,
            TasaEntregaExitosa = tasaExito
        };
    }

    // ═══════════════════════════════════════════
    //  HELPERS PRIVADOS
    // ═══════════════════════════════════════════

    private static RutaRepartoDetalleDto MapearRutaDetalle(RutaReparto r)
    {
        return new RutaRepartoDetalleDto
        {
            Id = r.Id,
            Codigo = r.Codigo,
            FechaReparto = r.FechaReparto.ToString("yyyy-MM-dd"),
            RepartidorId = r.RepartidorId,
            RepartidorNombre = r.Repartidor.NombreCompleto,
            OficinaOrigenJsonId = r.OficinaOrigenJsonId,
            OficinaOrigenNombre = r.OficinaOrigenNombre,
            Estado = r.Estado.ToString(),
            HoraSalida = r.HoraSalida,
            HoraRegreso = r.HoraRegreso,
            Observaciones = r.Observaciones,
            Entregas = r.Entregas
                .OrderBy(e => e.OrdenEnRuta)
                .Select(e => new EntregaPaqueteDto
                {
                    Id = e.Id,
                    NumeroExpedicion = e.NumeroExpedicion,
                    NumeroSeguimiento = e.NumeroSeguimiento,
                    DireccionEntrega = e.DireccionEntrega,
                    CodigoPostal = e.CodigoPostal,
                    Ciudad = e.Ciudad,
                    NombreDestinatario = e.NombreDestinatario,
                    NumeroIntento = e.NumeroIntento,
                    OrdenEnRuta = e.OrdenEnRuta,
                    Estado = e.Estado.ToString(),
                    FechaIntento = e.FechaIntento,
                    ReceptorNombre = e.ReceptorNombre,
                    Observaciones = e.Observaciones
                })
                .ToList()
        };
    }

    private static EntregaPaqueteDto MapearEntrega(EntregaPaquete e)
    {
        return new EntregaPaqueteDto
        {
            Id = e.Id,
            NumeroExpedicion = e.NumeroExpedicion,
            NumeroSeguimiento = e.NumeroSeguimiento,
            DireccionEntrega = e.DireccionEntrega,
            CodigoPostal = e.CodigoPostal,
            Ciudad = e.Ciudad,
            NombreDestinatario = e.NombreDestinatario,
            NumeroIntento = e.NumeroIntento,
            OrdenEnRuta = e.OrdenEnRuta,
            Estado = e.Estado.ToString(),
            FechaIntento = e.FechaIntento,
            ReceptorNombre = e.ReceptorNombre,
            Observaciones = e.Observaciones
        };
    }
}
