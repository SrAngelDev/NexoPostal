import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export enum TipoVehiculo {
  Furgoneta = 0,
  Moto = 1,
  Bicicleta = 2,
  APie = 3,
  Camion = 4
}

export const TIPO_VEHICULO_OPTIONS: { value: TipoVehiculo; label: string }[] = [
  { value: TipoVehiculo.Furgoneta, label: 'Furgoneta' },
  { value: TipoVehiculo.Moto, label: 'Moto' },
  { value: TipoVehiculo.Bicicleta, label: 'Bicicleta' },
  { value: TipoVehiculo.APie, label: 'A pie' },
  { value: TipoVehiculo.Camion, label: 'Camión' }
];

export function tipoVehiculoLabel(t: TipoVehiculo): string {
  return TIPO_VEHICULO_OPTIONS.find(o => o.value === t)?.label ?? String(t);
}

export interface VehiculoDto {
  id: number;
  matricula: string;
  tipo: TipoVehiculo;
  tipoNombre: string;
  marca?: string | null;
  modelo?: string | null;
  color?: string | null;
  anioFabricacion?: number | null;
  repartidorAsignadoId?: number | null;
  repartidorAsignadoNombre?: string | null;
  oficinaJsonId?: number | null;
  notas?: string | null;
  activo: boolean;
  fechaAlta: string;
  fechaModificacion: string;
}

export interface CrearVehiculoDto {
  matricula: string;
  tipo: TipoVehiculo;
  marca?: string | null;
  modelo?: string | null;
  color?: string | null;
  anioFabricacion?: number | null;
  oficinaJsonId?: number | null;
  notas?: string | null;
}

export interface AsignarVehiculoDto {
  repartidorId: number | null;
}

export interface ImportarResultDto {
  importados: number;
  omitidos: number;
  matriculasImportadas: string[];
  mensajes: string[];
}

@Injectable({ providedIn: 'root' })
export class AdminVehiculosService {
  private readonly http = inject(HttpClient);
  private readonly BASE = '/api/nexopostal/admin-vehiculos';

  listar(incluirInactivos = false): Observable<VehiculoDto[]> {
    return this.http.get<VehiculoDto[]>(`${this.BASE}?incluirInactivos=${incluirInactivos}`);
  }

  obtener(id: number): Observable<VehiculoDto> {
    return this.http.get<VehiculoDto>(`${this.BASE}/${id}`);
  }

  crear(dto: CrearVehiculoDto): Observable<VehiculoDto> {
    return this.http.post<VehiculoDto>(this.BASE, dto);
  }

  actualizar(id: number, dto: CrearVehiculoDto): Observable<VehiculoDto> {
    return this.http.put<VehiculoDto>(`${this.BASE}/${id}`, dto);
  }

  desactivar(id: number): Observable<{ mensaje: string; id: number }> {
    return this.http.delete<{ mensaje: string; id: number }>(`${this.BASE}/${id}`);
  }

  reactivar(id: number): Observable<{ mensaje: string; id: number }> {
    return this.http.post<{ mensaje: string; id: number }>(`${this.BASE}/${id}/reactivar`, {});
  }

  asignar(id: number, repartidorId: number | null): Observable<VehiculoDto> {
    const dto: AsignarVehiculoDto = { repartidorId };
    return this.http.post<VehiculoDto>(`${this.BASE}/${id}/asignar`, dto);
  }

  importarDesdeRepartidores(): Observable<ImportarResultDto> {
    return this.http.post<ImportarResultDto>(`${this.BASE}/importar-desde-repartidores`, {});
  }
}
