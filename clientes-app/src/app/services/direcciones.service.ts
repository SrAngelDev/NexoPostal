import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, map, catchError, debounceTime } from 'rxjs';

export interface DireccionSugerencia {
  displayName: string;
  lat: number;
  lon: number;
  address: {
    road?: string;
    house_number?: string;
    city?: string;
    postcode?: string;
    country?: string;
  };
}

@Injectable({
  providedIn: 'root'
})
export class DireccionesService {
  private readonly NOMINATIM_URL = '/api/nominatim';

  constructor(private http: HttpClient) {}

  /**
   * Buscar direcciones con autocompletado
   * @param query Texto de búsqueda
   * @param countryCode Código de país (es para España)
   */
  buscarDirecciones(query: string, countryCode: string = 'es'): Observable<DireccionSugerencia[]> {
    if (!query || query.length < 3) {
      return of([]);
    }

    const params = {
      q: query,
      format: 'json',
      addressdetails: '1',
      limit: '5',
      countrycodes: countryCode
    };

    return this.http.get<any[]>(`${this.NOMINATIM_URL}/search`, { params }).pipe(
      map(results => results.map(result => ({
        displayName: result.display_name,
        lat: parseFloat(result.lat),
        lon: parseFloat(result.lon),
        address: result.address || {}
      }))),
      catchError(error => {
        console.error('Error buscando direcciones:', error);
        return of([]);
      })
    );
  }

  /**
   * Validar código postal español
   * @param codigoPostal Código postal a validar
   */
  validarCodigoPostal(codigoPostal: string): boolean {
    const regex = /^[0-5]\d{4}$/;
    return regex.test(codigoPostal);
  }

  /**
   * Obtener información de una dirección específica por código postal
   * @param codigoPostal Código postal
   */
  buscarPorCodigoPostal(codigoPostal: string): Observable<DireccionSugerencia[]> {
    if (!this.validarCodigoPostal(codigoPostal)) {
      return of([]);
    }

    const params = {
      postalcode: codigoPostal,
      country: 'Spain',
      format: 'json',
      addressdetails: '1',
      limit: '5'
    };

    return this.http.get<any[]>(`${this.NOMINATIM_URL}/search`, { params }).pipe(
      map(results => results.map(result => ({
        displayName: result.display_name,
        lat: parseFloat(result.lat),
        lon: parseFloat(result.lon),
        address: result.address || {}
      }))),
      catchError(error => {
        console.error('Error buscando por código postal:', error);
        return of([]);
      })
    );
  }

  /**
   * Geocodificar inversamente (obtener dirección desde coordenadas)
   * @param lat Latitud
   * @param lon Longitud
   */
  obtenerDireccionDesdeCoords(lat: number, lon: number): Observable<DireccionSugerencia | null> {
    const params = {
      lat: lat.toString(),
      lon: lon.toString(),
      format: 'json',
      addressdetails: '1'
    };

    return this.http.get<any>(`${this.NOMINATIM_URL}/reverse`, { params }).pipe(
      map(result => ({
        displayName: result.display_name,
        lat: parseFloat(result.lat),
        lon: parseFloat(result.lon),
        address: result.address || {}
      })),
      catchError(error => {
        console.error('Error en geocodificación inversa:', error);
        return of(null);
      })
    );
  }

  /**
   * Validar que una dirección sea válida en España
   * @param direccion Dirección completa
   */
  validarDireccion(direccion: string): Observable<boolean> {
    return this.buscarDirecciones(direccion, 'es').pipe(
      map(resultados => resultados.length > 0)
    );
  }

  /**
   * Extraer código postal de una dirección
   * @param direccion Dirección sugerida
   */
  extraerCodigoPostal(direccion: DireccionSugerencia): string {
    return direccion.address.postcode || '';
  }

  /**
   * Formatear dirección para mostrar
   * @param direccion Dirección sugerida
   */
  formatearDireccion(direccion: DireccionSugerencia): string {
    const { road, house_number, city, postcode } = direccion.address;
    const partes = [];
    
    if (road) {
      partes.push(house_number ? `${road}, ${house_number}` : road);
    }
    if (postcode && city) {
      partes.push(`${postcode} ${city}`);
    } else if (city) {
      partes.push(city);
    }
    
    return partes.join(' - ') || direccion.displayName;
  }
}
