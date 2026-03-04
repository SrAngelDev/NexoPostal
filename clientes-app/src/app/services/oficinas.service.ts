import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, of, tap } from 'rxjs';

export interface Oficina {
  id: number;
  nombre: string;
  direccion: string;
  codigoPostal: string;
  ciudad: string;
  telefono: string;
  horario: string;
  servicios: string[];
  distancia?: number;
  coordenadas: { lat: number; lng: number };
}

export interface SugerenciaLocal {
  texto: string;
  ciudad: string;
  codigoPostal: string;
  lat: number;
  lng: number;
}

/** Forma del DTO que devuelve el backend */
interface OficinaBackend {
  id: number;
  nombre: string;
  direccion: string;
  codigoPostal: string;
  ciudad: string;
  provincia: string;
  telefono: string | null;
  email: string | null;
  horario: string;
  servicios: string;
  activa: boolean;
  distanciaKm: number | null;
  latitud: number | null;
  longitud: number | null;
}

@Injectable({
  providedIn: 'root'
})
export class OficinasService {
  private readonly API_URL = '/api/nexopostal/oficinas';
  private oficinasCache: Oficina[] | null = null;

  constructor(private http: HttpClient) {}

  /**
   * Cargar todas las oficinas desde el backend
   */
  cargarOficinas(): Observable<Oficina[]> {
    if (this.oficinasCache) {
      return of(this.oficinasCache);
    }

    return this.http.get<OficinaBackend[]>(`${this.API_URL}/listar`).pipe(
      map(data => {
        const oficinas = data.map(o => this.transformarOficina(o));
        this.oficinasCache = oficinas;
        return oficinas;
      }),
      catchError(error => {
        console.error('Error cargando oficinas:', error);
        return of([]);
      })
    );
  }

  /**
   * Buscar oficinas por código postal (backend)
   */
  buscarPorCodigoPostal(codigoPostal: string): Observable<Oficina[]> {
    const cpLimpio = codigoPostal.trim();

    return this.http.get<OficinaBackend[]>(`${this.API_URL}/buscar`, {
      params: { codigoPostal: cpLimpio }
    }).pipe(
      map(data => data.map(o => this.transformarOficina(o))),
      catchError(error => {
        console.error('Error buscando por CP:', error);
        return of([]);
      })
    );
  }

  /**
   * Buscar oficinas por dirección/ciudad (backend)
   */
  buscarPorDireccion(query: string): Observable<Oficina[]> {
    const q = query.trim();

    return this.http.get<OficinaBackend[]>(`${this.API_URL}/buscar`, {
      params: { query: q }
    }).pipe(
      map(data => data.map(o => this.transformarOficina(o))),
      catchError(error => {
        console.error('Error buscando por dirección:', error);
        return of([]);
      })
    );
  }

  /**
   * Calcular distancia entre dos coordenadas (fórmula Haversine)
   */
  calcularDistancia(
    lat1: number,
    lon1: number,
    lat2: number,
    lon2: number
  ): number {
    const R = 6371; // Radio de la Tierra en km
    const dLat = this.deg2rad(lat2 - lat1);
    const dLon = this.deg2rad(lon2 - lon1);
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos(this.deg2rad(lat1)) *
        Math.cos(this.deg2rad(lat2)) *
        Math.sin(dLon / 2) *
        Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
  }

  /**
   * Obtener sugerencias de códigos postales
   */
  obtenerSugerenciasCodigoPostal(query: string): Observable<string[]> {
    const queryLimpio = query.trim();
    return this.cargarOficinas().pipe(
      map(oficinas => {
        const codigos = new Set<string>();
        oficinas.forEach(o => {
          if (o.codigoPostal.startsWith(queryLimpio)) {
            codigos.add(o.codigoPostal);
          }
        });
        return Array.from(codigos).sort().slice(0, 8);
      })
    );
  }

  /**
   * Obtener sugerencias de direcciones/ciudades desde el JSON cacheado.
   * Devuelve texto descriptivo + coordenadas para poder centrar el mapa.
   */
  obtenerSugerenciasDireccion(query: string): Observable<SugerenciaLocal[]> {
    const q = query.toLowerCase().trim();
    return this.cargarOficinas().pipe(
      map(oficinas => {
        const vistas = new Set<string>();
        const resultado: SugerenciaLocal[] = [];

        for (const o of oficinas) {
          if (resultado.length >= 8) break;

          const coincideCiudad = o.ciudad.toLowerCase().includes(q);
          const coincideDireccion = o.direccion.toLowerCase().includes(q);
          const coincideNombre = o.nombre.toLowerCase().includes(q);
          const coincideCP = o.codigoPostal.startsWith(q);

          if (coincideCiudad || coincideDireccion || coincideNombre || coincideCP) {
            // Para ciudades agrupamos por ciudad para no repetir
            const clave = coincideCiudad && !coincideDireccion
              ? o.ciudad
              : `${o.direccion}, ${o.ciudad}`;

            if (!vistas.has(clave)) {
              vistas.add(clave);
              resultado.push({
                texto: `${o.direccion}, ${o.codigoPostal} ${o.ciudad}`,
                ciudad: o.ciudad,
                codigoPostal: o.codigoPostal,
                lat: o.coordenadas.lat,
                lng: o.coordenadas.lng
              });
            }
          }
        }
        return resultado;
      })
    );
  }

  private deg2rad(deg: number): number {
    return deg * (Math.PI / 180);
  }

  /**
   * Transformar oficina del DTO del backend al formato de la aplicación
   */
  private transformarOficina(o: OficinaBackend): Oficina {
    const servicios = o.servicios
      ? o.servicios.split(',').map(s => s.trim()).filter(s => s)
      : ['Recogida', 'Entrega'];

    return {
      id: o.id,
      nombre: o.nombre,
      direccion: o.direccion,
      codigoPostal: o.codigoPostal,
      ciudad: o.ciudad,
      telefono: o.telefono || '912 197 197',
      horario: o.horario || 'L-V: 9:00-14:00, 17:00-20:00',
      servicios: servicios.length > 0 ? servicios : ['Recogida', 'Entrega', 'Atención al cliente'],
      coordenadas: {
        lat: o.latitud || 0,
        lng: o.longitud || 0
      }
    };
  }
}

