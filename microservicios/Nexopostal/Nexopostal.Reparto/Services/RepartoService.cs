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
    Task<List<RepartidorResumenDto>> ObtenerRepartidores(int? oficinaJsonId = null, bool incluirInactivos = false);
    Task<RepartidorResumenDto?> ObtenerRepartidorPorIdentityId(string identityUserId);
    Task<RepartidorResumenDto> CrearRepartidor(CrearRepartidorDto dto);
    Task<(RepartidorResumenDto? Repartidor, string? Error)> EditarRepartidor(int id, EditarRepartidorDto dto);
    Task<(bool Ok, string? Error)> DesactivarRepartidor(int id);
    Task<(bool Ok, string? Error)> ReactivarRepartidor(int id);

    // ─── Rutas ───
    Task<List<RutaRepartoResumenDto>> ObtenerRutas(DateOnly? fecha = null, int? repartidorId = null, int? oficinaJsonId = null);
    Task<RutaRepartoDetalleDto?> ObtenerRutaPorId(int id);
    Task<RutaRepartoDetalleDto?> ObtenerRutaPorCodigo(string codigo);
    Task<RutaRepartoDetalleDto> CrearRuta(CrearRutaRepartoDto dto);
    Task<RutaRepartoDetalleDto?> IniciarRuta(int rutaId);
    Task<RutaRepartoDetalleDto?> FinalizarRuta(int rutaId, string? observaciones = null);
    Task<(bool Ok, string? Error)> CancelarRuta(int rutaId);
    Task<(bool Ok, string? Error)> ReactivarRuta(int rutaId);

    // ─── Entregas ───
    Task<EntregaPaqueteDto?> AgregarEntregaARuta(int rutaId, AgregarEntregaDto dto);
    Task<EntregaPaqueteDto?> RegistrarEntrega(int entregaId, RegistrarEntregaDto dto);
    Task<List<EntregaPaqueteDto>> ObtenerEntregasPorRuta(int rutaId);
    Task<List<EntregaPaqueteDto>> ObtenerEntregasPorSeguimiento(string numeroSeguimiento);
    Task<AutoAsignacionEntregaResultDto> AutoAsignarEntregaDesdeAdmision(AutoAsignacionEntregaDesdeAdmisionDto dto);

    // ─── Dashboard ───
    Task<DashboardRepartoDto> ObtenerDashboard(int? oficinaJsonId = null);

    // ─── Tracking en tiempo real (JefeReparto) ───
    Task RegistrarUbicacionRepartidor(string identityUserId, double latitud, double longitud, int? rutaActivaId);
    Task<List<UbicacionActivaDto>> ObtenerUbicacionesActivas(int? oficinaJsonId = null, int ventanaMinutos = 10);

    // ─── Asignación manual de paradas (JefeReparto) ───
    Task<List<EntregaPendienteAsignacionDto>> ObtenerEntregasPendientesAsignacion(int? oficinaJsonId = null);
    Task<EntregaPaqueteDto?> ReasignarEntregaARuta(int entregaId, int nuevaRutaId);
}

// ============================================================
//  Implementación del servicio de Reparto
// ============================================================
public class RepartoService : IRepartoService
{
    private readonly IRepartidorRepository _repartidorRepo;
    private readonly IRutaRepartoRepository _rutaRepo;
    private readonly IEntregaPaqueteRepository _entregaRepo;
    private readonly IUbicacionRepartidorRepository _ubicacionRepo;
    private readonly IRepartoNotifier _notifier;
    private readonly IVehiculoService _vehiculoService;
    private readonly IAuthUserSyncService _authSync;
    private readonly ILogger<RepartoService> _logger;

    public RepartoService(
        IRepartidorRepository repartidorRepo,
        IRutaRepartoRepository rutaRepo,
        IEntregaPaqueteRepository entregaRepo,
        IUbicacionRepartidorRepository ubicacionRepo,
        IRepartoNotifier notifier,
        IVehiculoService vehiculoService,
        IAuthUserSyncService authSync,
        ILogger<RepartoService> logger)
    {
        _repartidorRepo = repartidorRepo;
        _rutaRepo = rutaRepo;
        _entregaRepo = entregaRepo;
        _ubicacionRepo = ubicacionRepo;
        _notifier = notifier;
        _vehiculoService = vehiculoService;
        _authSync = authSync;
        _logger = logger;
    }

    // ═══════════════════════════════════════════
    //  REPARTIDORES
    // ═══════════════════════════════════════════

    public async Task<List<RepartidorResumenDto>> ObtenerRepartidores(int? oficinaJsonId = null, bool incluirInactivos = false)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var repartidores = await _repartidorRepo.GetAllAsync(oficinaJsonId, incluirInactivos);

        return repartidores.Select(r => new RepartidorResumenDto
        {
            Id = r.Id,
            IdentityUserId = r.IdentityUserId,
            NombreCompleto = r.NombreCompleto,
            CodigoEmpleado = r.CodigoEmpleado,
            Rol = r.Rol,
            Telefono = r.Telefono,
            OficinaJsonId = r.OficinaJsonId,
            OficinaNombre = r.OficinaNombre,
            TipoVehiculo = r.TipoVehiculo.ToString(),
            MatriculaVehiculo = r.MatriculaVehiculo,
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
            IdentityUserId = r.IdentityUserId,
            NombreCompleto = r.NombreCompleto,
            CodigoEmpleado = r.CodigoEmpleado,
            Rol = r.Rol,
            Telefono = r.Telefono,
            OficinaJsonId = r.OficinaJsonId,
            OficinaNombre = r.OficinaNombre,
            TipoVehiculo = r.TipoVehiculo.ToString(),
            MatriculaVehiculo = r.MatriculaVehiculo,
            Activo = r.Activo,
            RutasHoy = r.Rutas.Count(rt => rt.FechaReparto == hoy)
        };
    }

    public async Task<RepartidorResumenDto> CrearRepartidor(CrearRepartidorDto dto)
    {
        var rolNormalizado = string.Equals(dto.Rol, "JefeReparto", StringComparison.OrdinalIgnoreCase)
            ? "JefeReparto"
            : "Repartidor";

        var repartidor = new Repartidor
        {
            IdentityUserId = dto.IdentityUserId,
            NombreCompleto = dto.NombreCompleto,
            CodigoEmpleado = dto.CodigoEmpleado,
            Rol = rolNormalizado,
            Telefono = dto.Telefono,
            OficinaJsonId = dto.OficinaJsonId,
            OficinaNombre = dto.OficinaNombre,
            TipoVehiculo = Enum.Parse<TipoVehiculo>(dto.TipoVehiculo, ignoreCase: true),
            MatriculaVehiculo = dto.MatriculaVehiculo
        };

        await _repartidorRepo.CreateAsync(repartidor);

        _logger.LogInformation("Repartidor creado: {Codigo} - {Nombre} ({Rol})", repartidor.CodigoEmpleado, repartidor.NombreCompleto, repartidor.Rol);

        return new RepartidorResumenDto
        {
            Id = repartidor.Id,
            IdentityUserId = repartidor.IdentityUserId,
            NombreCompleto = repartidor.NombreCompleto,
            CodigoEmpleado = repartidor.CodigoEmpleado,
            Rol = repartidor.Rol,
            Telefono = repartidor.Telefono,
            OficinaJsonId = repartidor.OficinaJsonId,
            OficinaNombre = repartidor.OficinaNombre,
            TipoVehiculo = repartidor.TipoVehiculo.ToString(),
            MatriculaVehiculo = repartidor.MatriculaVehiculo,
            Activo = repartidor.Activo,
            RutasHoy = 0
        };
    }

    public async Task<(RepartidorResumenDto? Repartidor, string? Error)> EditarRepartidor(int id, EditarRepartidorDto dto)
    {
        var repartidor = await _repartidorRepo.GetByIdAsync(id);
        if (repartidor == null)
            return (null, "Repartidor no encontrado.");

        if (!Enum.TryParse<TipoVehiculo>(dto.TipoVehiculo, ignoreCase: true, out var tipo))
            return (null, $"Tipo de vehículo no válido: {dto.TipoVehiculo}.");

        var nombreAnterior = repartidor.NombreCompleto;
        repartidor.NombreCompleto    = string.IsNullOrWhiteSpace(dto.NombreCompleto) ? repartidor.NombreCompleto : dto.NombreCompleto.Trim();
        repartidor.Telefono          = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
        repartidor.OficinaJsonId     = dto.OficinaJsonId;
        repartidor.OficinaNombre     = dto.OficinaNombre?.Trim() ?? string.Empty;
        repartidor.TipoVehiculo      = tipo;
        repartidor.MatriculaVehiculo = string.IsNullOrWhiteSpace(dto.MatriculaVehiculo) ? null : dto.MatriculaVehiculo.Trim().ToUpperInvariant();

        await _repartidorRepo.UpdateAsync(repartidor);

        // Sincronizar nombre con Auth si cambió, para que el próximo JWT lo refleje.
        if (!string.Equals(repartidor.NombreCompleto, nombreAnterior, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(repartidor.IdentityUserId))
        {
            await _authSync.SincronizarNombreAsync(repartidor.IdentityUserId, repartidor.NombreCompleto);
        }

        // Si se especificó un vehículo de la flota, actualizar su asignación en la tabla de vehículos.
        // VehiculoId == 0 significa desasignar; VehiculoId > 0 significa asignar ese vehículo.
        if (dto.VehiculoId.HasValue)
        {
            int? repartidorParaAsignar = dto.VehiculoId.Value == 0 ? null : repartidor.Id;
            int vehiculoIdEfectivo = dto.VehiculoId.Value == 0
                ? (await _vehiculoService.ListarAsync(false, repartidor.OficinaJsonId))
                    .FirstOrDefault(v => v.RepartidorAsignadoId == repartidor.Id)?.Id ?? 0
                : dto.VehiculoId.Value;

            if (vehiculoIdEfectivo > 0)
                await _vehiculoService.AsignarAsync(vehiculoIdEfectivo, repartidorParaAsignar, null);
        }

        _logger.LogInformation("Repartidor {Id} actualizado por administrador", id);

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return (new RepartidorResumenDto
        {
            Id = repartidor.Id,
            IdentityUserId = repartidor.IdentityUserId,
            NombreCompleto = repartidor.NombreCompleto,
            CodigoEmpleado = repartidor.CodigoEmpleado,
            Rol = repartidor.Rol,
            Telefono = repartidor.Telefono,
            OficinaJsonId = repartidor.OficinaJsonId,
            OficinaNombre = repartidor.OficinaNombre,
            TipoVehiculo = repartidor.TipoVehiculo.ToString(),
            MatriculaVehiculo = repartidor.MatriculaVehiculo,
            Activo = repartidor.Activo,
            RutasHoy = repartidor.Rutas.Count(rt => rt.FechaReparto == hoy)
        }, null);
    }

    public async Task<(bool Ok, string? Error)> DesactivarRepartidor(int id)
    {
        var repartidor = await _repartidorRepo.GetByIdAsync(id);
        if (repartidor == null)
            return (false, "Repartidor no encontrado.");

        if (!repartidor.Activo)
            return (true, null); // idempotente

        if (await _repartidorRepo.TieneRutasActivasAsync(id))
            return (false, "No se puede desactivar: el repartidor tiene rutas planificadas o en curso. Reasigna o cancela esas rutas primero.");

        repartidor.Activo = false;
        await _repartidorRepo.UpdateAsync(repartidor);

        _logger.LogInformation("Repartidor {Id} desactivado", id);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ReactivarRepartidor(int id)
    {
        var repartidor = await _repartidorRepo.GetByIdAsync(id);
        if (repartidor == null)
            return (false, "Repartidor no encontrado.");

        if (repartidor.Activo)
            return (true, null);

        repartidor.Activo = true;
        await _repartidorRepo.UpdateAsync(repartidor);

        _logger.LogInformation("Repartidor {Id} reactivado", id);
        return (true, null);
    }

    // ═══════════════════════════════════════════
    //  RUTAS DE REPARTO
    // ═══════════════════════════════════════════

    public async Task<List<RutaRepartoResumenDto>> ObtenerRutas(DateOnly? fecha = null, int? repartidorId = null, int? oficinaJsonId = null)
    {
        var rutas = await _rutaRepo.GetAllAsync(fecha, repartidorId, oficinaJsonId);

        return rutas.Select(r => new RutaRepartoResumenDto
        {
            Id = r.Id,
            Codigo = r.Codigo,
            FechaReparto = r.FechaReparto.ToString("yyyy-MM-dd"),
            RepartidorNombre = r.Repartidor?.NombreCompleto ?? string.Empty,
            OficinaOrigenJsonId = r.OficinaOrigenJsonId,
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
        var detalle = await ObtenerRutaPorId(ruta.Id)
            ?? throw new InvalidOperationException($"No se pudo recuperar la ruta recién creada con ID {ruta.Id}.");

        // Notificar al repartidor vía SignalR
        var repartidor = await _repartidorRepo.GetByIdAsync(dto.RepartidorId);
        if (repartidor != null && !string.IsNullOrEmpty(repartidor.IdentityUserId))
        {
            await _notifier.NotificarRepartidorAsync(repartidor.IdentityUserId, "RutaAsignada", new
            {
                rutaId = detalle.Id,
                codigo = detalle.Codigo,
                fechaReparto = detalle.FechaReparto,
                mensaje = $"Tienes una nueva ruta asignada: {detalle.Codigo}"
            });
        }

        return detalle;
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

    public async Task<(bool Ok, string? Error)> CancelarRuta(int rutaId)
    {
        var ruta = await _rutaRepo.GetWithEntregasAsync(rutaId);
        if (ruta == null)
            return (false, "Ruta no encontrada");

        if (ruta.Estado == EstadoRuta.EnCurso)
            return (false, "No se puede cancelar una ruta en curso. Finalízala primero.");

        if (ruta.Estado == EstadoRuta.Cancelada)
            return (false, "La ruta ya está cancelada.");

        if (ruta.Estado == EstadoRuta.Completada || ruta.Estado == EstadoRuta.CompletadaParcial)
            return (false, "No se puede cancelar una ruta completada.");

        ruta.Estado = EstadoRuta.Cancelada;

        // Devolver entregas pendientes a estado Pendiente (para poder ser reasignadas)
        foreach (var entrega in ruta.Entregas.Where(e => e.Estado == EstadoEntrega.Pendiente || e.Estado == EstadoEntrega.EnCamino))
            entrega.Estado = EstadoEntrega.DevueltoAOficina;

        await _rutaRepo.UpdateAsync(ruta);
        _logger.LogInformation("Ruta {Codigo} cancelada manualmente", ruta.Codigo);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ReactivarRuta(int rutaId)
    {
        var ruta = await _rutaRepo.GetWithEntregasAsync(rutaId);
        if (ruta == null)
            return (false, "Ruta no encontrada");

        if (ruta.Estado != EstadoRuta.Cancelada)
            return (false, "Solo se pueden reactivar rutas en estado Cancelada.");

        ruta.Estado = EstadoRuta.Planificada;

        // Restaurar entregas devueltas a Pendiente
        foreach (var entrega in ruta.Entregas.Where(e => e.Estado == EstadoEntrega.DevueltoAOficina))
            entrega.Estado = EstadoEntrega.Pendiente;

        await _rutaRepo.UpdateAsync(ruta);
        _logger.LogInformation("Ruta {Codigo} reactivada a Planificada", ruta.Codigo);
        return (true, null);
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
        entrega.FotoEntrega = dto.FotoEntrega;

        await _entregaRepo.UpdateAsync(entrega);

        _logger.LogInformation("Entrega {Id} registrada como {Estado} para expedición {Expedicion}",
            entregaId, estado, entrega.NumeroExpedicion);

        // Notificar al repartidor confirmando registro
        if (entrega.RutaReparto != null)
        {
            var repartidor = await _repartidorRepo.GetByIdAsync(entrega.RutaReparto.RepartidorId);
            if (repartidor != null && !string.IsNullOrEmpty(repartidor.IdentityUserId))
            {
                await _notifier.NotificarRepartidorAsync(repartidor.IdentityUserId, "EntregaRegistrada", new
                {
                    entregaId = entrega.Id,
                    numeroSeguimiento = entrega.NumeroSeguimiento,
                    estado = estado.ToString()
                });
            }
        }

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
            TelefonoDestinatario = e.TelefonoDestinatario,
            NumeroIntento = e.NumeroIntento,
            OrdenEnRuta = e.OrdenEnRuta,
            Estado = e.Estado.ToString(),
            FechaIntento = e.FechaIntento,
            ReceptorNombre = e.ReceptorNombre,
            ReceptorDni = e.ReceptorDni,
            Observaciones = e.Observaciones,
            LatitudEntrega = e.LatitudEntrega,
            LongitudEntrega = e.LongitudEntrega,
            FirmaDigital = e.FirmaDigital,
            FotoEntrega = e.FotoEntrega
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
            TelefonoDestinatario = e.TelefonoDestinatario,
            NumeroIntento = e.NumeroIntento,
            OrdenEnRuta = e.OrdenEnRuta,
            Estado = e.Estado.ToString(),
            FechaIntento = e.FechaIntento,
            ReceptorNombre = e.ReceptorNombre,
            ReceptorDni = e.ReceptorDni,
            Observaciones = e.Observaciones,
            LatitudEntrega = e.LatitudEntrega,
            LongitudEntrega = e.LongitudEntrega,
            FirmaDigital = e.FirmaDigital,
            FotoEntrega = e.FotoEntrega
        }).ToList();
    }

    public async Task<AutoAsignacionEntregaResultDto> AutoAsignarEntregaDesdeAdmision(AutoAsignacionEntregaDesdeAdmisionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NumeroExpedicion) ||
            string.IsNullOrWhiteSpace(dto.NumeroSeguimiento) ||
            string.IsNullOrWhiteSpace(dto.CodigoPostalDestino) ||
            string.IsNullOrWhiteSpace(dto.DireccionEntrega) ||
            string.IsNullOrWhiteSpace(dto.CiudadDestino) ||
            string.IsNullOrWhiteSpace(dto.NombreDestinatario))
        {
            return new AutoAsignacionEntregaResultDto
            {
                Success = false,
                Message = "Datos insuficientes para auto-asignar la entrega."
            };
        }

        var fechaReparto = ParseFechaReparto(dto.FechaReparto);

        var entregasExistentes = await _entregaRepo.GetByExpedicionAsync(dto.NumeroExpedicion);
        var entregaActiva = entregasExistentes.FirstOrDefault(e =>
            e.RutaReparto.FechaReparto == fechaReparto &&
            (e.Estado == EstadoEntrega.Pendiente || e.Estado == EstadoEntrega.EnCamino));

        if (entregaActiva != null)
        {
            return new AutoAsignacionEntregaResultDto
            {
                Success = true,
                Idempotente = true,
                CreadaRuta = false,
                RutaId = entregaActiva.RutaRepartoId,
                RutaCodigo = entregaActiva.RutaReparto.Codigo,
                RepartidorId = entregaActiva.RutaReparto.RepartidorId,
                EntregaId = entregaActiva.Id,
                Message = "La entrega ya estaba asignada en una ruta activa del día."
            };
        }

        var repartidor = await SeleccionarRepartidor(fechaReparto, dto.OficinaPreferidaJsonId);
        if (repartidor == null)
        {
            return new AutoAsignacionEntregaResultDto
            {
                Success = false,
                Message = "No hay repartidores activos disponibles para auto-asignar la entrega."
            };
        }

        var ruta = await ObtenerRutaPlanificadaDelDia(fechaReparto, repartidor.Id);
        var rutaCreada = false;

        if (ruta == null)
        {
            ruta = await CrearRuta(new CrearRutaRepartoDto
            {
                RepartidorId = repartidor.Id,
                FechaReparto = fechaReparto.ToString("yyyy-MM-dd"),
                OficinaOrigenJsonId = repartidor.OficinaJsonId,
                OficinaOrigenNombre = string.IsNullOrWhiteSpace(repartidor.OficinaNombre)
                    ? dto.OficinaPreferidaNombre ?? $"Oficina {repartidor.OficinaJsonId}"
                    : repartidor.OficinaNombre,
                Observaciones = "Ruta creada automáticamente desde admisión logística"
            });

            rutaCreada = true;
        }

        if (ruta == null)
        {
            return new AutoAsignacionEntregaResultDto
            {
                Success = false,
                Message = "No se pudo crear ni recuperar una ruta planificada para la auto-asignación."
            };
        }

        var entregaExistenteRuta = (await _entregaRepo.GetByRutaAsync(ruta.Id))
            .FirstOrDefault(e => e.NumeroExpedicion == dto.NumeroExpedicion);

        if (entregaExistenteRuta != null)
        {
            return new AutoAsignacionEntregaResultDto
            {
                Success = true,
                Idempotente = true,
                CreadaRuta = rutaCreada,
                RutaId = ruta.Id,
                RutaCodigo = ruta.Codigo,
                RepartidorId = repartidor.Id,
                RepartidorNombre = repartidor.NombreCompleto,
                EntregaId = entregaExistenteRuta.Id,
                Message = "La entrega ya estaba incluida en la ruta seleccionada."
            };
        }

        var entrega = await AgregarEntregaARuta(ruta.Id, new AgregarEntregaDto
        {
            NumeroExpedicion = dto.NumeroExpedicion,
            NumeroSeguimiento = dto.NumeroSeguimiento,
            DireccionEntrega = dto.DireccionEntrega,
            CodigoPostal = dto.CodigoPostalDestino,
            Ciudad = dto.CiudadDestino,
            NombreDestinatario = dto.NombreDestinatario,
            TelefonoDestinatario = dto.TelefonoDestinatario
        });

        if (entrega == null)
        {
            return new AutoAsignacionEntregaResultDto
            {
                Success = false,
                CreadaRuta = rutaCreada,
                RutaId = ruta.Id,
                RutaCodigo = ruta.Codigo,
                RepartidorId = repartidor.Id,
                RepartidorNombre = repartidor.NombreCompleto,
                Message = "No se pudo agregar la entrega a la ruta planificada."
            };
        }

        _logger.LogInformation(
            "Auto-asignación completada: {Expedicion} ({Seguimiento}) -> Ruta {Ruta} (Repartidor {Repartidor})",
            dto.NumeroExpedicion,
            dto.NumeroSeguimiento,
            ruta.Codigo,
            repartidor.CodigoEmpleado);

        return new AutoAsignacionEntregaResultDto
        {
            Success = true,
            CreadaRuta = rutaCreada,
            RutaId = ruta.Id,
            RutaCodigo = ruta.Codigo,
            RepartidorId = repartidor.Id,
            RepartidorNombre = repartidor.NombreCompleto,
            EntregaId = entrega.Id,
            Message = rutaCreada
                ? "Ruta y entrega creadas automáticamente desde admisión."
                : "Entrega asignada automáticamente a una ruta planificada existente."
        };
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

    // ═══════════════════════════════════════════
    //  TRACKING TIEMPO REAL (JefeReparto)
    // ═══════════════════════════════════════════

    public async Task RegistrarUbicacionRepartidor(string identityUserId, double latitud, double longitud, int? rutaActivaId)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return;
        }

        var repartidor = await _repartidorRepo.GetByIdentityUserIdAsync(identityUserId);
        if (repartidor == null)
        {
            _logger.LogWarning("Ubicación recibida de usuario sin perfil de repartidor: {Identity}", identityUserId);
            return;
        }

        await _ubicacionRepo.UpsertAsync(repartidor.Id, latitud, longitud, rutaActivaId);
    }

    public async Task<List<UbicacionActivaDto>> ObtenerUbicacionesActivas(int? oficinaJsonId = null, int ventanaMinutos = 10)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var ubicaciones = await _ubicacionRepo.GetActivasAsync(TimeSpan.FromMinutes(Math.Max(1, ventanaMinutos)), oficinaJsonId);
        if (!ubicaciones.Any()) return new List<UbicacionActivaDto>();

        var rutasHoy = (await _rutaRepo.GetByFechaAsync(hoy)).ToDictionary(r => r.Id);
        var rutasPorRepartidor = await _rutaRepo.GetAllAsync(hoy);
        var enCursoPorRepartidor = rutasPorRepartidor
            .Where(r => r.Estado == EstadoRuta.EnCurso)
            .GroupBy(r => r.RepartidorId)
            .ToDictionary(g => g.Key, g => g.First());

        var ahora = DateTime.UtcNow;
        return ubicaciones.Select(u =>
        {
            RutaReparto? ruta = null;
            if (u.RutaActivaId.HasValue && rutasHoy.TryGetValue(u.RutaActivaId.Value, out var r))
            {
                ruta = r;
            }
            else if (enCursoPorRepartidor.TryGetValue(u.RepartidorId, out var rEnCurso))
            {
                ruta = rEnCurso;
            }

            return new UbicacionActivaDto
            {
                RepartidorId = u.RepartidorId,
                NombreRepartidor = u.Repartidor?.NombreCompleto ?? string.Empty,
                CodigoEmpleado = u.Repartidor?.CodigoEmpleado ?? string.Empty,
                OficinaJsonId = u.Repartidor?.OficinaJsonId ?? 0,
                OficinaNombre = u.Repartidor?.OficinaNombre ?? string.Empty,
                Latitud = u.Latitud,
                Longitud = u.Longitud,
                ActualizadoEn = u.ActualizadoEn,
                SegundosDesdeActualizacion = (int)(ahora - u.ActualizadoEn).TotalSeconds,
                RutaActivaId = ruta?.Id,
                RutaCodigo = ruta?.Codigo,
                RutaEstado = ruta?.Estado.ToString()
            };
        }).ToList();
    }

    // ═══════════════════════════════════════════
    //  ASIGNACIÓN MANUAL DE PARADAS (JefeReparto)
    // ═══════════════════════════════════════════

    public async Task<List<EntregaPendienteAsignacionDto>> ObtenerEntregasPendientesAsignacion(int? oficinaJsonId = null)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var rutas = await _rutaRepo.GetByFechaAsync(hoy, oficinaJsonId);
        var planificadas = rutas.Where(r => r.Estado == EstadoRuta.Planificada).ToList();

        if (!planificadas.Any()) return new List<EntregaPendienteAsignacionDto>();

        var rutaIds = planificadas.Select(r => r.Id).ToList();
        var entregas = await _entregaRepo.GetByRutaIdsAsync(rutaIds);
        var repartidorIds = planificadas.Select(r => r.RepartidorId).Distinct().ToList();
        var repartidoresAll = await _repartidorRepo.GetAllAsync();
        var repartidoresMap = repartidoresAll.Where(r => repartidorIds.Contains(r.Id)).ToDictionary(r => r.Id);
        var rutaMap = planificadas.ToDictionary(r => r.Id);

        return entregas
            .Where(e => e.Estado == EstadoEntrega.Pendiente)
            .Select(e =>
            {
                var ruta = rutaMap[e.RutaRepartoId];
                repartidoresMap.TryGetValue(ruta.RepartidorId, out var rep);
                return new EntregaPendienteAsignacionDto
                {
                    EntregaId = e.Id,
                    NumeroExpedicion = e.NumeroExpedicion,
                    NumeroSeguimiento = e.NumeroSeguimiento,
                    DireccionEntrega = e.DireccionEntrega,
                    CodigoPostal = e.CodigoPostal,
                    Ciudad = e.Ciudad,
                    NombreDestinatario = e.NombreDestinatario,
                    RutaActualId = ruta.Id,
                    RutaActualCodigo = ruta.Codigo,
                    RepartidorActualId = ruta.RepartidorId,
                    RepartidorActualNombre = rep?.NombreCompleto ?? string.Empty,
                    OficinaJsonId = ruta.OficinaOrigenJsonId,
                    OficinaNombre = ruta.OficinaOrigenNombre,
                    FechaReparto = ruta.FechaReparto.ToString("yyyy-MM-dd"),
                    Estado = e.Estado.ToString()
                };
            })
            .OrderBy(d => d.RutaActualCodigo)
            .ThenBy(d => d.NumeroExpedicion)
            .ToList();
    }

    public async Task<EntregaPaqueteDto?> ReasignarEntregaARuta(int entregaId, int nuevaRutaId)
    {
        var entrega = await _entregaRepo.GetWithRutaAsync(entregaId);
        if (entrega == null) return null;

        if (entrega.Estado != EstadoEntrega.Pendiente)
        {
            _logger.LogWarning("Intento de reasignar entrega {Id} en estado {Estado}", entregaId, entrega.Estado);
            return null;
        }

        var rutaDestino = await _rutaRepo.GetWithEntregasAsync(nuevaRutaId);
        if (rutaDestino == null || rutaDestino.Estado != EstadoRuta.Planificada)
        {
            _logger.LogWarning("Ruta destino {Id} no existe o no está planificada", nuevaRutaId);
            return null;
        }

        if (entrega.RutaRepartoId == nuevaRutaId)
        {
            return MapearEntrega(entrega);
        }

        var ordenSiguiente = rutaDestino.Entregas.Any() ? rutaDestino.Entregas.Max(e => e.OrdenEnRuta) + 1 : 1;
        entrega.RutaRepartoId = nuevaRutaId;
        entrega.OrdenEnRuta = ordenSiguiente;
        await _entregaRepo.UpdateAsync(entrega);

        _logger.LogInformation("Entrega {Id} reasignada a ruta {Ruta}", entregaId, rutaDestino.Codigo);

        return MapearEntrega(entrega);
    }

    // ═══════════════════════════════════════════
    //  HELPERS PRIVADOS ORIGINALES
    // ═══════════════════════════════════════════

    private static RutaRepartoDetalleDto MapearRutaDetalle(RutaReparto r)
    {
        return new RutaRepartoDetalleDto
        {
            Id = r.Id,
            Codigo = r.Codigo,
            FechaReparto = r.FechaReparto.ToString("yyyy-MM-dd"),
            RepartidorId = r.RepartidorId,
            RepartidorNombre = r.Repartidor?.NombreCompleto ?? string.Empty,
            OficinaOrigenJsonId = r.OficinaOrigenJsonId,
            OficinaOrigenNombre = r.OficinaOrigenNombre,
            Estado = r.Estado.ToString(),
            HoraSalida = r.HoraSalida,
            HoraRegreso = r.HoraRegreso,
            Observaciones = r.Observaciones,
            Entregas = (r.Entregas ?? [])
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
                    TelefonoDestinatario = e.TelefonoDestinatario,
                    NumeroIntento = e.NumeroIntento,
                    OrdenEnRuta = e.OrdenEnRuta,
                    Estado = e.Estado.ToString(),
                    FechaIntento = e.FechaIntento,
                    ReceptorNombre = e.ReceptorNombre,
                    ReceptorDni = e.ReceptorDni,
                    Observaciones = e.Observaciones,
                    LatitudEntrega = e.LatitudEntrega,
                    LongitudEntrega = e.LongitudEntrega,
                    FirmaDigital = e.FirmaDigital,
                    FotoEntrega = e.FotoEntrega
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
            TelefonoDestinatario = e.TelefonoDestinatario,
            NumeroIntento = e.NumeroIntento,
            OrdenEnRuta = e.OrdenEnRuta,
            Estado = e.Estado.ToString(),
            FechaIntento = e.FechaIntento,
            ReceptorNombre = e.ReceptorNombre,
            ReceptorDni = e.ReceptorDni,
            Observaciones = e.Observaciones,
            LatitudEntrega = e.LatitudEntrega,
            LongitudEntrega = e.LongitudEntrega,
            FirmaDigital = e.FirmaDigital,
            FotoEntrega = e.FotoEntrega
        };
    }

    private static DateOnly ParseFechaReparto(string? fechaReparto)
    {
        if (!string.IsNullOrWhiteSpace(fechaReparto) && DateOnly.TryParse(fechaReparto, out var parsed))
        {
            return parsed;
        }

        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    private async Task<Repartidor?> SeleccionarRepartidor(DateOnly fechaReparto, int? oficinaPreferidaJsonId)
    {
        var candidatos = await _repartidorRepo.GetAllAsync(oficinaPreferidaJsonId);
        var activos = candidatos.Where(r => r.Activo).ToList();

        if (!activos.Any() && oficinaPreferidaJsonId.HasValue)
        {
            activos = (await _repartidorRepo.GetAllAsync(null)).Where(r => r.Activo).ToList();
        }

        return activos
            .OrderBy(r => r.Rutas.Count(rt =>
                rt.FechaReparto == fechaReparto &&
                (rt.Estado == EstadoRuta.Planificada || rt.Estado == EstadoRuta.EnCurso)))
            .ThenBy(r => r.Rutas.Count(rt => rt.FechaReparto == fechaReparto))
            .ThenBy(r => r.Id)
            .FirstOrDefault();
    }

    private async Task<RutaRepartoDetalleDto?> ObtenerRutaPlanificadaDelDia(DateOnly fechaReparto, int repartidorId)
    {
        var rutas = await _rutaRepo.GetAllAsync(fechaReparto, repartidorId);
        var candidata = rutas.FirstOrDefault(r => r.Estado == EstadoRuta.Planificada);

        if (candidata == null)
        {
            return null;
        }

        return await ObtenerRutaPorId(candidata.Id);
    }
}
