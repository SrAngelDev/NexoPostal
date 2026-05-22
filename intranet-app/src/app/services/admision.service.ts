import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AltaEnvioOficinaRequest {
  peso: number;
  dimensiones: string;

  nombreRemitente: string;
  apellidosRemitente?: string;
  origen: string;
  codigoPostalOrigen: string;
  telefonoRemitente: string;
  emailRemitente?: string;
  dniRemitente?: string;

  nombreDestinatario: string;
  apellidosDestinatario?: string;
  destino: string;
  codigoPostalDestino: string;
  telefonoDestinatario: string;
  emailDestinatario?: string;

  tipoEntrega: 'Domicilio' | 'Oficina';
  oficinaDestinoId?: number | null;

  metodoCobro: string;
  observaciones?: string;
}

export interface AltaEnvioOficinaResponse {
  numeroExpedicion: string;
  numeroSeguimiento: string;
  costeCalculado: number;
  tipoEntrega: string;
  oficinaOrigenId?: number | null;
  oficinaDestinoId?: number | null;
  ctaDestinoCodigo?: string;
  mensaje: string;
}

export interface OficinaJsonItem {
  id: number;
  nombre: string;
  direccion: string;
  codigoPostal: string;
  ciudad: string;
  provincia?: string;
}

@Injectable({ providedIn: 'root' })
export class AdmisionService {
  constructor(private http: HttpClient) {}

  altaPresencialOficina(dto: AltaEnvioOficinaRequest): Observable<AltaEnvioOficinaResponse> {
    return this.http.post<AltaEnvioOficinaResponse>('/api/admision/oficina/alta', dto);
  }

  buscarOficinas(codigoPostal: string): Observable<OficinaJsonItem[]> {
    return this.http.get<OficinaJsonItem[]>(`/api/oficinasPostales/buscar?codigoPostal=${encodeURIComponent(codigoPostal)}`);
  }
}
