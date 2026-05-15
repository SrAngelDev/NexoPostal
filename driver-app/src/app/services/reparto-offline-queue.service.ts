import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  RegistrarEntregaRequest,
  RepartoService,
  UbicacionRepartidorRequest
} from './reparto.service';

export interface ConfirmacionPendiente {
  entregaId: number;
  request: RegistrarEntregaRequest;
}

export interface UbicacionPendiente {
  request: UbicacionRepartidorRequest;
}

type QueueItem =
  | { tipo: 'confirmacion'; creadoEn: string; data: ConfirmacionPendiente }
  | { tipo: 'ubicacion'; creadoEn: string; data: UbicacionPendiente };

@Injectable({ providedIn: 'root' })
export class RepartoOfflineQueueService {
  private readonly STORAGE_KEY = 'driver_reparto_offline_queue';
  readonly pendientes = signal(0);

  constructor() {
    this.actualizarContador();
  }

  encolarConfirmacion(data: ConfirmacionPendiente): void {
    const queue = this.leerQueue();
    queue.push({ tipo: 'confirmacion', creadoEn: new Date().toISOString(), data });
    this.escribirQueue(queue);
  }

  encolarUbicacion(data: UbicacionPendiente): void {
    const queue = this.leerQueue();
    const mixedQueue: QueueItem[] = queue.filter(q => q.tipo === 'confirmacion');
    mixedQueue.push({ tipo: 'ubicacion', creadoEn: new Date().toISOString(), data });
    this.escribirQueue(mixedQueue);
  }

  async procesarPendientes(repartoService: RepartoService): Promise<{ procesados: number; pendientes: number }> {
    if (!navigator.onLine) {
      return { procesados: 0, pendientes: this.leerQueue().length };
    }

    const queue = this.leerQueue();
    const restantes: QueueItem[] = [];
    let procesados = 0;

    for (const item of queue) {
      try {
        if (item.tipo === 'confirmacion') {
          await firstValueFrom(
            repartoService.confirmarEntrega(item.data.entregaId, item.data.request)
          );
        } else {
          await firstValueFrom(repartoService.registrarUbicacion(item.data.request));
        }
        procesados++;
      } catch {
        restantes.push(item);
      }
    }

    this.escribirQueue(restantes);

    return {
      procesados,
      pendientes: restantes.length
    };
  }

  private leerQueue(): QueueItem[] {
    const raw = localStorage.getItem(this.STORAGE_KEY);
    if (!raw) return [];

    try {
      const parsed = JSON.parse(raw) as QueueItem[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  private escribirQueue(queue: QueueItem[]): void {
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(queue));
    this.actualizarContador();
  }

  private actualizarContador(): void {
    this.pendientes.set(this.leerQueue().length);
  }
}
