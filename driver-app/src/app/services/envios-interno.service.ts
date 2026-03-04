import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// ============================
// DTOs para seguimiento INTERNO (repartidores)
// Solo accesible desde driver-app
// ============================

/** Detalle interno completo de un envío — consultado por NumeroExpedicion */
export interface EnvioInternoDetallado {
  numeroSeguimiento: string;
  numeroExpedicion: string;
  estadoPublico: string;
  estadoInterno: string;
  descripcionEstadoInterno: string;
  pesoKg: number;
  dimensiones: string;
  origen: string;
  destino: string;
  codigoPostalOrigen: string;
  codigoPostalDestino: string;
  nombreRemitente: string;
  apellidosRemitente: string;
  telefonoRemitente: string;
  emailRemitente?: string;
  nombreDestinatario: string;
  apellidosDestinatario: string;
  telefonoDestinatario: string;
  emailDestinatario?: string;
  tipoTarifa: string;
  tiempoEntregaEstimado: string;
  costeCalculado: number;
  pagado: boolean;
  fechaCreacion: string;
  observaciones?: string;
}

/** Resumen interno de un envío para listados */
export interface EnvioResumenInterno {
  numeroSeguimiento: string;
  numeroExpedicion: string;
  estadoPublico: string;
  estadoInterno: string;
  fechaCreacion: string;
  origen: string;
  destino: string;
  codigoPostalDestino: string;
  pesoKg: number;
  tipoTarifa: string;
  pagado: boolean;
}

/** Request para actualizar el estado interno */
export interface ActualizarEstadoInternoRequest {
  nuevoEstadoInterno: string;
  observaciones?: string;
}

/** Estados que puede aplicar un repartidor */
export const ESTADOS_REPARTIDOR: { valor: string; etiqueta: string; icono: string }[] = [
  { valor: 'RecogidoEnOrigen', etiqueta: 'Recogido en origen', icono: 'inventory_2' },
  { valor: 'EnReparto', etiqueta: 'En reparto', icono: 'local_shipping' },
  { valor: 'PrimerIntentoFallido', etiqueta: 'Intento fallido (1o)', icono: 'error_outline' },
  { valor: 'SegundoIntentoFallido', etiqueta: 'Intento fallido (2o)', icono: 'error' },
  { valor: 'DepositivoEnOficina', etiqueta: 'Depositado en oficina', icono: 'store' },
  { valor: 'EntregadoEnDomicilio', etiqueta: 'Entregado en domicilio', icono: 'home' },
  { valor: 'EntregadoEnOficina', etiqueta: 'Entregado en oficina', icono: 'storefront' },
  { valor: 'EntregadoAAutorizado', etiqueta: 'Entregado a autorizado', icono: 'person_check' },
  { valor: 'IncidenciaDireccionIncorrecta', etiqueta: 'Dirección incorrecta', icono: 'wrong_location' },
  { valor: 'IncidenciaPaqueteDanado', etiqueta: 'Paquete dañado', icono: 'report_problem' },
  { valor: 'IncidenciaDestinatarioRechaza', etiqueta: 'Destinatario rechaza', icono: 'block' },
  { valor: 'IncidenciaOtra', etiqueta: 'Otra incidencia', icono: 'flag' },
];

@Injectable({
  providedIn: 'root'
})
export class EnviosInternoService {
  private readonly API_URL = '/api/nexopostal/envios/interno';

  constructor(private http: HttpClient) {}

  /**
   * Obtiene el detalle interno de un envío por su número de expedición (NXI-...)
   */
  obtenerPorExpedicion(expedicion: string): Observable<EnvioInternoDetallado> {
    return this.http.get<EnvioInternoDetallado>(`${this.API_URL}/${expedicion}`);
  }

  /**
   * Lista envíos con datos internos, filtrados por estado y/o CP
   */
  listarEnvios(filtros?: { estadoInterno?: string; codigoPostal?: string }): Observable<EnvioResumenInterno[]> {
    let params: any = {};
    if (filtros?.estadoInterno) params.estadoInterno = filtros.estadoInterno;
    if (filtros?.codigoPostal) params.codigoPostal = filtros.codigoPostal;
    return this.http.get<EnvioResumenInterno[]>(`${this.API_URL}/listar`, { params });
  }

  /**
   * Actualiza el estado interno de un envío (al escanear el código de barras interno)
   */
  actualizarEstado(expedicion: string, request: ActualizarEstadoInternoRequest): Observable<EnvioInternoDetallado> {
    // Usa ruta 'interno-estado' para que el gateway enrute el PUT correctamente
    return this.http.put<EnvioInternoDetallado>(`/api/nexopostal/envios/interno-estado/${expedicion}/estado`, request);
  }

  /**
   * Valida el formato de un número de expedición interno
   */
  validarNumeroExpedicion(codigo: string): boolean {
    const regex = /^NXI-[A-Z0-9]{8}$/;
    return regex.test(codigo);
  }

  /**
   * Formatea el estado interno para mostrar al repartidor
   */
  formatearEstadoInterno(estado: string): string {
    const found = ESTADOS_REPARTIDOR.find(e => e.valor === estado);
    return found ? found.etiqueta : estado;
  }
}
