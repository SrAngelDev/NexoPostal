import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type EstadoEntregaConfirmacion =
  | 'Entregado'
  | 'Ausente'
  | 'DireccionIncorrecta'
  | 'Rechazado'
  | 'EntregadoPuntoAlternativo'
  | 'DevueltoAOficina';

export interface RepartidorPerfil {
  id: number;
  nombreCompleto: string;
  codigoEmpleado: string;
  telefono?: string;
  oficinaJsonId: number;
  oficinaNombre: string;
  tipoVehiculo: string;
  activo: boolean;
  rutasHoy: number;
}

export interface EntregaPaquete {
  id: number;
  numeroExpedicion: string;
  numeroSeguimiento: string;
  direccionEntrega: string;
  codigoPostal: string;
  ciudad: string;
  nombreDestinatario: string;
  telefonoDestinatario?: string;
  numeroIntento: number;
  ordenEnRuta: number;
  estado: string;
  fechaIntento?: string;
  receptorNombre?: string;
  receptorDni?: string;
  observaciones?: string;
  latitudEntrega?: number;
  longitudEntrega?: number;
  firmaDigital?: string;
  fotoEntrega?: string;
}

export interface RutaRepartoDetalle {
  id: number;
  codigo: string;
  fechaReparto: string;
  repartidorId: number;
  repartidorNombre: string;
  oficinaOrigenJsonId: number;
  oficinaOrigenNombre: string;
  estado: string;
  horaSalida?: string;
  horaRegreso?: string;
  observaciones?: string;
  entregas: EntregaPaquete[];
}

export interface FinalizarRutaRequest {
  observaciones?: string;
}

export interface RegistrarEntregaRequest {
  estado: EstadoEntregaConfirmacion;
  receptorNombre?: string;
  receptorDni?: string;
  observaciones?: string;
  latitud?: number;
  longitud?: number;
  firmaDigital?: string;
  fotoEntrega?: string;
}

export interface UbicacionRepartidorRequest {
  latitud: number;
  longitud: number;
  rutaId?: number;
  numeroSeguimiento?: string;
  tipoUbicacion?: string;
  descripcion?: string;
}

export const ESTADOS_CONFIRMACION: { valor: EstadoEntregaConfirmacion; etiqueta: string; icono: string }[] = [
  { valor: 'Entregado', etiqueta: 'Entregado', icono: 'home' },
  { valor: 'EntregadoPuntoAlternativo', etiqueta: 'Entregado punto alternativo', icono: 'storefront' },
  { valor: 'Ausente', etiqueta: 'Ausente', icono: 'person_off' },
  { valor: 'DireccionIncorrecta', etiqueta: 'Dirección incorrecta', icono: 'wrong_location' },
  { valor: 'Rechazado', etiqueta: 'Rechazado', icono: 'block' },
  { valor: 'DevueltoAOficina', etiqueta: 'Devuelto a oficina', icono: 'inventory_2' }
];

@Injectable({
  providedIn: 'root'
})
export class RepartoService {
  private readonly API_URL = '/api/nexopostal/reparto';

  constructor(private http: HttpClient) {}

  obtenerMiPerfil(): Observable<RepartidorPerfil> {
    return this.http.get<RepartidorPerfil>(`${this.API_URL}/mi-perfil`);
  }

  obtenerMiRuta(): Observable<RutaRepartoDetalle[]> {
    return this.http.get<RutaRepartoDetalle[]>(`${this.API_URL}/ruta`);
  }

  obtenerEntregas(rutaId: number): Observable<EntregaPaquete[]> {
    return this.http.get<EntregaPaquete[]>(`${this.API_URL}/entregas`, {
      params: { rutaId }
    });
  }

  iniciarRuta(rutaId: number): Observable<RutaRepartoDetalle> {
    return this.http.post<RutaRepartoDetalle>(`${this.API_URL}/ruta-iniciar/${rutaId}/iniciar`, {});
  }

  finalizarRuta(rutaId: number, request?: FinalizarRutaRequest): Observable<RutaRepartoDetalle> {
    return this.http.post<RutaRepartoDetalle>(
      `${this.API_URL}/ruta-finalizar/${rutaId}/finalizar`,
      request ?? {}
    );
  }

  confirmarEntrega(entregaId: number, request: RegistrarEntregaRequest): Observable<EntregaPaquete> {
    return this.http.post<EntregaPaquete>(`${this.API_URL}/confirmar`, request, {
      params: { entregaId }
    });
  }

  registrarUbicacion(request: UbicacionRepartidorRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.API_URL}/ubicacion`, request);
  }
}
