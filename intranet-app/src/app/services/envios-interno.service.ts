import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// ============================
// DTOs para seguimiento INTERNO
// Solo accesible desde intranet-app (operarios)
// ============================

/** Detalle interno completo de un envío — consultado por NumeroExpedicion */
export interface EnvioInternoDetallado {
  // Identificadores
  numeroSeguimiento: string;
  numeroExpedicion: string;

  // Estados
  estadoPublico: string;
  estadoInterno: string;
  descripcionEstadoInterno: string;

  // Datos del paquete
  pesoKg: number;
  dimensiones: string;

  // Datos logísticos
  origen: string;
  destino: string;
  codigoPostalOrigen: string;
  codigoPostalDestino: string;

  // Remitente
  nombreRemitente: string;
  apellidosRemitente: string;
  telefonoRemitente: string;
  emailRemitente?: string;
  dniRemitente?: string;

  // Destinatario
  nombreDestinatario: string;
  apellidosDestinatario: string;
  telefonoDestinatario: string;
  emailDestinatario?: string;
  dniDestinatario?: string;

  // Administrativos
  tipoTarifa: string;
  tiempoEntregaEstimado: string;
  costeCalculado: number;
  pagado: boolean;
  fechaCreacion: string;
  fechaPago?: string;
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

/** Estados internos disponibles con su descripción */
export const ESTADOS_INTERNOS: { valor: string; grupo: string; etiqueta: string }[] = [
  // Fase de admisión
  { valor: 'PendienteRecogida', grupo: 'Admisión', etiqueta: 'Pendiente de recogida' },
  { valor: 'RecogidoEnOrigen', grupo: 'Admisión', etiqueta: 'Recogido en origen' },

  // Fase de clasificación origen
  { valor: 'RecibidoEnCentroOrigen', grupo: 'Clasificación origen', etiqueta: 'Recibido en centro de origen' },
  { valor: 'EnClasificacionOrigen', grupo: 'Clasificación origen', etiqueta: 'En clasificación (origen)' },
  { valor: 'ClasificadoParaExpedicion', grupo: 'Clasificación origen', etiqueta: 'Clasificado para expedición' },

  // Fase de tránsito
  { valor: 'EnTransitoHaciaCentroDestino', grupo: 'Tránsito', etiqueta: 'En tránsito hacia centro destino' },
  { valor: 'EnTransitoIntermedio', grupo: 'Tránsito', etiqueta: 'En tránsito (centro intermedio)' },

  // Fase de clasificación destino
  { valor: 'RecibidoEnCentroDestino', grupo: 'Clasificación destino', etiqueta: 'Recibido en centro de destino' },
  { valor: 'EnClasificacionDestino', grupo: 'Clasificación destino', etiqueta: 'En clasificación (destino)' },
  { valor: 'AsignadoARuta', grupo: 'Clasificación destino', etiqueta: 'Asignado a ruta de reparto' },

  // Fase de reparto
  { valor: 'EnReparto', grupo: 'Reparto', etiqueta: 'En reparto' },
  { valor: 'PrimerIntentoFallido', grupo: 'Reparto', etiqueta: 'Primer intento fallido' },
  { valor: 'SegundoIntentoFallido', grupo: 'Reparto', etiqueta: 'Segundo intento fallido' },
  { valor: 'DepositivoEnOficina', grupo: 'Reparto', etiqueta: 'Depositado en oficina' },

  // Fase de entrega
  { valor: 'EntregadoEnDomicilio', grupo: 'Entrega', etiqueta: 'Entregado en domicilio' },
  { valor: 'EntregadoEnOficina', grupo: 'Entrega', etiqueta: 'Entregado en oficina' },
  { valor: 'EntregadoAAutorizado', grupo: 'Entrega', etiqueta: 'Entregado a autorizado' },

  // Incidencias
  { valor: 'IncidenciaDireccionIncorrecta', grupo: 'Incidencia', etiqueta: 'Dirección incorrecta' },
  { valor: 'IncidenciaPaqueteDanado', grupo: 'Incidencia', etiqueta: 'Paquete dañado' },
  { valor: 'IncidenciaDestinatarioRechaza', grupo: 'Incidencia', etiqueta: 'Destinatario rechaza' },
  { valor: 'IncidenciaOtra', grupo: 'Incidencia', etiqueta: 'Otra incidencia' },

  // Devolución
  { valor: 'EnDevolucionAlRemitente', grupo: 'Devolución', etiqueta: 'En devolución al remitente' },
  { valor: 'DevueltoAlRemitente', grupo: 'Devolución', etiqueta: 'Devuelto al remitente' },
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
   * Obtiene el detalle interno de un envío por su número de seguimiento público (NX...ES)
   */
  obtenerPorSeguimiento(numero: string): Observable<EnvioInternoDetallado> {
    return this.http.get<EnvioInternoDetallado>(`${this.API_URL}/por-seguimiento/${numero}`);
  }

  /**
   * Lista todos los envíos con datos internos.
   * Se pueden filtrar por estado interno y/o código postal destino.
   */
  listarEnvios(filtros?: { estadoInterno?: string; codigoPostal?: string }): Observable<EnvioResumenInterno[]> {
    let params: any = {};
    if (filtros?.estadoInterno) params.estadoInterno = filtros.estadoInterno;
    if (filtros?.codigoPostal) params.codigoPostal = filtros.codigoPostal;
    return this.http.get<EnvioResumenInterno[]>(`${this.API_URL}/listar`, { params });
  }

  /**
   * Actualiza el estado interno de un envío.
   * El estado público se sincroniza automáticamente en el backend.
   */
  actualizarEstado(expedicion: string, request: ActualizarEstadoInternoRequest): Observable<EnvioInternoDetallado> {
    // Usa ruta 'interno-estado' para que el gateway enrute el PUT correctamente
    return this.http.put<EnvioInternoDetallado>(`/api/nexopostal/envios/interno-estado/${expedicion}/estado`, request);
  }

  /**
   * Valida el formato de un número de expedición interno
   */
  validarNumeroExpedicion(codigo: string): boolean {
    // Formato interno: NXI- + 8 alfanuméricos
    const regex = /^NXI-[A-Z0-9]{8}$/;
    return regex.test(codigo);
  }

  /**
   * Formatea el estado interno para mostrar al operario
   */
  formatearEstadoInterno(estado: string): string {
    const found = ESTADOS_INTERNOS.find(e => e.valor === estado);
    return found ? found.etiqueta : estado;
  }

  /**
   * Devuelve la clase CSS según el grupo del estado interno
   */
  getEstadoClase(estado: string): string {
    const found = ESTADOS_INTERNOS.find(e => e.valor === estado);
    if (!found) return 'estado-desconocido';
    const clases: Record<string, string> = {
      'Admisión': 'estado-admision',
      'Clasificación origen': 'estado-clasificacion',
      'Tránsito': 'estado-transito',
      'Clasificación destino': 'estado-clasificacion',
      'Reparto': 'estado-reparto',
      'Entrega': 'estado-entregado',
      'Incidencia': 'estado-incidencia',
      'Devolución': 'estado-devolucion',
    };
    return clases[found.grupo] || 'estado-desconocido';
  }
}
