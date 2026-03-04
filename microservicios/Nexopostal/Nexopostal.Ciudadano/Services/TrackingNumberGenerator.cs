namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Servicio para generar números de seguimiento y expedición únicos.
/// - NumeroSeguimiento (público): NX + 9 dígitos + ES → para clientes
/// - NumeroExpedicion (interno): NXI- + 8 alfanuméricos → para operarios/repartidores
/// </summary>
public interface ITrackingNumberGenerator
{
    /// <summary>
    /// Genera un número de seguimiento público. Formato: NX123456789ES
    /// </summary>
    string Generate();

    /// <summary>
    /// Genera un número de expedición interno. Formato: NXI-A3F72K9B
    /// </summary>
    string GenerateExpedicion();
}

/// <summary>
/// Implementación del generador de números de seguimiento y expedición
/// </summary>
public class TrackingNumberGenerator : ITrackingNumberGenerator
{
    private static readonly Random _random = new Random();
    private static readonly object _lock = new object();
    private const string AlphaNumChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Sin I, O, 0, 1 para evitar confusión

    public string Generate()
    {
        lock (_lock)
        {
            // Timestamp en milisegundos (últimos 9 dígitos)
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var timestampStr = timestamp.ToString().Substring(timestamp.ToString().Length - 9);

            // Componente aleatorio (2 dígitos)
            var randomComponent = _random.Next(10, 99);

            // Formato: NX + timestamp(9) + random(2) + ES
            return $"NX{timestampStr}{randomComponent}ES";
        }
    }

    public string GenerateExpedicion()
    {
        lock (_lock)
        {
            // Generar 8 caracteres alfanuméricos aleatorios
            var chars = new char[8];
            for (int i = 0; i < 8; i++)
            {
                chars[i] = AlphaNumChars[_random.Next(AlphaNumChars.Length)];
            }

            // Formato: NXI- + 8 alfanuméricos
            return $"NXI-{new string(chars)}";
        }
    }
}
