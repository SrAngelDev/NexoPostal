import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';

// ============================================================
//  Notificación recibida vía SignalR
// ============================================================
export interface NotificacionSignalR {
  tipo: string;
  titulo: string;
  mensaje: string;
  ctaId: number;
  ctaCodigo: string;
  numeroExpedicion?: string;
  esUrgente: boolean;
  fechaHora: string;
  datos?: any;
}

export interface ConexionInfo {
  operarioId: number;
  nombre: string;
  rol: string;
  ctaId: number;
  ctaCodigo: string;
  ctaNombre: string;
  mensaje: string;
}

export interface CtaCambiadaPayload {
  operarioCtaId: number;
  ctaAnteriorId: number;
  ctaAnteriorCodigo: string;
  ctaNuevoId: number;
  ctaNuevoCodigo: string;
  ctaNuevoNombre: string;
  mensaje: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection: signalR.HubConnection | null = null;

  // Estado reactivo con signals
  readonly conectado = signal(false);
  readonly conexionInfo = signal<ConexionInfo | null>(null);
  readonly notificaciones = signal<NotificacionSignalR[]>([]);
  readonly notificacionesNoLeidas = signal(0);
  readonly ultimaNotificacion = signal<NotificacionSignalR | null>(null);
  readonly ctaCambiada = signal<CtaCambiadaPayload | null>(null);

  constructor(private authService: AuthService) {}

  /**
   * Inicia la conexión SignalR al Hub de la Intranet.
   * El Hub asigna automáticamente al operario a los grupos de su CTA.
   */
  conectar(): void {
    if (this.hubConnection) return;

    const token = this.authService.getToken();
    if (!token) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/intranet', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Registrar listeners antes de conectar
    this.registrarEventos();

    // Conectar
    this.hubConnection.start()
      .then(() => {
        this.conectado.set(true);
        console.log('✅ SignalR conectado al Hub de Intranet');
      })
      .catch(err => {
        console.error('❌ Error conectando SignalR:', err);
        this.conectado.set(false);
      });

    // Reconexión 
    this.hubConnection.onreconnecting(() => {
      this.conectado.set(false);
      console.log('🔄 Reconectando SignalR...');
    });

    this.hubConnection.onreconnected(() => {
      this.conectado.set(true);
      console.log('✅ SignalR reconectado');
    });

    this.hubConnection.onclose(() => {
      this.conectado.set(false);
      this.hubConnection = null;
      console.log('🔌 SignalR desconectado');
    });
  }

  /**
   * Desconecta del Hub SignalR
   */
  desconectar(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.conectado.set(false);
      this.conexionInfo.set(null);
    }
  }

  /**
   * Desconecta y vuelve a conectar (útil tras un cambio de CTA)
   */
  reconectar(): void {
    this.desconectar();
    setTimeout(() => this.conectar(), 500);
  }

  /**
   * Marca todas las notificaciones como leídas
   */
  marcarComoLeidas(): void {
    this.notificacionesNoLeidas.set(0);
  }

  /**
   * Limpia todas las notificaciones
   */
  limpiarNotificaciones(): void {
    this.notificaciones.set([]);
    this.notificacionesNoLeidas.set(0);
  }

  // ─── Registro de eventos SignalR ───
  private registrarEventos(): void {
    if (!this.hubConnection) return;

    // Confirmación de conexión (enviada por el Hub al conectarse)
    this.hubConnection.on('ConexionEstablecida', (info: ConexionInfo) => {
      this.conexionInfo.set(info);
      console.log(`📡 Conectado a ${info.ctaCodigo} como ${info.rol}`);
    });

    // Eventos de paquetes
    this.hubConnection.on('PaqueteRecibidoEnCta', (n: NotificacionSignalR) => this.agregarNotificacion(n));
    
    // Eventos de tareas
    this.hubConnection.on('TareaAsignada', (n: NotificacionSignalR) => this.agregarNotificacion(n));
    this.hubConnection.on('TareaIniciada', (n: NotificacionSignalR) => this.agregarNotificacion(n));
    this.hubConnection.on('TareaCompletada', (n: NotificacionSignalR) => this.agregarNotificacion(n));
    this.hubConnection.on('TareaCancelada', (n: NotificacionSignalR) => this.agregarNotificacion(n));

    // Eventos de movimientos
    this.hubConnection.on('MovimientoDespachado', (n: NotificacionSignalR) => this.agregarNotificacion(n));
    this.hubConnection.on('MovimientoRecibido', (n: NotificacionSignalR) => this.agregarNotificacion(n));

    // Eventos de incidencias
    this.hubConnection.on('IncidenciaCreada', (n: NotificacionSignalR) => this.agregarNotificacion(n));
    this.hubConnection.on('IncidenciaActualizada', (n: NotificacionSignalR) => this.agregarNotificacion(n));

    // Notificación general
    this.hubConnection.on('NotificacionGeneral', (n: NotificacionSignalR) => this.agregarNotificacion(n));

    // Cambio de CTA asignado al operario
    this.hubConnection.on('CtaCambiada', (payload: CtaCambiadaPayload) => {
      this.ctaCambiada.set(payload);
    });
  }

  private agregarNotificacion(n: NotificacionSignalR): void {
    const actuales = this.notificaciones();
    // Máximo 50 notificaciones en memoria
    const nuevas = [n, ...actuales].slice(0, 50);
    this.notificaciones.set(nuevas);
    this.notificacionesNoLeidas.update(c => c + 1);
    this.ultimaNotificacion.set(n);
  }
}
