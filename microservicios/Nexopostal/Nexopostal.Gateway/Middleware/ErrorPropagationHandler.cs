using Microsoft.AspNetCore.Http;

namespace Nexopostal.Gateway.Middleware;

/// <summary>
/// DelegatingHandler que intercepta las respuestas HTTP de los microservicios
/// ANTES de que la librería AspNetCore.ApiGateway llame a EnsureSuccessStatusCode().
///
/// Cuando el microservicio devuelve un código no exitoso (400, 404, 409, 422…),
/// guarda el código real y el cuerpo en HttpContext.Items y cambia el status
/// a 200 OK para que EnsureSuccessStatusCode() no lance excepción.
///
/// Luego, GatewayErrorMiddleware (que usa response buffering) detecta la marca
/// y reescribe la respuesta HTTP al cliente con el código y cuerpo reales.
/// </summary>
public class ErrorPropagationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ErrorPropagationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Reenviar el token JWT al microservicio de destino.
        // aspNetCore.ApiGateway no lo propaga automáticamente, por lo que
        // endpoints protegidos con [Authorize] en el backend devolverían 401.
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null
            && httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader)
            && !request.Headers.Contains("Authorization"))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString();
            var realStatus = (int)response.StatusCode;

            // Guardar la información real del error en HttpContext.Items
            if (httpContext != null)
            {
                httpContext.Items["GatewayRealStatus"] = realStatus;
                httpContext.Items["GatewayRealBody"] = body;
                httpContext.Items["GatewayRealContentType"] = contentType ?? "application/json";
            }

            // Cambiar el status a 200 para engañar a EnsureSuccessStatusCode()
            response.StatusCode = System.Net.HttpStatusCode.OK;
        }

        return response;
    }
}
