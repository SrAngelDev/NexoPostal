import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Interfaces del perfil
export interface PerfilDto {
  identityUserId: string;
  dni?: string;
  telefono?: string;
  direccionPredeterminada?: string;
  fechaCreacion: Date;
}

export interface ActualizarPerfilDto {
  dni?: string;
  telefono?: string;
  direccionPredeterminada?: string;
}

export interface DireccionFavoritaDto {
  id: number;
  alias: string;
  nombreDestinatario: string;
  direccion: string;
  codigoPostal: string;
  ciudad: string;
  provincia: string;
  telefono?: string;
}

export interface CrearDireccionFavoritaDto {
  alias: string;
  nombreDestinatario: string;
  direccion: string;
  codigoPostal: string;
  ciudad: string;
  provincia: string;
  telefono?: string;
}

@Injectable({ providedIn: 'root' })
export class PerfilService {
  private readonly API_URL = '/api/nexopostal/perfil';

  constructor(private http: HttpClient) {}

  obtenerPerfil(): Observable<PerfilDto> {
    return this.http.get<PerfilDto>(`${this.API_URL}/get`);
  }

  actualizarPerfil(datos: ActualizarPerfilDto): Observable<PerfilDto> {
    return this.http.post<PerfilDto>(`${this.API_URL}/guardar`, datos);
  }

  obtenerDirecciones(): Observable<DireccionFavoritaDto[]> {
    return this.http.get<DireccionFavoritaDto[]>(`${this.API_URL}/direcciones`);
  }

  agregarDireccion(direccion: CrearDireccionFavoritaDto): Observable<DireccionFavoritaDto> {
    return this.http.post<DireccionFavoritaDto>(`${this.API_URL}/agregar-direccion`, direccion);
  }

  actualizarDireccion(id: number, direccion: CrearDireccionFavoritaDto): Observable<DireccionFavoritaDto> {
    return this.http.put<DireccionFavoritaDto>(`${this.API_URL}/editar-direccion/${id}`, direccion);
  }

  eliminarDireccion(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/eliminar-direccion/${id}`);
  }
}
