import { Injectable, signal, effect, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type ThemeMode = 'light' | 'dark';

const STORAGE_KEY = 'nexopostal:theme';

/**
 * Gestiona el tema claro/oscuro de la aplicación.
 * - Persiste la preferencia en localStorage.
 * - Respeta prefers-color-scheme la primera vez.
 * - Aplica/quita la clase `.dark` en <html>.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  readonly theme = signal<ThemeMode>(this.readInitial());

  constructor() {
    if (this.isBrowser) {
      effect(() => {
        const mode = this.theme();
        const root = document.documentElement;
        root.classList.toggle('dark', mode === 'dark');
        try { localStorage.setItem(STORAGE_KEY, mode); } catch { /* ignore */ }
      });
    }
  }

  toggle(): void {
    this.theme.set(this.theme() === 'dark' ? 'light' : 'dark');
  }

  set(mode: ThemeMode): void {
    this.theme.set(mode);
  }

  isDark(): boolean {
    return this.theme() === 'dark';
  }

  private readInitial(): ThemeMode {
    if (!this.isBrowser) return 'light';
    try {
      const stored = localStorage.getItem(STORAGE_KEY) as ThemeMode | null;
      if (stored === 'light' || stored === 'dark') return stored;
    } catch { /* ignore */ }
    const prefersDark = typeof window !== 'undefined'
      && window.matchMedia?.('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }
}
