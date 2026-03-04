import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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

// ─── Servicio ───

@Injectable({ providedIn: 'root' })
export class ScanService {
  private readonly API_URL = '/api/scan';

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
}
