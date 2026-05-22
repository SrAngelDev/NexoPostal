import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface IncidenciaGlobalDto {
  id: number;
  numeroExpedicion: string;
  tipo: string;
  estado: string;
  reportadaPor: string;
  fechaCreacion: string;
  fechaResolucion?: string | null;
  ctaId?: number | null;
  ctaCodigo?: string | null;
  ctaNombre?: string | null;
  descripcion?: string | null;
}

export interface MovimientoGlobalDto {
  id: number;
  numeroExpedicion: string;
  ctaOrigenCodigo: string;
  ctaDestinoCodigo: string;
  estado: string;
  tipoTransporte: string;
  esUrgente: boolean;
  fechaCreacion: string;
  fechaSalida?: string | null;
  fechaLlegada?: string | null;
}

@Injectable({ providedIn: 'root' })
export class VistasGlobalesService {
  private readonly INCIDENCIAS_URL = '/api/nexopostal/incidencias/global';
  private readonly MOVIMIENTOS_URL = '/api/nexopostal/movimientos/global';

  constructor(private http: HttpClient) {}

  listarIncidenciasGlobales(opts: { estado?: string; ctaId?: number; tipo?: string } = {}): Observable<IncidenciaGlobalDto[]> {
    let params = new HttpParams();
    if (opts.estado) params = params.set('estado', opts.estado);
    if (opts.ctaId !== undefined) params = params.set('ctaId', opts.ctaId.toString());
    if (opts.tipo) params = params.set('tipo', opts.tipo);
    return this.http.get<IncidenciaGlobalDto[]>(this.INCIDENCIAS_URL, { params });
  }

  listarMovimientosGlobales(opts: { estado?: string; ctaOrigenId?: number; ctaDestinoId?: number } = {}): Observable<MovimientoGlobalDto[]> {
    let params = new HttpParams();
    if (opts.estado) params = params.set('estado', opts.estado);
    if (opts.ctaOrigenId !== undefined) params = params.set('ctaOrigenId', opts.ctaOrigenId.toString());
    if (opts.ctaDestinoId !== undefined) params = params.set('ctaDestinoId', opts.ctaDestinoId.toString());
    return this.http.get<MovimientoGlobalDto[]>(this.MOVIMIENTOS_URL, { params });
  }
}
