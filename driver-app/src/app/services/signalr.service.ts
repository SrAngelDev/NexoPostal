import { Injectable, signal, computed } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';

export interface NotificacionItem {
  id: string;
  evento: string;
  titulo: string;
  mensaje: string;
  payload: any;
  fechaUtc: string;
  leida: boolean;
}

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private connection: signalR.HubConnection | null = null;
  private readonly HUB_URL = '/hubs/reparto';

  estadoConexion = signal<'desconectado' | 'conectando' | 'conectado'>('desconectado');
  notificaciones = signal<NotificacionItem[]>([]);
  noLeidas = computed(() => this.notificaciones().filter(n => !n.leida).length);

  constructor(private auth: AuthService) {}

  async iniciar(): Promise<void> {
    if (this.connection) return;

    this.estadoConexion.set('conectando');

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.HUB_URL, {
        accessTokenFactory: () => this.auth.getToken() ?? ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.onreconnecting(() => this.estadoConexion.set('conectando'));
    this.connection.onreconnected(() => this.estadoConexion.set('conectado'));
    this.connection.onclose(() => this.estadoConexion.set('desconectado'));

    this.connection.on('RutaAsignada', (payload: any) => {
      this.agregarNotificacion({
        evento: 'RutaAsignada',
        titulo: 'Nueva ruta asignada',
        mensaje: payload?.mensaje ?? `Ruta ${payload?.codigo ?? ''} asignada`,
        payload
      });
    });

    this.connection.on('EntregaRegistrada', (payload: any) => {
      this.agregarNotificacion({
        evento: 'EntregaRegistrada',
        titulo: 'Entrega registrada',
        mensaje: `${payload?.numeroSeguimiento ?? ''} → ${payload?.estado ?? ''}`,
        payload
      });
    });

    try {
      await this.connection.start();
      this.estadoConexion.set('conectado');
    } catch (err) {
      console.warn('SignalR start error:', err);
      this.estadoConexion.set('desconectado');
    }
  }

  async detener(): Promise<void> {
    if (!this.connection) return;
    try { await this.connection.stop(); } catch {}
    this.connection = null;
    this.estadoConexion.set('desconectado');
  }

  marcarTodasLeidas(): void {
    this.notificaciones.update(list => list.map(n => ({ ...n, leida: true })));
  }

  limpiar(): void {
    this.notificaciones.set([]);
  }

  private agregarNotificacion(data: { evento: string; titulo: string; mensaje: string; payload: any }): void {
    const item: NotificacionItem = {
      id: crypto.randomUUID(),
      evento: data.evento,
      titulo: data.titulo,
      mensaje: data.mensaje,
      payload: data.payload,
      fechaUtc: new Date().toISOString(),
      leida: false
    };
    this.notificaciones.update(list => [item, ...list].slice(0, 50));
  }
}
