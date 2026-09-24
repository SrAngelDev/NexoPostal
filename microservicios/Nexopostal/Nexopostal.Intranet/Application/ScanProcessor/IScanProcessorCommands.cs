using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.ScanProcessor;
/// <summary>Contrato de operaciones que modifican estado para ScanProcessor.</summary>
public interface IScanProcessorCommands
{
    /// <summary>Procesa un escaneo individual.</summary>
    Task<ScanResultDto> ProcesarEscaneo(ScanRequestDto request);
    /// <summary>Procesa un lote de escaneos con el mismo modo.</summary>
    Task<ScanBatchResultDto> ProcesarLote(ScanBatchRequestDto request);
}
