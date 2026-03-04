import { Injectable } from '@angular/core';

interface CacheEntry<T> {
  data: T;
  timestamp: number;
}

/**
 * Servicio de caché en memoria para reducir llamadas redundantes a la API.
 * TTL configurable, por defecto 5 minutos.
 */
@Injectable({ providedIn: 'root' })
export class CacheService {
  private cache = new Map<string, CacheEntry<any>>();
  private readonly DEFAULT_TTL_MS = 5 * 60 * 1000; // 5 minutos

  /**
   * Obtiene un valor del caché si existe y no ha expirado
   */
  get<T>(key: string, ttlMs?: number): T | null {
    const entry = this.cache.get(key);
    if (!entry) return null;

    const ttl = ttlMs ?? this.DEFAULT_TTL_MS;
    if (Date.now() - entry.timestamp > ttl) {
      this.cache.delete(key);
      return null;
    }

    return entry.data as T;
  }

  /**
   * Almacena un valor en caché
   */
  set<T>(key: string, data: T): void {
    this.cache.set(key, { data, timestamp: Date.now() });
  }

  /**
   * Invalida una entrada del caché
   */
  invalidar(key: string): void {
    this.cache.delete(key);
  }

  /**
   * Invalida todas las entradas que comiencen con un prefijo
   */
  invalidarPorPrefijo(prefijo: string): void {
    const keysToDelete: string[] = [];
    this.cache.forEach((_, key) => {
      if (key.startsWith(prefijo)) keysToDelete.push(key);
    });
    keysToDelete.forEach(key => this.cache.delete(key));
  }

  /**
   * Limpia todo el caché
   */
  limpiar(): void {
    this.cache.clear();
  }

  /**
   * Obtiene el número de entradas en caché
   */
  get tamaño(): number {
    return this.cache.size;
  }
}
