using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Historial;
/// <summary>Contrato de lectura sin escrituras para Historial.</summary>
public interface IHistorialQueries
{
    /// <summary>
    /// Obtiene el historial completo de un paquete por número de expedición (vista interna).
    /// Incluye todos los eventos, visibles y no visibles para el cliente.
    /// </summary>
    Task<List<HistorialEventoInternoDto>> ObtenerHistorialInterno(string numeroExpedicion);
    /// <summary>
    /// Obtiene el historial público de un paquete por número de seguimiento.
    /// Solo incluye eventos marcados como visibles para el cliente.
    /// </summary>
    Task<List<HistorialEventoDto>> ObtenerHistorialPublico(string numeroSeguimiento);
    /// <summary>
    /// Obtiene el último evento registrado de un paquete.
    /// </summary>
    Task<HistorialEventoInternoDto?> ObtenerUltimoEvento(string numeroExpedicion);
}
