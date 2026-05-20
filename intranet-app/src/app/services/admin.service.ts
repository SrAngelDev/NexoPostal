import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

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

export interface UsuarioAdminDto {
  id: string;
  nombreCompleto: string;
  email: string;
  codigoEmpleado?: string;
  rol: string;
  fechaRegistro: string;
  bloqueado: boolean;
}

export interface AdminCrearEmpleadoDto {
  nombreCompleto: string;
  email: string;
  codigoEmpleado?: string;
  rol: string;
  password: string;
}

export interface CtaResumenDto {
  id: number;
  codigo: string;
  nombre: string;
  area: string;
}

export interface AdminOperarioCtaAsignacionDto {
  operarioCtaId: number;
  ctaId: number;
  ctaCodigo: string;
  ctaNombre: string;
  area: string;
  rolOperativo: string;
  activo: boolean;
  fechaAsignacion: string;
  tareasPendientes: number;
  tareasEnProgreso: number;
  tareasCompletadasHoy: number;
}

export interface AdminOperarioDetalleDto {
  identityUserId: string;
  nombreCompleto: string;
  codigoEmpleado: string;
  asignacionesCta: AdminOperarioCtaAsignacionDto[];
}

export interface AdminOperarioOficinaDto {
  oficinaJsonId: number;
  oficinaNombre: string;
  codigoPostal: string;
  ciudad: string;
  direccion: string;
  rol: string;
  activo: boolean;
  fechaAsignacion: string;
}

export interface OficinaJsonResumen {
  id: number;
  nombre: string;
  direccion: string;
  codigoPostal: string;
  ciudad: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly API_URL = '/api/nexopostal/ctas';
  private readonly USUARIOS_URL = '/api/nexopostal/admin-usuarios';

  constructor(private http: HttpClient) {}

  obtenerDashboardGlobal(): Observable<DashboardAdminDto> {
    return this.http.get<DashboardAdminDto>(`${this.API_URL}/dashboard-global`);
  }

  obtenerCtas(): Observable<CtaResumenDto[]> {
    return this.http.get<CtaResumenDto[]>(this.API_URL);
  }

  obtenerDashboardCta(ctaId: number): Observable<DashboardCtaDto> {
    return this.http.get<DashboardCtaDto>(`${this.API_URL}/${ctaId}/dashboard`);
  }

  // ─── Gestión de usuarios (Admin) ───

  listarUsuarios(rol?: string, bloqueado?: boolean, q?: string): Observable<UsuarioAdminDto[]> {
    let params = new HttpParams();
    if (rol) params = params.set('rol', rol);
    if (bloqueado !== undefined) params = params.set('bloqueado', bloqueado.toString());
    if (q) params = params.set('q', q);
    return this.http.get<UsuarioAdminDto[]>(this.USUARIOS_URL, { params });
  }

  obtenerDetalleUsuario(id: string): Observable<UsuarioAdminDto> {
    return this.http.get<UsuarioAdminDto>(`${this.USUARIOS_URL}/${id}`);
  }

  crearEmpleado(dto: AdminCrearEmpleadoDto): Observable<UsuarioAdminDto> {
    return this.http.post<UsuarioAdminDto>(this.USUARIOS_URL, dto);
  }

  cambiarRol(id: string, nuevoRol: string): Observable<void> {
    return this.http.put<void>(`${this.USUARIOS_URL}/${id}/rol`, { nuevoRol });
  }

  bloquearUsuario(id: string): Observable<void> {
    return this.http.put<void>(`${this.USUARIOS_URL}/${id}/bloquear`, {});
  }

  desbloquearUsuario(id: string): Observable<void> {
    return this.http.put<void>(`${this.USUARIOS_URL}/${id}/desbloquear`, {});
  }

  resetPasswordUsuario(id: string, nuevaPassword: string): Observable<void> {
    return this.http.post<void>(`${this.USUARIOS_URL}/${id}/reset-password`, { nuevaPassword });
  }

  obtenerDetalleOperativoUsuario(id: string): Observable<AdminOperarioDetalleDto> {
    return this.http.get<AdminOperarioDetalleDto>(`${this.USUARIOS_URL}/${id}/detalle-operativo`);
  }

  moverCtaUsuario(id: string, nuevoCtaId: number, operarioCtaId?: number): Observable<void> {
    const payload: { nuevoCtaId: number; operarioCtaId?: number } = { nuevoCtaId };
    if (operarioCtaId !== undefined) payload.operarioCtaId = operarioCtaId;
    return this.http.put<void>(`${this.USUARIOS_URL}/${id}/cta`, payload);
  }

  asignarPrimeraCtaUsuario(
    id: string,
    nuevoCtaId: number,
    nombreCompleto: string,
    codigoEmpleado: string,
    rol: string
  ): Observable<void> {
    return this.http.put<void>(`${this.USUARIOS_URL}/${id}/cta`, {
      nuevoCtaId,
      nombreCompleto,
      codigoEmpleado,
      rol
    });
  }

  // ─── Asignación de oficina (solo OperarioOficina) ───

  obtenerOficinaUsuario(id: string): Observable<AdminOperarioOficinaDto | null> {
    return this.http
      .get<AdminOperarioOficinaDto>(`${this.USUARIOS_URL}/${id}/oficina`, { observe: 'response' })
      .pipe(map(r => (r.status === 204 ? null : r.body)));
  }

  /** Cambia (o crea por primera vez) la oficina asignada al operario. */
  actualizarOficinaUsuario(
    id: string,
    nuevoOficinaJsonId: number,
    primeraVez?: { nombreCompleto: string; codigoEmpleado: string; rol?: string }
  ): Observable<AdminOperarioOficinaDto> {
    const payload: Record<string, unknown> = { nuevoOficinaJsonId };
    if (primeraVez) {
      payload['nombreCompleto'] = primeraVez.nombreCompleto;
      payload['codigoEmpleado'] = primeraVez.codigoEmpleado;
      if (primeraVez.rol) payload['rol'] = primeraVez.rol;
    }
    return this.http.put<AdminOperarioOficinaDto>(`${this.USUARIOS_URL}/${id}/oficina`, payload);
  }

  /** Lista las oficinas del catálogo cuyo CP encaja con las rutas del CTA. */
  obtenerOficinasPorCta(ctaId: number): Observable<OficinaJsonResumen[]> {
    return this.http.get<OficinaJsonResumen[]>(`/api/nexopostal/oficinaspostales/por-cta/${ctaId}`);
  }
}

