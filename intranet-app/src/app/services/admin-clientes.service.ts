import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EstadoEnvio, EstadoInterno } from './admin-envios.service';

const BASE = '/api/nexopostal/admin-clientes';

export interface ClienteListItemDto {
  id: string;
  nombreCompleto: string;
  email: string;
  phoneNumber?: string | null;
  rol: string;
  fechaRegistro: string;
  bloqueado: boolean;
  eliminado: boolean;
}

export interface DireccionAgendaDto {
  id: number;
  alias: string;
  nombreDestinatario: string;
  direccion: string;
  codigoPostal: string;
  ciudad: string;
  provincia: string;
  telefono?: string | null;
}

export interface PerfilClienteDto {
  id: number;
  identityUserId: string;
  dni?: string | null;
  telefono?: string | null;
  direccionPredeterminada?: string | null;
  fechaCreacion: string;
  agenda: DireccionAgendaDto[];
}

export interface EnvioResumenClienteDto {
  numeroSeguimiento: string;
  numeroExpedicion: string;
  fechaCreacion: string;
  estadoActual: EstadoEnvio;
  estadoInternoActual: EstadoInterno;
  pagado: boolean;
  origen: string;
  destino: string;
  codigoPostalDestino: string;
  nombreDestinatario: string;
  costeCalculado: number;
  tipoTarifa: string;
}

export interface EstadisticasClienteDto {
  totalEnvios: number;
  pagados: number;
  entregados: number;
  incidencias: number;
  gastoTotal: number;
}

export interface PerfilCompletoClienteDto {
  identityUserId: string;
  perfil: PerfilClienteDto | null;
  estadisticas: EstadisticasClienteDto;
  envios: EnvioResumenClienteDto[];
}

@Injectable({ providedIn: 'root' })
export class AdminClientesService {
  private readonly http = inject(HttpClient);

  listar(filtros?: { q?: string | null; bloqueado?: boolean | null }): Observable<ClienteListItemDto[]> {
    const params: Record<string, string> = {};
    if (filtros?.q) params['q'] = filtros.q;
    if (filtros?.bloqueado != null) params['bloqueado'] = String(filtros.bloqueado);
    return this.http.get<ClienteListItemDto[]>(BASE, { params });
  }

  perfilCompleto(id: string): Observable<PerfilCompletoClienteDto> {
    return this.http.get<PerfilCompletoClienteDto>(`${BASE}/${id}/perfil-completo`);
  }

  bloquear(id: string): Observable<void> {
    return this.http.put<void>(`${BASE}/${id}/bloquear`, {});
  }

  desbloquear(id: string): Observable<void> {
    return this.http.put<void>(`${BASE}/${id}/desbloquear`, {});
  }

  resetPassword(id: string, nuevaPassword: string): Observable<void> {
    return this.http.post<void>(`${BASE}/${id}/reset-password`, { nuevaPassword });
  }
}
