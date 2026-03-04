import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UsuarioInfoDto {
  id: string;
  email: string;
  nombreCompleto: string;
  phoneNumber?: string;
  fechaRegistro: Date;
  roles: string[];
}

export interface ActualizarUsuarioDto {
  nombreCompleto: string;
  email: string;
  phoneNumber?: string;
}

export interface CambiarPasswordDto {
  passwordActual: string;
  nuevaPassword: string;
}

@Injectable({ providedIn: 'root' })
export class UsuarioService {
  private readonly API_URL = '/api/nexopostal/auth';

  constructor(private http: HttpClient) {}

  obtenerUsuario(): Observable<UsuarioInfoDto> {
    return this.http.get<UsuarioInfoDto>(`${this.API_URL}/me`);
  }

  actualizarUsuario(datos: ActualizarUsuarioDto): Observable<UsuarioInfoDto> {
    return this.http.post<UsuarioInfoDto>(`${this.API_URL}/actualizar-perfil`, datos);
  }

  cambiarPassword(datos: CambiarPasswordDto): Observable<{ mensaje: string }> {
    return this.http.post<{ mensaje: string }>(`${this.API_URL}/cambiar-password`, datos);
  }
}
