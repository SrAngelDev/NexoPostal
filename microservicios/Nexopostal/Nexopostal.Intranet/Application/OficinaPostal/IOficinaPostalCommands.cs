using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.OficinaPostal;
/// <summary>Contrato de operaciones que modifican estado para OficinaPostal.</summary>
public interface IOficinaPostalCommands
{
    /// <summary>Crea o cambia la oficina asignada a un operario (acción admin).</summary>
    Task<(bool Ok, string? Error, MiOficinaInfoDto? Resultado)> ActualizarOficinaAdmin(string identityUserId, AdminActualizarOficinaDto dto);
}
