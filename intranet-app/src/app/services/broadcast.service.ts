import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type BroadcastTipo = 'info' | 'success' | 'warning' | 'error';
export type BroadcastAlcance = 'all' | 'admin' | 'cta' | 'cta-rol';
export type BroadcastRol = 'cta' | 'supervisor' | 'operarios';

export interface BroadcastRequest {
  titulo: string;
  mensaje: string;
  tipo: BroadcastTipo;
  alcance: BroadcastAlcance;
  ctaId?: number | null;
  rol?: BroadcastRol | null;
}

export interface BroadcastResponse {
  ok: boolean;
  fechaUtc: string;
}

@Injectable({ providedIn: 'root' })
export class BroadcastService {
  private readonly http = inject(HttpClient);

  enviar(req: BroadcastRequest): Observable<BroadcastResponse> {
    return this.http.post<BroadcastResponse>('/api/nexopostal/notificaciones/broadcast', req);
  }
}
