import { Injectable, signal, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type ThemeMode = 'light';

const STORAGE_KEY = 'nexopostal:theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  readonly theme = signal<ThemeMode>('light');

  constructor() {
    if (this.isBrowser) {
      document.documentElement.classList.remove('dark');
      try { localStorage.removeItem(STORAGE_KEY); } catch { /* ignore */ }
    }
  }

  toggle(): void { /* no-op */ }
  set(_: ThemeMode): void { /* no-op */ }
  isDark(): boolean { return false; }
}
