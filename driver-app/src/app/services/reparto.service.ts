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

export interface UbicacionActiva {
  repartidorId: number;
  nombreRepartidor: string;
  codigoEmpleado: string;
  oficinaJsonId: number;
  oficinaNombre: string;
  latitud: number;
  longitud: number;
  actualizadoEn: string;
  segundosDesdeActualizacion: number;
  rutaActivaId?: number;
  rutaCodigo?: string;
  rutaEstado?: string;
}

export interface EntregaPendienteAsignacion {
  entregaId: number;
  numeroExpedicion: string;
  numeroSeguimiento: string;
  direccionEntrega: string;
  codigoPostal: string;
  ciudad: string;
  nombreDestinatario: string;
  rutaActualId: number;
  rutaActualCodigo: string;
  repartidorActualId: number;
  repartidorActualNombre: string;
  oficinaJsonId: number;
  oficinaNombre: string;
  fechaReparto: string;
  estado: string;
}

export interface RutaResumen {
  id: number;
  codigo: string;
  fechaReparto: string;
  repartidorNombre: string;
  oficinaOrigenNombre: string;
  estado: string;
  totalEntregas: number;
  entregados: number;
  pendientes: number;
  fallidos: number;
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
    return this.http.post<RutaRepartoDetalle>(`${this.API_URL}/rutas/${rutaId}/iniciar`, {});
  }

  finalizarRuta(rutaId: number, request?: FinalizarRutaRequest): Observable<RutaRepartoDetalle> {
    return this.http.post<RutaRepartoDetalle>(
      `${this.API_URL}/rutas/${rutaId}/finalizar`,
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

  // ─── Endpoints del JefeReparto ───

  obtenerUbicacionesActivas(oficinaJsonId?: number, ventanaMinutos = 10): Observable<UbicacionActiva[]> {
    const params: Record<string, string | number> = { ventanaMinutos };
    if (oficinaJsonId !== undefined) params['oficinaJsonId'] = oficinaJsonId;
    return this.http.get<UbicacionActiva[]>(`${this.API_URL}/ubicaciones-activas`, { params });
  }

  obtenerEntregasPendientesAsignacion(oficinaJsonId?: number): Observable<EntregaPendienteAsignacion[]> {
    const params: Record<string, string | number> = {};
    if (oficinaJsonId !== undefined) params['oficinaJsonId'] = oficinaJsonId;
    return this.http.get<EntregaPendienteAsignacion[]>(`${this.API_URL}/entregas/pendientes-asignacion`, { params });
  }

  reasignarEntrega(entregaId: number, nuevaRutaId: number): Observable<EntregaPaquete> {
    return this.http.patch<EntregaPaquete>(`${this.API_URL}/entregas/${entregaId}/reasignar`, { nuevaRutaId });
  }

  obtenerRutas(fecha?: string): Observable<RutaResumen[]> {
    const params: Record<string, string> = {};
    if (fecha) params['fecha'] = fecha;
    return this.http.get<RutaResumen[]>(`${this.API_URL}/rutas`, { params });
  }
}
