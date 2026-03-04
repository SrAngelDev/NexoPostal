import { Injectable } from '@angular/core';

/**
 * Servicio de guardado automático de formularios.
 * Persiste el estado en localStorage para evitar pérdidas de datos.
 */
@Injectable({ providedIn: 'root' })
export class AutoSaveService {
  private readonly PREFIX = 'autosave_';
  private readonly TTL_MS = 24 * 60 * 60 * 1000; // 24 horas

  /**
   * Guarda el estado de un formulario en localStorage
   */
  guardarEstado(clave: string, datos: any): void {
    const entry = {
      data: datos,
      timestamp: new Date().toISOString()
    };
    localStorage.setItem(`${this.PREFIX}${clave}`, JSON.stringify(entry));
  }

  /**
   * Restaura el estado guardado si no ha expirado (24h TTL)
   */
  restaurarEstado<T>(clave: string): T | null {
    const raw = localStorage.getItem(`${this.PREFIX}${clave}`);
    if (!raw) return null;

    try {
      const entry = JSON.parse(raw);
      const timestamp = new Date(entry.timestamp).getTime();
      if (Date.now() - timestamp > this.TTL_MS) {
        this.limpiar(clave);
        return null;
      }
      return entry.data as T;
    } catch {
      this.limpiar(clave);
      return null;
    }
  }

  /**
   * Elimina el estado guardado para una clave
   */
  limpiar(clave: string): void {
    localStorage.removeItem(`${this.PREFIX}${clave}`);
  }

  /**
   * Comprueba si hay un estado guardado para la clave
   */
  tieneEstado(clave: string): boolean {
    return localStorage.getItem(`${this.PREFIX}${clave}`) !== null;
  }

  /**
   * Limpia todos los estados expirados
   */
  limpiarExpirados(): void {
    const keysToRemove: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key?.startsWith(this.PREFIX)) {
        const raw = localStorage.getItem(key);
        if (raw) {
          try {
            const entry = JSON.parse(raw);
            const timestamp = new Date(entry.timestamp).getTime();
            if (Date.now() - timestamp > this.TTL_MS) {
              keysToRemove.push(key);
            }
          } catch {
            keysToRemove.push(key);
          }
        }
      }
    }
    keysToRemove.forEach(key => localStorage.removeItem(key));
  }
}
