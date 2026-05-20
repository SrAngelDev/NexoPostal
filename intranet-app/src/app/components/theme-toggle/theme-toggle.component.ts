import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="theme-toggle"
      (click)="theme.toggle()"
      [attr.aria-label]="theme.isDark() ? 'Activar modo claro' : 'Activar modo oscuro'"
      [title]="theme.isDark() ? 'Modo claro' : 'Modo oscuro'"
    >
      <span class="material-symbols-outlined">
        {{ theme.isDark() ? 'light_mode' : 'dark_mode' }}
      </span>
    </button>
  `,
  styles: [`
    .theme-toggle {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border-radius: 999px;
      background: transparent;
      color: currentColor;
      border: 1px solid color-mix(in srgb, currentColor 22%, transparent);
      cursor: pointer;
      transition: background var(--dur) var(--ease), border-color var(--dur) var(--ease), transform var(--dur-fast) var(--ease);
    }
    .theme-toggle:hover {
      background: color-mix(in srgb, currentColor 12%, transparent);
      border-color: color-mix(in srgb, currentColor 45%, transparent);
    }
    .theme-toggle:active { transform: scale(0.94); }
    .theme-toggle:focus-visible { outline: none; box-shadow: var(--ring); }
    .theme-toggle .material-symbols-outlined { font-size: 1.25rem; }
  `]
})
export class ThemeToggleComponent {
  readonly theme = inject(ThemeService);
}
