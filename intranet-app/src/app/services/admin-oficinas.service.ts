import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface OficinaPostalAdminDto {
  id: number;
  nombre: string;
  direccion: string;
  codigoPostal: string;
  ciudad: string;
  provincia?: string | null;
  telefono?: string | null;
  horario?: string | null;
  servicios?: string | null;
  latitud?: number | null;
  longitud?: number | null;
  activo: boolean;
  fechaAlta: string;
  fechaModificacion: string;
  operariosActivos: number;
}

export interface CrearOficinaPostalDto {
  nombre: string;
  direccion: string;
  codigoPostal: string;
  ciudad: string;
  provincia?: string | null;
  telefono?: string | null;
  horario?: string | null;
  servicios?: string | null;
  latitud?: number | null;
  longitud?: number | null;
}

@Injectable({ providedIn: 'root' })
export class AdminOficinasService {
  private readonly http = inject(HttpClient);
  private readonly BASE = '/api/nexopostal/admin-oficinas';

  listar(incluirInactivas = false): Observable<OficinaPostalAdminDto[]> {
    return this.http.get<OficinaPostalAdminDto[]>(`${this.BASE}?incluirInactivas=${incluirInactivas}`);
  }

  obtener(id: number): Observable<OficinaPostalAdminDto> {
    return this.http.get<OficinaPostalAdminDto>(`${this.BASE}/${id}`);
  }

  crear(dto: CrearOficinaPostalDto): Observable<OficinaPostalAdminDto> {
    return this.http.post<OficinaPostalAdminDto>(this.BASE, dto);
  }

  actualizar(id: number, dto: CrearOficinaPostalDto): Observable<OficinaPostalAdminDto> {
    return this.http.put<OficinaPostalAdminDto>(`${this.BASE}/${id}`, dto);
  }

  desactivar(id: number): Observable<{ mensaje: string; id: number }> {
    return this.http.delete<{ mensaje: string; id: number }>(`${this.BASE}/${id}`);
  }

  reactivar(id: number): Observable<{ mensaje: string; id: number }> {
    return this.http.post<{ mensaje: string; id: number }>(`${this.BASE}/${id}/reactivar`, {});
  }
}
