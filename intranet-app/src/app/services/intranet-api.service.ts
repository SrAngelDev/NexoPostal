import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// ============================================================
//  DTOs del microservicio Intranet (logística)
// ============================================================

export interface MiCtaInfo {
  operarioId: number;
  nombreCompleto: string;
  codigoEmpleado: string;
  rol: string;
  ctaId: number;
  ctaCodigo: string;
  ctaNombre: string;
  area: string;
}

export interface CtaAsignacion {
  operarioCtaId: number;
  ctaId: number;
  ctaCodigo: string;
  ctaNombre: string;
  area: string;
}

export interface MisCtasInfo {
  nombreCompleto: string;
  codigoEmpleado: string;
  rol: string;
  ctas: CtaAsignacion[];
}

export interface DashboardCta {
  ctaId: number;
  ctaCodigo: string;
  ctaNombre: string;
  area: string;
  totalOperarios: number;
  operariosActivos: number;
  tareasPendientes: number;
  tareasEnProgreso: number;
  tareasCompletadasHoy: number;
  tareasUrgentes: number;
  movimientosProgramados: number;
  movimientosEnTransito: number;
  movimientosRecibidosHoy: number;
  incidenciasAbiertas: number;
  incidenciasEnRevision: number;
}

export interface OperarioResumen {
  id: number;
  nombreCompleto: string;
  codigoEmpleado: string;
  rol: string;
  activo: boolean;
  fechaAsignacion: string;
}

export interface AsignacionResumen {
  id: number;
  numeroExpedicion: string;
  tipoTarea: string;
  estadoTarea: string;
  esUrgente: boolean;
  operarioAsignado: string;
  asignadoPor: string;
  fechaAsignacion: string;
  fechaCompletada?: string;
}

export interface AsignacionDetalle {
  id: number;
  numeroExpedicion: string;
  tipoTarea: string;
  estadoTarea: string;
  esUrgente: boolean;
  observaciones?: string;
  operarioAsignadoId: number;
  operarioAsignadoNombre: string;
  operarioAsignadoCodigo: string;
  asignadoPorId: number;
  asignadoPorNombre: string;
  ctaId: number;
  ctaCodigo: string;
  fechaAsignacion: string;
  fechaInicio?: string;
  fechaCompletada?: string;
}

export interface CrearAsignacionRequest {
  numeroExpedicion: string;
  operarioAsignadoId: number;
  tipoTarea: string;
  esUrgente: boolean;
  observaciones?: string;
}

export interface IncidenciaResumen {
  id: number;
  numeroExpedicion: string;
  tipo: string;
  estado: string;
  reportadaPor: string;
  fechaCreacion: string;
  fechaResolucion?: string;
}

export interface MovimientoResumen {
  id: number;
  numeroExpedicion: string;
  ctaOrigenCodigo: string;
  ctaDestinoCodigo: string;
  estado: string;
  tipoTransporte: string;
  esUrgente: boolean;
  fechaCreacion: string;
  fechaSalida?: string;
  fechaLlegada?: string;
}

@Injectable({
  providedIn: 'root'
})
export class IntranetApiService {

  constructor(private http: HttpClient) {}

  // ─── Operarios ───

  /** Obtiene la info del CTA del operario autenticado (primer CTA) */
  obtenerMiCta(): Observable<MiCtaInfo> {
    return this.http.get<MiCtaInfo>('/api/operarios/mi-cta');
  }

  /** Obtiene TODOS los CTAs del operario autenticado */
  obtenerMisCtas(): Observable<MisCtasInfo> {
    return this.http.get<MisCtasInfo>('/api/operarios/mis-ctas');
  }

  /** Obtiene los operarios de un CTA */
  obtenerOperariosCta(ctaId: number): Observable<OperarioResumen[]> {
    return this.http.get<OperarioResumen[]>(`/api/operarios/cta/${ctaId}`);
  }

  // ─── CTAs ───

  /** Obtiene el dashboard de un CTA con estadísticas */
  obtenerDashboardCta(ctaId: number): Observable<DashboardCta> {
    return this.http.get<DashboardCta>(`/api/ctas/${ctaId}/dashboard`);
  }

  // ─── Asignaciones ───

  /** Crea una nueva asignación de tarea */
  crearAsignacion(dto: CrearAsignacionRequest): Observable<AsignacionDetalle> {
    return this.http.post<AsignacionDetalle>('/api/asignaciones/crear', dto);
  }

  /** Obtiene asignaciones de un CTA */
  obtenerAsignacionesCta(ctaId: number): Observable<AsignacionResumen[]> {
    return this.http.get<AsignacionResumen[]>(`/api/asignaciones/cta/${ctaId}`);
  }

  /** Obtiene detalle de una asignación */
  obtenerAsignacionDetalle(id: number): Observable<AsignacionDetalle> {
    return this.http.get<AsignacionDetalle>(`/api/asignaciones/${id}`);
  }

  /** Inicia una tarea */
  iniciarTarea(id: number): Observable<AsignacionDetalle> {
    return this.http.put<AsignacionDetalle>(`/api/asignaciones/${id}/iniciar`, {});
  }

  /** Completa una tarea */
  completarTarea(id: number): Observable<AsignacionDetalle> {
    return this.http.put<AsignacionDetalle>(`/api/asignaciones/${id}/completar`, {});
  }

  /** Cancela una tarea */
  cancelarTarea(id: number): Observable<AsignacionDetalle> {
    return this.http.put<AsignacionDetalle>(`/api/asignaciones/${id}/cancelar`, {});
  }

  // ─── Movimientos ───

  /** Obtiene movimientos de un CTA (como origen o destino) */
  obtenerMovimientosCta(ctaId: number): Observable<MovimientoResumen[]> {
    return this.http.get<MovimientoResumen[]>(`/api/movimientos/cta/${ctaId}`);
  }

  // ─── Incidencias ───

  /** Obtiene incidencias de un CTA */
  obtenerIncidenciasCta(ctaId: number): Observable<IncidenciaResumen[]> {
    return this.http.get<IncidenciaResumen[]>(`/api/incidencias/cta/${ctaId}`);
  }
}
