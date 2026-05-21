import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TarifaBandaDto {
  id: number;
  serie: string;
  ordenBanda: number;
  pesoHastaKg: number;
  precioBase: number;
  fechaModificacion: string;
  modificadoPorUserId?: string | null;
}

export interface EditarTarifaBandaBulkItemDto {
  id: number;
  precioBase: number;
}

@Injectable({ providedIn: 'root' })
export class AdminTarifasService {
  private readonly http = inject(HttpClient);
  private readonly BASE = '/api/nexopostal/admin-tarifas';

  listar(): Observable<TarifaBandaDto[]> {
    return this.http.get<TarifaBandaDto[]>(this.BASE);
  }

  editar(id: number, precioBase: number): Observable<TarifaBandaDto> {
    return this.http.put<TarifaBandaDto>(`${this.BASE}/${id}`, { precioBase });
  }

  editarBulk(items: EditarTarifaBandaBulkItemDto[]): Observable<TarifaBandaDto[]> {
    return this.http.put<TarifaBandaDto[]>(`${this.BASE}/bulk`, items);
  }

  resetDefaults(): Observable<TarifaBandaDto[]> {
    return this.http.post<TarifaBandaDto[]>(`${this.BASE}/reset-defaults`, {});
  }
}
