import { Injectable, OnDestroy } from '@angular/core';
import { Subject, BehaviorSubject } from 'rxjs';
import * as signalR from '@microsoft/signalr';

// ─── Interfaces para los eventos de tracking ───

export interface TrackingEstadoEvento {
  numeroSeguimiento: string;
  estado: string;
  estadoAnterior: string;
  ubicacion: string;
  descripcion: string;
  fecha: string;
  visibleParaCliente: boolean;
}

export interface TrackingUbicacionEvento {
  numeroSeguimiento: string;
  tipoUbicacion: string;
  ubicacionNombre: string;
  fecha: string;
}

export interface TrackingEntregaEvento {
  numeroSeguimiento: string;
  fechaEntrega: string;
  receptorNombre: string;
}

export interface TrackingIncidenciaEvento {
  numeroSeguimiento: string;
  tipo: string;
  descripcion: string;
  fecha: string;
}

export type EstadoConexion = 'desconectado' | 'conectando' | 'conectado' | 'error';

/**
 * Servicio de tracking en tiempo real para envíos de NexoPostal.
 *
 * Utiliza SignalR para recibir actualizaciones push del servidor
 * sin necesidad de polling. El cliente se suscribe a un número de
 * seguimiento y recibe eventos (cambio de estado, ubicación,
 * entrega, incidencia) a medida que ocurren.
 *
 * Uso:
 *   trackingService.conectar();
 *   trackingService.suscribir('NP-2025-XXXXXX');
 *   trackingService.estadoActualizado$.subscribe(e => ...);
 */
@Injectable({
  providedIn: 'root'
})
export class TrackingService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;
  private suscripcionActual: string | null = null;

  // ─── Observables públicos para los componentes ───

  /** Estado de la conexión SignalR */
  private _estadoConexion = new BehaviorSubject<EstadoConexion>('desconectado');
  readonly estadoConexion$ = this._estadoConexion.asObservable();

  /** Emite cuando cambia el estado de un envío */
  private _estadoActualizado = new Subject<TrackingEstadoEvento>();
  readonly estadoActualizado$ = this._estadoActualizado.asObservable();

  /** Emite cuando cambia la ubicación del envío */
  private _ubicacionActualizada = new Subject<TrackingUbicacionEvento>();
  readonly ubicacionActualizada$ = this._ubicacionActualizada.asObservable();

  /** Emite cuando el envío ha sido entregado */
  private _entregaCompletada = new Subject<TrackingEntregaEvento>();
  readonly entregaCompletada$ = this._entregaCompletada.asObservable();

  /** Emite cuando se detecta una incidencia */
  private _incidenciaDetectada = new Subject<TrackingIncidenciaEvento>();
  readonly incidenciaDetectada$ = this._incidenciaDetectada.asObservable();

  /** Confirmación de suscripción exitosa */
  private _suscripcionConfirmada = new Subject<string>();
  readonly suscripcionConfirmada$ = this._suscripcionConfirmada.asObservable();

  // ─── Conexión ───

  /**
   * Establece la conexión WebSocket con el hub de tracking.
   * En desarrollo usa el proxy; en producción usa la URL del API Gateway.
   */
  conectar(): void {
    if (this.hubConnection) return;

    this._estadoConexion.next('conectando');

    // La URL se resuelve por el proxy en desarrollo
    // y por nginx en producción (wss://nexopostal.es/hubs/tracking)
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/tracking')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Registrar handlers ANTES de iniciar la conexión
    this.registrarHandlers();

    // Eventos de reconexión
    this.hubConnection.onreconnecting(() => {
      this._estadoConexion.next('conectando');
    });

    this.hubConnection.onreconnected(() => {
      this._estadoConexion.next('conectado');
      // Re-suscribir al número de seguimiento actual si había uno
      if (this.suscripcionActual) {
        this.suscribirInterno(this.suscripcionActual);
      }
    });

    this.hubConnection.onclose(() => {
      this._estadoConexion.next('desconectado');
    });

    // Iniciar conexión
    this.hubConnection
      .start()
      .then(() => {
        this._estadoConexion.next('conectado');
      })
      .catch(err => {
        console.error('Error al conectar con TrackingHub:', err);
        this._estadoConexion.next('error');
      });
  }

  /**
   * Cierra la conexión con el hub.
   */
  desconectar(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.suscripcionActual = null;
      this._estadoConexion.next('desconectado');
    }
  }

  // ─── Suscripciones ───

  /**
   * Se suscribe al tracking de un número de seguimiento.
   * Si ya estaba suscrito a otro, se desuscribe automáticamente.
   */
  suscribir(numeroSeguimiento: string): void {
    if (!this.hubConnection || this._estadoConexion.value !== 'conectado') {
      console.warn('No hay conexión activa. Conectando...');
      this.conectar();
      // Reintentar después de la conexión
      const sub = this.estadoConexion$.subscribe(estado => {
        if (estado === 'conectado') {
          this.suscribirInterno(numeroSeguimiento);
          sub.unsubscribe();
        }
      });
      return;
    }

    this.suscribirInterno(numeroSeguimiento);
  }

  /**
   * Cancela la suscripción al número de seguimiento actual.
   */
  desuscribir(): void {
    if (this.hubConnection && this.suscripcionActual) {
      this.hubConnection.invoke('DesuscribirTracking', this.suscripcionActual)
        .catch(err => console.error('Error al desuscribir:', err));
      this.suscripcionActual = null;
    }
  }

  // ─── Internos ───

  private suscribirInterno(numeroSeguimiento: string): void {
    if (!this.hubConnection) return;

    // Desuscribir del anterior si existe
    if (this.suscripcionActual && this.suscripcionActual !== numeroSeguimiento) {
      this.hubConnection.invoke('DesuscribirTracking', this.suscripcionActual)
        .catch(() => {});
    }

    this.hubConnection.invoke('SuscribirTracking', numeroSeguimiento)
      .then(() => {
        this.suscripcionActual = numeroSeguimiento;
      })
      .catch(err => console.error('Error al suscribir tracking:', err));
  }

  /**
   * Registra los handlers para los eventos del hub.
   * Los nombres deben coincidir con los del TrackingHub.cs en el servidor.
   */
  private registrarHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('EstadoActualizado', (evento: TrackingEstadoEvento) => {
      this._estadoActualizado.next(evento);
    });

    this.hubConnection.on('UbicacionActualizada', (evento: TrackingUbicacionEvento) => {
      this._ubicacionActualizada.next(evento);
    });

    this.hubConnection.on('EntregaCompletada', (evento: TrackingEntregaEvento) => {
      this._entregaCompletada.next(evento);
    });

    this.hubConnection.on('IncidenciaDetectada', (evento: TrackingIncidenciaEvento) => {
      this._incidenciaDetectada.next(evento);
    });

    this.hubConnection.on('SuscripcionConfirmada', (info: { mensaje: string }) => {
      if (this.suscripcionActual) {
        this._suscripcionConfirmada.next(this.suscripcionActual);
      }
    });
  }

  ngOnDestroy(): void {
    this.desconectar();
  }
}
