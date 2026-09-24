using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.AdminCta;
/// <summary>Contrato de operaciones que modifican estado para AdminCta.</summary>
public interface IAdminCtaCommands
{
    Task<(CtaDetalleDto? cta, string? error)> CrearCta(CrearCtaDto dto);
    Task<(CtaDetalleDto? cta, string? error)> EditarCta(int id, EditarCtaDto dto);
    Task<(bool ok, string? error)> DesactivarCta(int id);
    Task<(bool ok, string? error)> ReactivarCta(int id);
}
