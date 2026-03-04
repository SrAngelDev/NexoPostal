import { Injectable, signal } from '@angular/core';

/** Tipos de notificación disponibles */
export type TipoNotificacion = 'exito' | 'error' | 'aviso' | 'info';

/** Interfaz de una notificación */
export interface Notificacion {
  id: number;
  tipo: TipoNotificacion;
  titulo: string;
  mensaje: string;
  detalles?: string;
  autoCerrar: boolean;
  duracion: number; // ms
}

@Injectable({ providedIn: 'root' })
export class NotificacionService {
  private _id = 0;

  /** Cola de notificaciones activas */
  readonly notificaciones = signal<Notificacion[]>([]);

  // ===== MÉTODOS PÚBLICOS =====

  /** Notificación de éxito — se cierra sola en 3s */
  exito(titulo: string, mensaje: string = ''): void {
    this.agregar({ tipo: 'exito', titulo, mensaje, autoCerrar: true, duracion: 3000 });
  }

  /** Notificación de error — requiere cierre manual, muestra detalles */
  error(titulo: string, mensaje: string = '', detalles?: string): void {
    this.agregar({ tipo: 'error', titulo, mensaje, detalles, autoCerrar: false, duracion: 0 });
  }

  /** Notificación de aviso — se cierra sola en 5s */
  aviso(titulo: string, mensaje: string = ''): void {
    this.agregar({ tipo: 'aviso', titulo, mensaje, autoCerrar: true, duracion: 5000 });
  }

  /** Notificación informativa — se cierra sola en 4s */
  info(titulo: string, mensaje: string = ''): void {
    this.agregar({ tipo: 'info', titulo, mensaje, autoCerrar: true, duracion: 4000 });
  }

  /** Cierra una notificación por su ID */
  cerrar(id: number): void {
    this.notificaciones.update(n => n.filter(x => x.id !== id));
  }

  /** Cierra todas las notificaciones */
  cerrarTodas(): void {
    this.notificaciones.set([]);
  }

  // ===== HELPERS PARA EXTRAER ERRORES HTTP =====

  /**
   * Extrae un mensaje legible de un error HTTP.
   * Funciona con los errores estandarizados del Gateway y con ValidationProblemDetails de ASP.NET.
   */
  extraerErrorHttp(err: any): { titulo: string; mensaje: string; detalles?: string } {
    const status = err?.status || err?.error?.status || 0;
    const body = err?.error;

    // ValidationProblemDetails de ASP.NET (campo "errors" con diccionario de arrays)
    if (body?.errors && typeof body.errors === 'object') {
      const detallesArr: string[] = [];
      for (const [campo, msgs] of Object.entries(body.errors)) {
        const mensajes = Array.isArray(msgs) ? msgs : [msgs];
        detallesArr.push(...mensajes.map((m: string) => `• ${m}`));
      }
      return {
        titulo: body.title || body.error || this.getTituloEstado(status),
        mensaje: body.detail || 'Revisa los campos del formulario.',
        detalles: detallesArr.join('\n')
      };
    }

    // Error estandarizado del Gateway { error, status, message }
    if (body?.error || body?.message) {
      return {
        titulo: body.error || this.getTituloEstado(status),
        mensaje: body.message || body.detail || '',
        detalles: body.detalles || undefined
      };
    }

    // Error plano (string)
    if (typeof body === 'string' && body.length > 0) {
      return {
        titulo: this.getTituloEstado(status),
        mensaje: body
      };
    }

    // Fallback
    return {
      titulo: this.getTituloEstado(status),
      mensaje: 'Se produjo un error inesperado. Inténtalo de nuevo.'
    };
  }

  /** Muestra directamente un error HTTP como notificación */
  errorHttp(err: any, tituloFallback?: string): void {
    const { titulo, mensaje, detalles } = this.extraerErrorHttp(err);
    this.error(tituloFallback || titulo, mensaje, detalles);
  }

  // ===== INTERNOS =====

  private agregar(opts: Omit<Notificacion, 'id'>): void {
    const id = ++this._id;
    const notificacion: Notificacion = { ...opts, id };

    this.notificaciones.update(n => [...n, notificacion]);

    if (opts.autoCerrar && opts.duracion > 0) {
      setTimeout(() => this.cerrar(id), opts.duracion);
    }
  }

  private getTituloEstado(status: number): string {
    const titulos: Record<number, string> = {
      400: 'Solicitud inválida',
      401: 'No autorizado',
      403: 'Acceso denegado',
      404: 'No encontrado',
      409: 'Conflicto',
      422: 'Datos no procesables',
      429: 'Demasiadas solicitudes',
      500: 'Error del servidor'
    };
    return titulos[status] || `Error ${status || 'desconocido'}`;
  }
}
