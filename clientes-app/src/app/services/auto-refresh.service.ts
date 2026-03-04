import { Injectable, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';

interface PollingEntry {
  interval: ReturnType<typeof setInterval>;
  callback: () => void;
}

/**
 * Servicio de actualización automática de datos.
 * Permite registrar callbacks que se ejecutan periódicamente (polling).
 */
@Injectable({ providedIn: 'root' })
export class AutoRefreshService implements OnDestroy {
  private pollingEntries = new Map<string, PollingEntry>();
  private destroy$ = new Subject<void>();

  /**
   * Inicia polling para una clave dada
   * @param clave Identificador único del polling
   * @param callback Función a ejecutar periódicamente
   * @param intervaloMs Intervalo en milisegundos (por defecto 30s)
   */
  iniciarPolling(clave: string, callback: () => void, intervaloMs: number = 30000): void {
    this.detenerPolling(clave);

    // Ejecutar inmediatamente
    callback();

    const interval = setInterval(callback, intervaloMs);
    this.pollingEntries.set(clave, { interval, callback });
  }

  /**
   * Detiene el polling para una clave
   */
  detenerPolling(clave: string): void {
    const entry = this.pollingEntries.get(clave);
    if (entry) {
      clearInterval(entry.interval);
      this.pollingEntries.delete(clave);
    }
  }

  /**
   * Detiene todos los pollings activos
   */
  detenerTodo(): void {
    this.pollingEntries.forEach(entry => clearInterval(entry.interval));
    this.pollingEntries.clear();
  }

  /**
   * Verifica si hay polling activo para una clave
   */
  estaActivo(clave: string): boolean {
    return this.pollingEntries.has(clave);
  }

  /**
   * Fuerza una ejecución inmediata del callback para una clave
   */
  refrescarAhora(clave: string): void {
    const entry = this.pollingEntries.get(clave);
    if (entry) entry.callback();
  }

  ngOnDestroy(): void {
    this.detenerTodo();
    this.destroy$.next();
    this.destroy$.complete();
  }
}
