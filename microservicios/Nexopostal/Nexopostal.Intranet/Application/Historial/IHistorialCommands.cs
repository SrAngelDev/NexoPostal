using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Historial;
/// <summary>Contrato de operaciones que modifican estado para Historial.</summary>
public interface IHistorialCommands
{
    /// <summary>
    /// Registra un nuevo evento de trazabilidad en el historial.
    /// Se llama cada vez que un paquete cambia de estado o ubicación.
    /// </summary>
    Task<HistorialEventoInternoDto> RegistrarEvento(CrearHistorialEventoDto dto);
}
