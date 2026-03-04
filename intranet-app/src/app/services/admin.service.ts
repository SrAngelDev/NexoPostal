import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DashboardCtaDto {
  ctaId: number;
  ctaCodigo: string;
  ctaNombre: string;
  area: string;
  totalOperarios: number;
  operariosActivos: number;
  tareasPendientes: number;
  tareasEnProgreso: number;
  tareasCompletadasHoy: number;
  tareasUrgentes: number;
  movimientosProgramados: number;
  movimientosEnTransito: number;
  movimientosRecibidosHoy: number;
  incidenciasAbiertas: number;
  incidenciasEnRevision: number;
}

export interface DashboardAdminDto {
  totalCtas: number;
  ctasActivos: number;
  totalOperarios: number;
  operariosActivos: number;
  tareasPendientesGlobal: number;
  tareasEnProgresoGlobal: number;
  tareasCompletadasHoyGlobal: number;
  tareasUrgentesGlobal: number;
  movimientosProgramadosGlobal: number;
  movimientosEnTransitoGlobal: number;
  movimientosRecibidosHoyGlobal: number;
  incidenciasAbiertasGlobal: number;
  incidenciasEnRevisionGlobal: number;
  detallePorCta: DashboardCtaDto[];
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly API_URL = '/api/nexopostal/ctas';

  constructor(private http: HttpClient) {}

  obtenerDashboardGlobal(): Observable<DashboardAdminDto> {
    return this.http.get<DashboardAdminDto>(`${this.API_URL}/dashboard-global`);
  }

  obtenerCtas(): Observable<any[]> {
    return this.http.get<any[]>(this.API_URL);
  }

  obtenerDashboardCta(ctaId: number): Observable<DashboardCtaDto> {
    return this.http.get<DashboardCtaDto>(`${this.API_URL}/${ctaId}/dashboard`);
  }
}
