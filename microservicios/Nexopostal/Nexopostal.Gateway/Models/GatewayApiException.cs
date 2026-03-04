using System.Net;

namespace Nexopostal.Gateway.Models;

/// <summary>
/// Excepción personalizada que transporta el código de estado HTTP real
/// y el cuerpo de la respuesta del microservicio backend.
/// Se lanza desde el DelegatingHandler antes de que la librería ApiGateway
/// invoque EnsureSuccessStatusCode() y pierda la información.
/// </summary>
public class GatewayApiException : Exception
{
    /// <summary>Código HTTP real devuelto por el microservicio (400, 404, 422…)</summary>
    public int StatusCode { get; }

    /// <summary>Cuerpo de la respuesta tal cual lo envió el microservicio</summary>
    public string ResponseBody { get; }

    /// <summary>Content-Type de la respuesta original</summary>
    public string? ContentType { get; }

    public GatewayApiException(int statusCode, string responseBody, string? contentType)
        : base($"El microservicio respondió con {statusCode}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        ContentType = contentType;
    }
}
