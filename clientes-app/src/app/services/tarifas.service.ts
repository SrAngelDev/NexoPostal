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

export interface TarifaDetalle {
  id: number;
  nombre: string;
  descripcion: string;
  tiempoEntregaEstimado: string;
  tiempoEstimadoDias: number;
  precioBase: number;
  recargo: number;
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
}
