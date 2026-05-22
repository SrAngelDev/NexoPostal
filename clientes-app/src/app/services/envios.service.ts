import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

// DTOs para Cotización
export interface CotizarEnvioRequest {
  peso: number;
  dimensiones?: string;
  codigoPostalOrigen: string;
  codigoPostalDestino: string;
}

export interface CotizacionResultado {
  precio: number;
  moneda: string;
  tiempoEstimadoDias: number;
  observaciones: string;
}

// DTOs para Envío
export interface CrearEnvioRequest {
  peso: number;
  dimensiones?: string;
  descripcion?: string;
  valorDeclarado?: number;
  remitente: DireccionEnvio;
  destinatario: DireccionEnvio;
}

export interface DireccionEnvio {
  nombre: string;
  direccion: string;
  ciudad: string;
  codigoPostal: string;
  telefono?: string;
  email?: string;
}

export interface EnvioResponse {
  numeroSeguimiento: string;
  estado: string;
  fechaCreacion: string;
  destino: string;
  precio: number;
  pagado: boolean;
  tipoTarifa: string;
}

// DTOs para Trazabilidad
export interface EventoTrazabilidad {
  id: number;
  fecha: string;
  ubicacion: string;
  estado: string;
  observaciones?: string;
}

export interface TrazabilidadResponse {
  numeroSeguimiento: string;
  estadoActual: string;
  /** Estado interno detallado (nombre del enum EstadoInterno backend). Usado por la barra de progreso. */
  estadoInterno?: string;
  descripcion?: string;
  fechaCreacion: string;
  fechaEntrega?: string;
  numeroBultos?: number;
  eventos: EventoTrazabilidad[];
}

@Injectable({
  providedIn: 'root'
})
export class EnviosService {
  private readonly API_URL = '/api/nexopostal/envios';

  constructor(private http: HttpClient) {}

  /**
   * Cotiza el precio de un envío sin necesidad de autenticación
   * @param request Datos del paquete (peso, dimensiones, origen, destino)
   * @returns Precio estimado y tiempo de entrega
   */
  cotizarEnvio(request: CotizarEnvioRequest): Observable<CotizacionResultado> {
    return this.http.post<CotizacionResultado>(`${this.API_URL}/cotizar`, request);
  }

  /**
   * Crea un nuevo envío (requiere autenticación)
   * @param request Datos completos del envío
   * @returns Envío creado con número de seguimiento
   */
  crearEnvio(request: CrearEnvioRequest): Observable<EnvioResponse> {
    return this.http.post<EnvioResponse>(`${this.API_URL}/crear`, request);
  }

  /**
   * Consulta un envío por su número de seguimiento
   * @param numeroSeguimiento Número de seguimiento del envío
   * @returns Datos del envío
   */
  consultarEnvio(numeroSeguimiento: string): Observable<EnvioResponse> {
    return this.http.get<EnvioResponse>(`${this.API_URL}/track/${numeroSeguimiento}`);
  }

  /**
   * Obtiene la trazabilidad completa de un envío
   * @param numeroSeguimiento Número de seguimiento del envío
   * @returns Historial de eventos de trazabilidad
   */
  obtenerTrazabilidad(numeroSeguimiento: string): Observable<TrazabilidadResponse> {
    return this.http.get<TrazabilidadResponse>(`${this.API_URL}/track/${numeroSeguimiento}`);
  }

  /**
   * Obtiene todos los envíos del usuario autenticado
   * @returns Lista de envíos del usuario
   */
  obtenerMisEnvios(): Observable<EnvioResponse[]> {
    return this.http.get<EnvioResponse[]>(`${this.API_URL}/mis-envios`);
  }

  /**
   * Descarga la etiqueta de un envío en formato PDF
   * @param numeroSeguimiento Número de seguimiento del envío
   * @returns Blob del archivo PDF
   */
  descargarEtiqueta(numeroSeguimiento: string): Observable<Blob> {
    return this.http.get(`${this.API_URL}/etiqueta/${numeroSeguimiento}`, {
      responseType: 'blob'
    });
  }

  /**
   * Descarga la factura de un envío en formato PDF
   * @param numeroSeguimiento Número de seguimiento del envío
   * @returns Blob del archivo PDF
   */
  descargarFactura(numeroSeguimiento: string): Observable<Blob> {
    return this.http.get(`${this.API_URL}/factura/${numeroSeguimiento}`, {
      responseType: 'blob'
    });
  }

  /**
   * Método auxiliar para descargar y guardar la etiqueta
   * @param numeroSeguimiento Número de seguimiento del envío
   */
  descargarYGuardarEtiqueta(numeroSeguimiento: string): void {
    this.descargarEtiqueta(numeroSeguimiento).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Etiqueta_${numeroSeguimiento}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (error) => {
        console.error('Error descargando etiqueta:', error);
      }
    });
  }

  /**
   * Valida el formato de un número de seguimiento público
   * @param numero Número de seguimiento a validar
   * @returns true si el formato es válido (NX + 11 dígitos + ES)
   */
  validarNumeroSeguimiento(numero: string): boolean {
    // Formato público: NX + dígitos + ES (ej: NX12345678999ES)
    const regex = /^NX\d{9,11}ES$/;
    return regex.test(numero);
  }

  /**
   * Formatea el estado del envío para mostrar al usuario
   * @param estado Estado del envío
   * @returns Estado formateado
   */
  formatearEstado(estado: string): string {
    const estados: { [key: string]: string } = {
      'Pendiente': 'Pendiente de admisión',
      'Admitido': 'Admitido en oficina',
      'EnTransito': 'En tránsito',
      'EnReparto': 'En reparto',
      'Entregado': 'Entregado',
      'Devuelto': 'Devuelto al remitente',
      'Incidencia': 'Incidencia'
    };
    
    return estados[estado] || estado;
  }
}
