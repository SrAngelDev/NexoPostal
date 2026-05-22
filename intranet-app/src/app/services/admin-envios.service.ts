import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

// ─── Enums espejo de Ciudadano (números → orden visual) ───
export enum EstadoEnvio {
  PendientePago = -1,
  Admitido = 0,
  EnTransito = 1,
  EnOficina = 2,
  EnReparto = 3,
  Entregado = 4,
  Incidencia = 5,
  Devuelto = 6
}

export enum EstadoInterno {
  PendientePago = -1,
  PendienteRecogida = 0,
  RecogidoEnOrigen = 1,
  RecibidoEnCentroOrigen = 10,
  EnClasificacionOrigen = 11,
  ClasificadoParaExpedicion = 12,
  EnTransitoHaciaCentroDestino = 20,
  EnTransitoIntermedio = 21,
  RecibidoEnCentroDestino = 30,
  EnClasificacionDestino = 31,
  AsignadoARuta = 32,
  EnReparto = 40,
  PrimerIntentoFallido = 41,
  SegundoIntentoFallido = 42,
  DepositadoEnOficina = 43,
  EntregadoEnDomicilio = 50,
  EntregadoEnOficina = 51,
  EntregadoAAutorizado = 52,
  IncidenciaDireccionIncorrecta = 60,
  IncidenciaPaqueteDanado = 61,
  IncidenciaDestinatarioRechaza = 62,
  IncidenciaOtra = 63,
  EnDevolucionAlRemitente = 70,
  DevueltoAlRemitente = 71
}

export const ESTADO_PUBLICO_OPTIONS: { value: EstadoEnvio; label: string }[] = [
  { value: EstadoEnvio.PendientePago, label: 'Pendiente Pago' },
  { value: EstadoEnvio.Admitido, label: 'Admitido' },
  { value: EstadoEnvio.EnTransito, label: 'En Tránsito' },
  { value: EstadoEnvio.EnOficina, label: 'En Oficina' },
  { value: EstadoEnvio.EnReparto, label: 'En Reparto' },
  { value: EstadoEnvio.Entregado, label: 'Entregado' },
  { value: EstadoEnvio.Incidencia, label: 'Incidencia' },
  { value: EstadoEnvio.Devuelto, label: 'Devuelto' }
];

export const ESTADO_INTERNO_OPTIONS: { value: EstadoInterno; label: string }[] = [
  { value: EstadoInterno.PendientePago, label: 'Pendiente Pago' },
  { value: EstadoInterno.PendienteRecogida, label: 'Pendiente Recogida' },
  { value: EstadoInterno.RecogidoEnOrigen, label: 'Recogido en Origen' },
  { value: EstadoInterno.RecibidoEnCentroOrigen, label: 'Recibido en CTA Origen' },
  { value: EstadoInterno.EnClasificacionOrigen, label: 'En Clasificación Origen' },
  { value: EstadoInterno.ClasificadoParaExpedicion, label: 'Clasificado para Expedición' },
  { value: EstadoInterno.EnTransitoHaciaCentroDestino, label: 'En Tránsito hacia CTA Destino' },
  { value: EstadoInterno.EnTransitoIntermedio, label: 'En Tránsito Intermedio' },
  { value: EstadoInterno.RecibidoEnCentroDestino, label: 'Recibido en CTA Destino' },
  { value: EstadoInterno.EnClasificacionDestino, label: 'En Clasificación Destino' },
  { value: EstadoInterno.AsignadoARuta, label: 'Asignado a Ruta' },
  { value: EstadoInterno.EnReparto, label: 'En Reparto' },
  { value: EstadoInterno.PrimerIntentoFallido, label: '1er Intento Fallido' },
  { value: EstadoInterno.SegundoIntentoFallido, label: '2º Intento Fallido' },
  { value: EstadoInterno.DepositadoEnOficina, label: 'Depositado en Oficina' },
  { value: EstadoInterno.EntregadoEnDomicilio, label: 'Entregado en Domicilio' },
  { value: EstadoInterno.EntregadoEnOficina, label: 'Entregado en Oficina' },
  { value: EstadoInterno.EntregadoAAutorizado, label: 'Entregado a Autorizado' },
  { value: EstadoInterno.IncidenciaDireccionIncorrecta, label: 'Incidencia · Dirección' },
  { value: EstadoInterno.IncidenciaPaqueteDanado, label: 'Incidencia · Dañado' },
  { value: EstadoInterno.IncidenciaDestinatarioRechaza, label: 'Incidencia · Rechazo' },
  { value: EstadoInterno.IncidenciaOtra, label: 'Incidencia · Otra' },
  { value: EstadoInterno.EnDevolucionAlRemitente, label: 'En Devolución' },
  { value: EstadoInterno.DevueltoAlRemitente, label: 'Devuelto al Remitente' }
];

export function estadoPublicoLabel(e: EstadoEnvio): string {
  return ESTADO_PUBLICO_OPTIONS.find(o => o.value === e)?.label ?? String(e);
}
export function estadoInternoLabel(e: EstadoInterno): string {
  return ESTADO_INTERNO_OPTIONS.find(o => o.value === e)?.label ?? String(e);
}

export interface AdminEnvioListItemDto {
  numeroSeguimiento: string;
  numeroExpedicion: string;
  fechaCreacion: string;
  estadoActual: EstadoEnvio;
  estadoInternoActual: EstadoInterno;
  pagado: boolean;
  origen: string;
  destino: string;
  codigoPostalDestino: string;
  nombreRemitente: string;
  emailRemitente: string;
  nombreDestinatario: string;
  tipoTarifa: string;
  costeCalculado: number;
}

export interface AdminEnvioDetalleDto extends AdminEnvioListItemDto {
  identityUserId?: string | null;
  pesoKg: number;
  dimensiones: string;
  codigoPostalOrigen: string;
  tiempoEntregaEstimado: string;
  apellidosRemitente: string;
  telefonoRemitente: string;
  dniRemitente?: string | null;
  apellidosDestinatario: string;
  telefonoDestinatario: string;
  emailDestinatario?: string | null;
  dniDestinatario?: string | null;
  observaciones?: string | null;
  fechaPago?: string | null;
}

export interface ListarEnviosFiltros {
  estado?: EstadoEnvio | null;
  estadoInterno?: EstadoInterno | null;
  fechaDesde?: string | null;
  fechaHasta?: string | null;
  q?: string | null;
  cp?: string | null;
  pagado?: boolean | null;
  limit?: number | null;
}

export interface CambiarEstadoEnvioDto {
  estadoPublico: EstadoEnvio;
  estadoInterno: EstadoInterno;
  motivo?: string | null;
}

export interface AccionEnvioDto {
  motivo?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AdminEnviosService {
  private readonly http = inject(HttpClient);
  private readonly BASE = '/api/nexopostal/admin-envios';

  listar(filtros: ListarEnviosFiltros = {}): Observable<AdminEnvioListItemDto[]> {
    let params = new HttpParams();
    if (filtros.estado !== undefined && filtros.estado !== null) params = params.set('estado', filtros.estado);
    if (filtros.estadoInterno !== undefined && filtros.estadoInterno !== null) params = params.set('estadoInterno', filtros.estadoInterno);
    if (filtros.fechaDesde) params = params.set('fechaDesde', filtros.fechaDesde);
    if (filtros.fechaHasta) params = params.set('fechaHasta', filtros.fechaHasta);
    if (filtros.q) params = params.set('q', filtros.q);
    if (filtros.cp) params = params.set('cp', filtros.cp);
    if (filtros.pagado !== undefined && filtros.pagado !== null) params = params.set('pagado', filtros.pagado);
    if (filtros.limit) params = params.set('limit', filtros.limit);
    return this.http.get<AdminEnvioListItemDto[]>(this.BASE, { params });
  }

  obtener(numero: string): Observable<AdminEnvioDetalleDto> {
    return this.http.get<AdminEnvioDetalleDto>(`${this.BASE}/${encodeURIComponent(numero)}`);
  }

  cambiarEstado(numero: string, dto: CambiarEstadoEnvioDto): Observable<AdminEnvioDetalleDto> {
    return this.http.put<AdminEnvioDetalleDto>(`${this.BASE}/${encodeURIComponent(numero)}/estado`, dto);
  }

  anular(numero: string, dto: AccionEnvioDto): Observable<AdminEnvioDetalleDto> {
    return this.http.post<AdminEnvioDetalleDto>(`${this.BASE}/${encodeURIComponent(numero)}/anular`, dto);
  }

  reabrir(numero: string, dto: AccionEnvioDto): Observable<AdminEnvioDetalleDto> {
    return this.http.post<AdminEnvioDetalleDto>(`${this.BASE}/${encodeURIComponent(numero)}/reabrir`, dto);
  }
}
