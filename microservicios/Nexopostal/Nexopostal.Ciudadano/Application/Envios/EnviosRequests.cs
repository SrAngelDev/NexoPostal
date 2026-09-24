using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;

namespace Nexopostal.Ciudadano.Application.Envios;
public sealed record CrearEnvioCommand(CrearEnvioDto Dto, string UserId, TipoEntrega TipoEntrega) : ICommand<EnvioCreadoDto>;
public sealed record ObtenerTrackingQuery(string Numero) : IQuery<EnvioTrackingDto?>;
public sealed record ObtenerMisEnviosQuery(string UserId) : IQuery<List<EnvioResumenDto>>;
public sealed class EnviosHandlers : IRequestHandler<CrearEnvioCommand, EnvioCreadoDto>, IRequestHandler<ObtenerTrackingQuery, EnvioTrackingDto?>, IRequestHandler<ObtenerMisEnviosQuery, List<EnvioResumenDto>>
{
    private readonly IEnvioRepository _envioRepo;
    private readonly ITrackingNumberGenerator _trackingGenerator;
    private readonly ITarifasService _tarifasService;
    private readonly ILogger<EnviosHandlers> _logger;
    public EnviosHandlers(IEnvioRepository envioRepo, ITrackingNumberGenerator trackingGenerator, ITarifasService tarifasService, ILogger<EnviosHandlers> logger)
    {
        _envioRepo = envioRepo;
        _trackingGenerator = trackingGenerator;
        _tarifasService = tarifasService;
        _logger = logger;
    }

    public async Task<EnvioCreadoDto> Handle(CrearEnvioCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dto = request.Dto;
        var userId = request.UserId;
        var tipoEntrega = request.TipoEntrega;
        var dimensiones = _tarifasService.ParseDimensiones(dto.Dimensiones);
        var tarifa = _tarifasService.Calcular(new TarifaCalculoInput(dto.Peso, dimensiones.Largo, dimensiones.Ancho, dimensiones.Alto, dto.CodigoPostalOrigen, dto.CodigoPostalDestino, "Estandar"));
        // Creamos el envío
        var envio = new Envio
        {
            NumeroSeguimiento = _trackingGenerator.Generate(),
            NumeroExpedicion = _trackingGenerator.GenerateExpedicion(),
            IdentityUserId = userId,
            PesoKg = dto.Peso,
            Dimensiones = dto.Dimensiones,
            Origen = dto.Origen,
            Destino = dto.Destino,
            CodigoPostalOrigen = dto.CodigoPostalOrigen,
            CodigoPostalDestino = dto.CodigoPostalDestino,
            OficinaOrigenId = dto.OficinaOrigenId,
            OficinaDestinoId = dto.OficinaDestinoId,
            TipoEntrega = tipoEntrega,
            EstadoActual = EstadoEnvio.Admitido,
            EstadoInternoActual = EstadoInterno.PendienteRecogida,
            FechaCreacion = DateTime.UtcNow,
            CosteCalculado = tarifa.PrecioTotal,
            TipoTarifa = tarifa.TipoTarifa,
            TiempoEntregaEstimado = tarifa.TiempoEntregaEstimado,
            Pagado = false,
            Observaciones = dto.Observaciones,
            NombreRemitente = dto.NombreRemitente,
            TelefonoRemitente = dto.TelefonoRemitente ?? string.Empty,
            NombreDestinatario = dto.NombreDestinatario,
            TelefonoDestinatario = dto.TelefonoDestinatario ?? string.Empty
        };
        await _envioRepo.CreateAsync(envio);
        _logger.LogInformation("Envío creado: {NumeroSeguimiento} por usuario {UserId} (TipoEntrega={TipoEntrega}, OficinaOrigen={OO}, OficinaDestino={OD})", envio.NumeroSeguimiento, userId, tipoEntrega, dto.OficinaOrigenId, dto.OficinaDestinoId);
        // Construimos la respuesta
        var respuesta = new EnvioCreadoDto
        {
            NumeroSeguimiento = envio.NumeroSeguimiento,
            NumeroExpedicion = envio.NumeroExpedicion,
            CosteCalculado = envio.CosteCalculado,
            EstadoActual = envio.EstadoActual.ToString(),
            TipoEntrega = envio.TipoEntrega.ToString(),
            OficinaOrigenId = envio.OficinaOrigenId,
            OficinaDestinoId = envio.OficinaDestinoId,
            FechaCreacion = envio.FechaCreacion,
            UrlEtiqueta = $"/api/etiquetas/{envio.NumeroSeguimiento}"};
        return respuesta;
    }

    public async Task<EnvioTrackingDto?> Handle(ObtenerTrackingQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var numero = request.Numero;
        var envio = await _envioRepo.GetByTrackingAsync(numero);
        if (envio == null)
        {
            _logger.LogWarning("Intento de tracking de envío inexistente: {Numero}", numero);
            return null;
        }

        var resultado = new EnvioTrackingDto
        {
            NumeroSeguimiento = envio.NumeroSeguimiento,
            EstadoActual = envio.EstadoActual.ToString(),
            EstadoInterno = envio.EstadoInternoActual.ToString(),
            Descripcion = ObtenerDescripcionEstado(envio.EstadoActual),
            FechaCreacion = envio.FechaCreacion,
            FechaEntrega = envio.EstadoActual == EstadoEnvio.Entregado ? envio.FechaPago : null,
            NumeroBultos = 1
        };
        return resultado;
    }

    public async Task<List<EnvioResumenDto>> Handle(ObtenerMisEnviosQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = request.UserId;
        var enviosList = await _envioRepo.GetByUserAsync(userId);
        var envios = enviosList.Select(e => new EnvioResumenDto { NumeroSeguimiento = e.NumeroSeguimiento, Estado = e.EstadoActual.ToString(), FechaCreacion = e.FechaCreacion, Destino = e.Destino, Precio = e.CosteCalculado, Pagado = e.Pagado, TipoTarifa = e.TipoTarifa }).ToList();
        return envios;
    }

    private static string ObtenerDescripcionEstado(EstadoEnvio estado) => estado switch
    {
        EstadoEnvio.PendientePago => "Envío pendiente de confirmación de pago",
        EstadoEnvio.Admitido => "Envío admitido en oficina de NexoPostal",
        EstadoEnvio.EnTransito => "Envío en tránsito hacia destino",
        EstadoEnvio.EnOficina => "Envío disponible en oficina de destino",
        EstadoEnvio.EnReparto => "Envío en reparto — pendiente de entrega",
        EstadoEnvio.Entregado => "Envío entregado al destinatario o autorizado en oficina",
        EstadoEnvio.Incidencia => "Incidencia registrada — contacte con atención al cliente",
        EstadoEnvio.Devuelto => "Envío devuelto al remitente",
        _ => "Estado desconocido"
    };
}
