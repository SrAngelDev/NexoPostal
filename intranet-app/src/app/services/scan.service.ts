import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

// ─── Interfaces ───

export interface ModoEscaneo {
  valor: string;
  etiqueta: string;
  icono: string;
  requiere: 'cta' | 'oficina';
}

export interface ScanRequest {
  codigoEscaneado: string;
  modoOperacion: string;
  ctaId?: number;
  ctaCodigo?: string;
  oficinaJsonId?: number;
  oficinaNombre?: string;
  codigoPostalDestino?: string;
  codigoPostalOrigen?: string;
  operarioNombre?: string;
  esUrgente?: boolean;
  observaciones?: string;
}

export interface ScanResult {
  exito: boolean;
  numeroExpedicion: string;
  modoOperacion: string;
  modoDescripcion: string;
  estadoAnterior?: string;
  estadoNuevo: string;
  ubicacionNombre?: string;
  mensaje: string;
  detalles?: string;
  fechaProcesado: string;
  movimientoTroncalCreado: boolean;
  notificacionEnviada: boolean;
}

export interface ScanBatchRequest {
  codigosEscaneados: string[];
  modoOperacion: string;
  ctaId?: number;
  ctaCodigo?: string;
  oficinaJsonId?: number;
  oficinaNombre?: string;
  operarioNombre?: string;
}

export interface ScanBatchResult {
  totalEscaneados: number;
  exitosos: number;
  fallidos: number;
  resultados: ScanResult[];
}

export interface OficinaJsonDto {
  id: number;
  nombre: string;
  direccion: string;
  codigoPostal: string;
  ciudad: string;
  provincia?: string;
  telefono?: string;
  email?: string;
  esCentral?: boolean;
}

export interface MiOficinaInfoDto {
  oficinaJsonId: number;
  oficinaNombre: string;
  codigoPostal: string;
  ciudad: string;
  direccion: string;
  rol: string;
  activo: boolean;
  fechaAsignacion: string;
}

// ─── Servicio ───

@Injectable({ providedIn: 'root' })
export class ScanService {
  private readonly API_URL = '/api/scan';
  private readonly OFICINAS_URL = '/api/oficinaspostales';
  private readonly OPERARIOS_URL = '/api/operarios';

  constructor(private http: HttpClient) {}

  /**
   * Obtiene los modos de escaneo disponibles.
   */
  obtenerModos(): Observable<ModoEscaneo[]> {
    return this.http.get<ModoEscaneo[]>(`${this.API_URL}/modos`);
  }

  /**
   * Procesa un escaneo individual.
   */
  procesar(request: ScanRequest): Observable<ScanResult> {
    return this.http.post<ScanResult>(`${this.API_URL}/procesar`, request);
  }

  /**
   * Procesa un lote de escaneos.
   */
  procesarLote(request: ScanBatchRequest): Observable<ScanBatchResult> {
    return this.http.post<ScanBatchResult>(`${this.API_URL}/procesar-lote`, request);
  }

  /**
   * Valida que el código tenga formato de expedición NexoPostal.
   */
  validarCodigo(codigo: string): boolean {
    return /^NXI-[A-Z0-9]{8}$/.test(codigo.toUpperCase());
  }

  /**
   * Lista las oficinas cuyo CP cae dentro de las rutas del CTA indicado.
   */
  obtenerOficinasPorCta(ctaId: number): Observable<OficinaJsonDto[]> {
    return this.http.get<OficinaJsonDto[]>(`${this.OFICINAS_URL}/por-cta/${ctaId}`);
  }

  /**
   * Devuelve la oficina asignada al operario autenticado (si existe).
   * El backend responde 204 si no hay asignación.
   */
  obtenerMiOficina(): Observable<MiOficinaInfoDto | null> {
    return this.http
      .get<MiOficinaInfoDto>(`${this.OPERARIOS_URL}/mi-oficina`, { observe: 'response' })
      .pipe(map(r => (r.status === 204 ? null : r.body)));
  }
}
