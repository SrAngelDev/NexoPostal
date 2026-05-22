import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// ===== DTOs de Pago =====

export interface CrearSesionPagoRequest {
  // Paquete
  peso: number;
  dimensiones: string;
  codigoPostalOrigen: string;
  codigoPostalDestino: string;
  // Tarifa
  tipoTarifa: string;
  coste: number;
  tiempoEntregaEstimado: string;
  // Remitente
  nombreRemitente: string;
  apellidosRemitente: string;
  telefonoRemitente: string;
  emailRemitente: string;
  dniRemitente?: string;
  direccionOrigen: string;
  // Destinatario
  nombreDestinatario: string;
  apellidosDestinatario: string;
  telefonoDestinatario: string;
  emailDestinatario?: string;
  dniDestinatario?: string;
  direccionDestino: string;
  // Modalidad de entrega
  oficinaOrigenId?: number | null;
  tipoEntrega?: 'Domicilio' | 'Oficina';
  oficinaDestinoId?: number | null;
  // URL base para retorno de Stripe
  urlBase: string;
}

export interface SesionPagoCreadaResponse {
  sessionUrl: string;
  sessionId: string;
  numeroSeguimiento: string;
}

export interface VerificarPagoResponse {
  pagado: boolean;
  numeroSeguimiento: string;
  estado: string;
  precio: number;
  destino: string;
  tipoTarifa: string;
  tiempoEntregaEstimado: string;
  emailRemitente: string;
  fechaPago?: string;
}

export interface ReintentarPagoRequest {
  urlBase: string;
}

@Injectable({
  providedIn: 'root'
})
export class PagosService {
  private readonly API_URL = '/api/nexopostal/pagos';

  constructor(private http: HttpClient) {}

  /**
   * Crea una sesión de Stripe Checkout para pagar un envío.
   * Devuelve la URL a la que redirigir al usuario.
   */
  crearSesionPago(request: CrearSesionPagoRequest): Observable<SesionPagoCreadaResponse> {
    return this.http.post<SesionPagoCreadaResponse>(`${this.API_URL}/crear-sesion`, request);
  }

  /**
   * Verifica el estado de pago de una sesión de Stripe.
   * Se llama tras volver de Stripe a la página de éxito.
   */
  verificarPago(sessionId: string): Observable<VerificarPagoResponse> {
    return this.http.get<VerificarPagoResponse>(`${this.API_URL}/verificar/${sessionId}`);
  }

  /**
   * Reintenta el pago de un envío pendiente.
   * Crea una nueva sesión de Stripe para el mismo envío.
   */
  reintentarPago(numeroSeguimiento: string, request: ReintentarPagoRequest): Observable<SesionPagoCreadaResponse> {
    return this.http.post<SesionPagoCreadaResponse>(`${this.API_URL}/reintentar/${numeroSeguimiento}`, request);
  }
}
