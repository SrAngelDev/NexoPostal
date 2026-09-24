using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Admision;
/// <summary>Contrato de operaciones que modifican estado para Admision.</summary>
public interface IAdmisionCommands
{
    /// <summary>
    /// Admite un paquete en la red logística:
    /// resuelve el CTA por código postal, crea el movimiento si es necesario
    /// y notifica en tiempo real al CTA destino.
    /// </summary>
    Task<AdmisionPaqueteResponseDto> AdmitirPaquete(AdmisionPaqueteDto dto);
}
