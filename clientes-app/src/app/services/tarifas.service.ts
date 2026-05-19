import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ConsultarTarifasRequest {
  peso: number;
  largo?: number;
  ancho?: number;
  alto?: number;
  codigoPostalOrigen?: string;
  codigoPostalDestino?: string;
  tipoServicio?: string;
}

/** Petición al endpoint POST /calcular — misma ruta de cálculo que usa CrearSesionPago */
export interface CalcularTarifaRequest {
  peso: number;
  largo?: number;
  ancho?: number;
  alto?: number;
  codigoPostalOrigen: string;
  codigoPostalDestino: string;
  tipoTarifa?: string;
}

/** Respuesta del endpoint POST /calcular */
export interface CalculoPrecioResponse {
  precioBase: number;
  recargo: number;
  iva: number;
  precioTotal: number;
  moneda: string;
  tiempoEntregaEstimado: string;
  tiempoEstimadoDias: number;
  tipoTarifa: string;
  zona: string;
  pesoFacturable: number;
  pesoVolumetrico: number;
  aplicaRecargo: boolean;
  recargoPorcentaje: number;
}

export interface TarifaDetalle {
  id: number;
  nombre: string;
  descripcion: string;
  tiempoEntregaEstimado: string;
  tiempoEstimadoDias: number;
  precioBase: number;
  recargo: number;
  iva: number;
  precioTotal: number;
  activa: boolean;
  precioEstimado?: number;
}

export interface TarifasResponse {
  tipoServicio: string;
  zona: string;
  pesoReal: number;
  pesoVolumetrico: number;
  pesoFacturable: number;
  aplicaRecargo: boolean;
  recargoPorcentaje: number;
  tarifas: TarifaDetalle[];
}

@Injectable({
  providedIn: 'root'
})
export class TarifasService {
  private readonly API_URL = '/api/nexopostal/tarifas';

  constructor(private http: HttpClient) {}

  consultarTarifas(request: ConsultarTarifasRequest): Observable<TarifasResponse> {
    const params: any = {
      peso: request.peso
    };

    if (request.largo != null) params.largo = request.largo;
    if (request.ancho != null) params.ancho = request.ancho;
    if (request.alto != null) params.alto = request.alto;
    if (request.codigoPostalOrigen) params.codigoPostalOrigen = request.codigoPostalOrigen;
    if (request.codigoPostalDestino) params.codigoPostalDestino = request.codigoPostalDestino;
    if (request.tipoServicio) params.tipoServicio = request.tipoServicio;

    return this.http.get<TarifasResponse>(`${this.API_URL}/consultar`, { params });
  }

  /**
   * Calcula el precio exacto para una tarifa concreta usando POST /calcular.
   * Usa exactamente la misma lógica que CrearSesionPago, garantizando
   * que el precio mostrado al usuario coincide con lo que Stripe cobrará.
   */
  calcularTarifa(request: CalcularTarifaRequest): Observable<CalculoPrecioResponse> {
    return this.http.post<CalculoPrecioResponse>(`${this.API_URL}/calcular`, request);
  }
}
