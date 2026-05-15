using System.Net.Http.Json;
using Nexopostal.Intranet.DTOs;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Orquesta la creación automática de ruta y entrega en el microservicio Reparto
/// a partir de una admisión ya validada en Intranet.
/// </summary>
public interface IRepartoOrquestacionService
{
    Task<OrquestacionRepartoResultadoDto> AutoAsignarEntregaDesdeAdmisionAsync(
        AdmisionPaqueteDto admision,
        ResolverCtaResponseDto ctaDestino);
}

public class OrquestacionRepartoResultadoDto
{
    public bool Success { get; set; }
    public bool Idempotente { get; set; }
    public bool CreadaRuta { get; set; }
    public int? RutaId { get; set; }
    public string? RutaCodigo { get; set; }
    public int? RepartidorId { get; set; }
    public string? RepartidorNombre { get; set; }
    public int? EntregaId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class RepartoOrquestacionService : IRepartoOrquestacionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RepartoOrquestacionService> _logger;

    public RepartoOrquestacionService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RepartoOrquestacionService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OrquestacionRepartoResultadoDto> AutoAsignarEntregaDesdeAdmisionAsync(
        AdmisionPaqueteDto admision,
        ResolverCtaResponseDto ctaDestino)
    {
        try
        {
            var serviceKey = _configuration["RepartoSettings:ServiceKey"]
                ?? _configuration["InterServiceSettings:ServiceKey"]
                ?? "nexopostal-internal-service-key-2025";

            var request = new RepartoAutoAsignacionRequestDto
            {
                NumeroExpedicion = admision.NumeroExpedicion,
                NumeroSeguimiento = string.IsNullOrWhiteSpace(admision.NumeroSeguimiento)
                    ? admision.NumeroExpedicion
                    : admision.NumeroSeguimiento,
                CodigoPostalDestino = admision.CodigoPostalDestino,
                DireccionEntrega = admision.DireccionEntrega ?? string.Empty,
                CiudadDestino = string.IsNullOrWhiteSpace(admision.CiudadDestino)
                    ? ctaDestino.Provincia
                    : admision.CiudadDestino,
                NombreDestinatario = ObtenerNombreDestinatario(admision),
                TelefonoDestinatario = admision.TelefonoDestinatario,
                EsUrgente = admision.EsUrgente,
                OficinaPreferidaJsonId = ctaDestino.CtaId,
                OficinaPreferidaNombre = ctaDestino.CtaCodigo,
                Observaciones = admision.Observaciones
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/reparto/interno/admision/auto-asignar")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Add("X-Service-Key", serviceKey);

            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Reparto respondió {StatusCode} al auto-asignar {Expedicion}: {Body}",
                    (int)response.StatusCode,
                    admision.NumeroExpedicion,
                    body);

                return new OrquestacionRepartoResultadoDto
                {
                    Success = false,
                    Message = $"Reparto devolvió {(int)response.StatusCode} al auto-asignar la entrega."
                };
            }

            var data = await response.Content.ReadFromJsonAsync<RepartoAutoAsignacionResponseDto>();
            if (data == null)
            {
                return new OrquestacionRepartoResultadoDto
                {
                    Success = false,
                    Message = "Reparto no devolvió un payload válido para la auto-asignación."
                };
            }

            return new OrquestacionRepartoResultadoDto
            {
                Success = data.Success,
                Idempotente = data.Idempotente,
                CreadaRuta = data.CreadaRuta,
                RutaId = data.RutaId,
                RutaCodigo = data.RutaCodigo,
                RepartidorId = data.RepartidorId,
                RepartidorNombre = data.RepartidorNombre,
                EntregaId = data.EntregaId,
                Message = data.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al orquestar auto-asignación en Reparto para expedición {Expedicion}",
                admision.NumeroExpedicion);

            return new OrquestacionRepartoResultadoDto
            {
                Success = false,
                Message = "No se pudo contactar con Reparto para generar la entrega automática."
            };
        }
    }

    private static string ObtenerNombreDestinatario(AdmisionPaqueteDto admision)
    {
        if (!string.IsNullOrWhiteSpace(admision.NombreDestinatario))
            return admision.NombreDestinatario;

        if (!string.IsNullOrWhiteSpace(admision.Destinatario))
            return admision.Destinatario;

        return "Destinatario no informado";
    }

    private class RepartoAutoAsignacionRequestDto
    {
        public string NumeroExpedicion { get; set; } = string.Empty;
        public string NumeroSeguimiento { get; set; } = string.Empty;
        public string CodigoPostalDestino { get; set; } = string.Empty;
        public string DireccionEntrega { get; set; } = string.Empty;
        public string CiudadDestino { get; set; } = string.Empty;
        public string NombreDestinatario { get; set; } = string.Empty;
        public string? TelefonoDestinatario { get; set; }
        public bool EsUrgente { get; set; }
        public int? OficinaPreferidaJsonId { get; set; }
        public string? OficinaPreferidaNombre { get; set; }
        public string? Observaciones { get; set; }
    }

    private class RepartoAutoAsignacionResponseDto
    {
        public bool Success { get; set; }
        public bool Idempotente { get; set; }
        public bool CreadaRuta { get; set; }
        public int? RutaId { get; set; }
        public string? RutaCodigo { get; set; }
        public int? RepartidorId { get; set; }
        public string? RepartidorNombre { get; set; }
        public int? EntregaId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
