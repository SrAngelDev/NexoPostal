import { Injectable, signal } from '@angular/core';

export interface ConfirmacionConfig {
  titulo?: string;
  mensaje: string;
  textoConfirmar?: string;
  textoCancelar?: string;
  tipo?: 'peligro' | 'normal';
}

@Injectable({ providedIn: 'root' })
export class ConfirmacionService {
  readonly visible = signal(false);
  readonly config = signal<ConfirmacionConfig>({ mensaje: '' });

  private _resolver: ((valor: boolean) => void) | null = null;

  /**
   * Muestra un modal de confirmación y devuelve una Promise<boolean>.
   * Uso: `if (await this.confirmacion.confirmar({ mensaje: '¿Seguro?' })) { ... }`
   */
  confirmar(cfg: ConfirmacionConfig): Promise<boolean> {
    this.config.set({
      titulo: cfg.titulo || 'Confirmación',
      mensaje: cfg.mensaje,
      textoConfirmar: cfg.textoConfirmar || 'Confirmar',
      textoCancelar: cfg.textoCancelar || 'Cancelar',
      tipo: cfg.tipo || 'normal'
    });
    this.visible.set(true);

    return new Promise<boolean>(resolve => {
      this._resolver = resolve;
    });
  }

  /** @internal — llamado por el componente modal */
  resolver(valor: boolean): void {
    this.visible.set(false);
    this._resolver?.(valor);
    this._resolver = null;
  }
}
