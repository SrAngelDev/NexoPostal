using System.Net.Http.Json;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Cliente HTTP que registra paquetes en la bandeja del JefeReparto
/// del microservicio Reparto. Se invoca al escanear DisponibleParaReparto
/// en el CTA destino.
/// </summary>
public interface IRepartoBandejaService
{
    Task<RegistrarBandejaResultDto> RegistrarPaqueteAsync(RegistrarPaqueteBandejaIntranetDto dto);
}

public class RegistrarPaqueteBandejaIntranetDto
{
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string? NumeroSeguimiento { get; set; }
    public int CtaId { get; set; }
    public string? CtaCodigo { get; set; }
    public string? NombreDestinatario { get; set; }
    public string? TelefonoDestinatario { get; set; }
    public string? DireccionEntrega { get; set; }
    public string? CodigoPostalDestino { get; set; }
    public string? CiudadDestino { get; set; }
    public bool EsUrgente { get; set; }
    public string? Observaciones { get; set; }
}

public class RegistrarBandejaResultDto
{
    public bool Success { get; set; }
    public bool Idempotente { get; set; }
    public int? Id { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class RepartoBandejaService : IRepartoBandejaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RepartoBandejaService> _logger;

    public RepartoBandejaService(HttpClient httpClient, ILogger<RepartoBandejaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RegistrarBandejaResultDto> RegistrarPaqueteAsync(RegistrarPaqueteBandejaIntranetDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/reparto/interno/bandeja/registrar", dto);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Reparto respondió {StatusCode} al registrar {Expedicion} en bandeja: {Body}",
                    (int)response.StatusCode, dto.NumeroExpedicion, body);
                return new RegistrarBandejaResultDto
                {
                    Success = false,
                    Message = $"Reparto devolvió {(int)response.StatusCode}."
                };
            }

            var data = await response.Content.ReadFromJsonAsync<RegistrarBandejaResultDto>();
            return data ?? new RegistrarBandejaResultDto
            {
                Success = false,
                Message = "Respuesta vacía del servicio Reparto."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando {Expedicion} en bandeja de Reparto.", dto.NumeroExpedicion);
            return new RegistrarBandejaResultDto
            {
                Success = false,
                Message = "Error de comunicación con Reparto."
            };
        }
    }
}
