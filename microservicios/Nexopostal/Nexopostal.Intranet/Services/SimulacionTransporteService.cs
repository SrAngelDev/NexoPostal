using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio en segundo plano que simula el transporte de paquetes
/// a través de la red logística de NexoPostal.
/// 
/// Flujo simulado:
///   1. Oficina → CTA origen (RecogidoEnOrigen → RecibidoEnCentroOrigen)
///   2. Clasificación automática (RecibidoEnCentroOrigen → ClasificadoParaExpedicion)
///   3. Despacho troncal (ClasificadoParaExpedicion → EnTransitoHaciaCentroDestino)
///   4. Tránsito entre CTAs (MovimientoPaquete: Programado → EnTransito → Recibido)
///   5. CTA destino → Oficina destino (RecibidoEnCentroDestino → DepositadoEnOficina)
///
/// Cada transición tiene un retardo configurable para simular
/// tiempos reales de transporte de forma acelerada.
/// </summary>
public class SimulacionTransporteService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimulacionTransporteService> _logger;

    // ═══════════════════════════════════════════
    //  TIEMPOS DE SIMULACIÓN (en segundos)
    //  Ajustar para demo rápida o más realista
    // ═══════════════════════════════════════════
    private const int IntervaloComprobacion = 10;   // Cada cuántos segundos comprueba
    private const int DemoraOficinaACta = 20;       // Oficina → CTA origen
    private const int DemoraClasificacion = 10;     // Clasificación automática
    private const int DemoraDespachoTroncal = 10;   // Preparar despacho
    private const int DemoraTransito = 30;          // Tránsito CTA → CTA
    private const int DemoraCtaAOficina = 20;       // CTA destino → Oficina destino

    public SimulacionTransporteService(
        IServiceScopeFactory scopeFactory,
        ILogger<SimulacionTransporteService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚚 SimulacionTransporteService iniciado. Intervalo: {Seg}s", IntervaloComprobacion);

        // Esperar un poco para que el sistema arranque completamente
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var historialRepo = scope.ServiceProvider.GetRequiredService<IHistorialEstadoRepository>();
                var movimientoRepo = scope.ServiceProvider.GetRequiredService<IMovimientoPaqueteRepository>();
                var ctaRepo = scope.ServiceProvider.GetRequiredService<ICentroTratamientoRepository>();
                var rutaRepo = scope.ServiceProvider.GetRequiredService<IRutaCtaRepository>();
                var historialService = scope.ServiceProvider.GetRequiredService<IHistorialService>();
                var movimientoService = scope.ServiceProvider.GetRequiredService<IMovimientoService>();
                var clasificacionService = scope.ServiceProvider.GetRequiredService<IClasificacionService>();
                var oficinasService = scope.ServiceProvider.GetRequiredService<OficinasJsonService>();

                // Ejecutar todas las fases de simulación
                await SimularOficinaACta(historialRepo, ctaRepo, rutaRepo, historialService, oficinasService);
                await SimularClasificacion(historialRepo, historialService, clasificacionService);
                await SimularDespachoTroncal(historialRepo, movimientoRepo, historialService, movimientoService);
                await SimularTransitoCta(movimientoRepo, historialService, movimientoService);
                await SimularCtaAOficina(historialRepo, ctaRepo, historialService, oficinasService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ciclo de simulación de transporte");
            }

            await Task.Delay(TimeSpan.FromSeconds(IntervaloComprobacion), stoppingToken);
        }

        _logger.LogInformation("🚚 SimulacionTransporteService detenido");
    }

    // ═══════════════════════════════════════════════════════════════
    //  FASE 1: Oficina → CTA origen
    //  RecogidoEnOrigen → RecibidoEnCentroOrigen
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Busca paquetes en estado RecogidoEnOrigen que llevan suficiente tiempo
    /// en la oficina y los traslada al CTA correspondiente.
    /// </summary>
    private async Task SimularOficinaACta(
        IHistorialEstadoRepository historialRepo,
        ICentroTratamientoRepository ctaRepo,
        IRutaCtaRepository rutaRepo,
        IHistorialService historialService,
        OficinasJsonService oficinasService)
    {
        var umbral = DateTime.UtcNow.AddSeconds(-DemoraOficinaACta);

        // Obtener los últimos eventos de cada paquete en estado RecogidoEnOrigen
        var paquetesEnOficina = await historialRepo.GetExpedicionesPendientesEnEstadoAsync("RecogidoEnOrigen", umbral);

        foreach (var evento in paquetesEnOficina)
        {
            // Resolver CTA desde la oficina (usar su CP)
            CentroTratamiento? ctaOrigen = null;
            if (evento.UbicacionId.HasValue)
            {
                var oficina = oficinasService.ObtenerPorId(evento.UbicacionId.Value);
                if (oficina != null && !string.IsNullOrEmpty(oficina.CodigoPostal) && oficina.CodigoPostal.Length >= 2)
                {
                    var prefijoCp = oficina.CodigoPostal[..2];
                    var ruta = await rutaRepo.GetByPrefijoAsync(prefijoCp);
                    ctaOrigen = ruta?.Cta;
                }
            }

            // Si no se pudo resolver, usar CTA-MAD como fallback
            ctaOrigen ??= await ctaRepo.GetByCodigoAsync("CTA-MAD");

            if (ctaOrigen == null) continue;

            await historialService.RegistrarEvento(new CrearHistorialEventoDto
            {
                NumeroExpedicion = evento.NumeroExpedicion,
                NumeroSeguimiento = evento.NumeroSeguimiento,
                Estado = "RecibidoEnCentroOrigen",
                EstadoPrevio = "RecogidoEnOrigen",
                TipoUbicacion = TipoUbicacion.Cta.ToString(),
                UbicacionId = ctaOrigen.Id,
                UbicacionNombre = ctaOrigen.Codigo,
                UbicacionCodigo = ctaOrigen.Codigo,
                OperarioNombre = "Sistema (Simulación)",
                Descripcion = $"📦 Paquete transportado desde oficina al {ctaOrigen.Nombre}",
                Observaciones = "Transporte simulado automáticamente",
                VisibleParaCliente = true
            });

            _logger.LogInformation(
                "🚛 Simulación: {Exp} → Oficina → {Cta} (RecibidoEnCentroOrigen)",
                evento.NumeroExpedicion, ctaOrigen.Codigo);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  FASE 2: Clasificación automática
    //  RecibidoEnCentroOrigen → ClasificadoParaExpedicion
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Clasifica automáticamente los paquetes que llegan al CTA.
    /// Si hay un movimiento troncal programado para el paquete, se mantiene
    /// automáticamente la ruta asignada.
    /// </summary>
    private async Task SimularClasificacion(
        IHistorialEstadoRepository historialRepo,
        IHistorialService historialService,
        IClasificacionService clasificacionService)
    {
        var umbral = DateTime.UtcNow.AddSeconds(-DemoraClasificacion);

        var paquetesEnCta = await historialRepo.GetExpedicionesPendientesEnEstadoAsync("RecibidoEnCentroOrigen", umbral);

        foreach (var evento in paquetesEnCta)
        {
            await historialService.RegistrarEvento(new CrearHistorialEventoDto
            {
                NumeroExpedicion = evento.NumeroExpedicion,
                NumeroSeguimiento = evento.NumeroSeguimiento,
                Estado = "ClasificadoParaExpedicion",
                EstadoPrevio = "RecibidoEnCentroOrigen",
                TipoUbicacion = TipoUbicacion.Cta.ToString(),
                UbicacionId = evento.UbicacionId,
                UbicacionNombre = evento.UbicacionNombre,
                UbicacionCodigo = evento.UbicacionCodigo,
                OperarioNombre = "Sistema (Simulación)",
                Descripcion = $"📋 Paquete clasificado automáticamente en {evento.UbicacionNombre}",
                Observaciones = "Clasificación simulada automáticamente",
                VisibleParaCliente = true
            });

            _logger.LogInformation(
                "📋 Simulación: {Exp} clasificado en {Cta}",
                evento.NumeroExpedicion, evento.UbicacionNombre);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  FASE 3: Despacho troncal
    //  ClasificadoParaExpedicion → EnTransitoHaciaCentroDestino
    //  Busca MovimientoPaquete Programado y lo despacha
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Despacha los paquetes clasificados que tienen un movimiento troncal programado.
    /// </summary>
    private async Task SimularDespachoTroncal(
        IHistorialEstadoRepository historialRepo,
        IMovimientoPaqueteRepository movimientoRepo,
        IHistorialService historialService,
        IMovimientoService movimientoService)
    {
        var umbral = DateTime.UtcNow.AddSeconds(-DemoraDespachoTroncal);

        var paquetesClasificados = await historialRepo.GetExpedicionesPendientesEnEstadoAsync("ClasificadoParaExpedicion", umbral);

        foreach (var evento in paquetesClasificados)
        {
            // Buscar movimiento troncal programado para este paquete
            var movimientos = await movimientoRepo.GetByExpedicionAsync(evento.NumeroExpedicion);
            var movimiento = movimientos.FirstOrDefault(m => m.Estado == EstadoMovimiento.Programado);

            if (movimiento == null)
            {
                // Si no hay movimiento troncal, el paquete es local al CTA.
                // Avanzar directamente a RecibidoEnCentroDestino
                await historialService.RegistrarEvento(new CrearHistorialEventoDto
                {
                    NumeroExpedicion = evento.NumeroExpedicion,
                    NumeroSeguimiento = evento.NumeroSeguimiento,
                    Estado = "RecibidoEnCentroDestino",
                    EstadoPrevio = "ClasificadoParaExpedicion",
                    TipoUbicacion = TipoUbicacion.Cta.ToString(),
                    UbicacionId = evento.UbicacionId,
                    UbicacionNombre = evento.UbicacionNombre,
                    UbicacionCodigo = evento.UbicacionCodigo,
                    OperarioNombre = "Sistema (Simulación)",
                    Descripcion = $"📦 Paquete local - permanece en {evento.UbicacionNombre}",
                    Observaciones = "Paquete local sin transporte troncal necesario",
                    VisibleParaCliente = true
                });

                _logger.LogInformation(
                    "📦 Simulación: {Exp} es local en {Cta}, sin troncal necesario",
                    evento.NumeroExpedicion, evento.UbicacionNombre);

                continue;
            }

            // Despachar el movimiento troncal
            await movimientoService.DespacharMovimiento(movimiento.Id);

            await historialService.RegistrarEvento(new CrearHistorialEventoDto
            {
                NumeroExpedicion = evento.NumeroExpedicion,
                NumeroSeguimiento = evento.NumeroSeguimiento,
                Estado = "EnTransitoHaciaCentroDestino",
                EstadoPrevio = "ClasificadoParaExpedicion",
                TipoUbicacion = TipoUbicacion.Cta.ToString(),
                UbicacionId = movimiento.CtaOrigenId,
                UbicacionNombre = movimiento.CtaOrigen.Codigo,
                UbicacionCodigo = movimiento.CtaOrigen.Codigo,
                OperarioNombre = "Sistema (Simulación)",
                Descripcion = $"🚚 Despacho troncal: {movimiento.CtaOrigen.Codigo} → {movimiento.CtaDestino.Codigo} vía {movimiento.TipoTransporte}",
                Observaciones = "Despacho simulado automáticamente",
                VisibleParaCliente = true
            });

            _logger.LogInformation(
                "🚚 Simulación: {Exp} despachado {Origen} → {Destino}",
                evento.NumeroExpedicion, movimiento.CtaOrigen.Codigo, movimiento.CtaDestino.Codigo);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  FASE 4: Tránsito entre CTAs
    //  Movimiento EnTransito → Recibido
    //  EnTransitoHaciaCentroDestino → RecibidoEnCentroDestino
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Simula la llegada de paquetes en tránsito al CTA destino.
    /// </summary>
    private async Task SimularTransitoCta(
        IMovimientoPaqueteRepository movimientoRepo,
        IHistorialService historialService,
        IMovimientoService movimientoService)
    {
        var umbral = DateTime.UtcNow.AddSeconds(-DemoraTransito);

        // Buscar movimientos en tránsito que llevan suficiente tiempo
        var movimientosEnTransito = await movimientoRepo.GetEnTransitoAnterioresAAsync(umbral);

        foreach (var movimiento in movimientosEnTransito)
        {
            // Recibir el movimiento
            await movimientoService.RecibirMovimiento(movimiento.Id);

            await historialService.RegistrarEvento(new CrearHistorialEventoDto
            {
                NumeroExpedicion = movimiento.NumeroExpedicion,
                Estado = "RecibidoEnCentroDestino",
                EstadoPrevio = "EnTransitoHaciaCentroDestino",
                TipoUbicacion = TipoUbicacion.Cta.ToString(),
                UbicacionId = movimiento.CtaDestinoId,
                UbicacionNombre = movimiento.CtaDestino.Codigo,
                UbicacionCodigo = movimiento.CtaDestino.Codigo,
                OperarioNombre = "Sistema (Simulación)",
                Descripcion = $"📬 Paquete recibido en CTA destino {movimiento.CtaDestino.Nombre} (desde {movimiento.CtaOrigen.Codigo})",
                Observaciones = "Recepción troncal simulada automáticamente",
                VisibleParaCliente = true
            });

            _logger.LogInformation(
                "📬 Simulación: {Exp} llegó a {CtaDestino} desde {CtaOrigen}",
                movimiento.NumeroExpedicion, movimiento.CtaDestino.Codigo, movimiento.CtaOrigen.Codigo);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  FASE 5: CTA destino → Oficina destino
    //  RecibidoEnCentroDestino → DepositadoEnOficina
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Simula el transporte del paquete desde el CTA destino
    /// hasta la oficina de destino más cercana.
    /// </summary>
    private async Task SimularCtaAOficina(
        IHistorialEstadoRepository historialRepo,
        ICentroTratamientoRepository ctaRepo,
        IHistorialService historialService,
        OficinasJsonService oficinasService)
    {
        var umbral = DateTime.UtcNow.AddSeconds(-DemoraCtaAOficina);

        var paquetesEnCtaDestino = await historialRepo.GetExpedicionesPendientesEnEstadoAsync("RecibidoEnCentroDestino", umbral);

        foreach (var evento in paquetesEnCtaDestino)
        {
            // Resolver una oficina en la zona del CTA
            DTOs.OficinaJsonDto? oficinaDestino = null;
            if (evento.UbicacionId.HasValue)
            {
                var cta = await ctaRepo.GetByIdAsync(evento.UbicacionId.Value);
                if (cta != null && !string.IsNullOrEmpty(cta.CodigoPostal))
                {
                    oficinaDestino = oficinasService.ResolverOficinaMasCercana(cta.CodigoPostal);
                }
            }

            // Fallback: primera oficina disponible
            if (oficinaDestino == null)
            {
                var todas = oficinasService.ObtenerTodas();
                oficinaDestino = todas.FirstOrDefault();
            }

            if (oficinaDestino == null) continue;

            await historialService.RegistrarEvento(new CrearHistorialEventoDto
            {
                NumeroExpedicion = evento.NumeroExpedicion,
                NumeroSeguimiento = evento.NumeroSeguimiento,
                Estado = "DepositadoEnOficina",
                EstadoPrevio = "RecibidoEnCentroDestino",
                TipoUbicacion = TipoUbicacion.Oficina.ToString(),
                UbicacionId = oficinaDestino.Id,
                UbicacionNombre = oficinaDestino.Nombre,
                OperarioNombre = "Sistema (Simulación)",
                Descripcion = $"🏤 Paquete depositado en {oficinaDestino.Nombre} para recogida o reparto",
                Observaciones = "Entrega a oficina destino simulada automáticamente",
                VisibleParaCliente = true
            });

            _logger.LogInformation(
                "🏤 Simulación: {Exp} depositado en oficina {Oficina}",
                evento.NumeroExpedicion, oficinaDestino.Nombre);
        }
    }
}
