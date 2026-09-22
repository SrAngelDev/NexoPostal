import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-footer-publico',
  standalone: true,
  template: `
    <footer class="footer">
      <div class="footer-inner">
        <div class="footer-mast">
          <button class="footer-logo" type="button" (click)="go('/')" aria-label="Ir al inicio">
            <span class="material-symbols-outlined" aria-hidden="true">local_shipping</span>
            <span>Nexo<span class="brand-accent">Postal</span></span>
          </button>
          <p>Envíos nacionales, seguimiento y atención desde una única red.</p>
        </div>

        <nav class="footer-links" aria-label="Enlaces del pie">
          <button type="button" (click)="go('/calculadora-tarifas')">Tarifas</button>
          <button type="button" (click)="go('/buscador-oficinas')">Oficinas</button>
          <button type="button" (click)="go('/ayuda')">Ayuda</button>
          <button type="button" (click)="go('/politica-privacidad')">Privacidad</button>
          <button type="button" (click)="go('/terminos-uso')">Términos</button>
        </nav>

        <p class="footer-legal">© 2026 NexoPostal S.A. Todos los derechos reservados.</p>
      </div>
    </footer>
  `,
  styles: [`
    :host { display: block; }
    .footer { padding: var(--space-2xl) var(--page-gutter) var(--space-lg); background: var(--brand-primary-strong); color: var(--text-on-brand); }
    .footer-inner { width: min(100%, var(--page-max)); margin-inline: auto; }
    .footer-mast { display: grid; gap: var(--space-sm); padding-bottom: var(--space-xl); }
    .footer-logo { width: fit-content; display: inline-flex; align-items: center; gap: var(--space-xs); padding: 0; border: 0; background: transparent; color: inherit; font: 800 var(--text-xl)/1 var(--font-sans); letter-spacing: -0.03em; cursor: pointer; white-space: nowrap; }
    .footer-logo .material-symbols-outlined, .brand-accent { color: var(--brand-accent); }
    .footer-mast p { max-width: 42ch; color: var(--color-on-brand-muted); line-height: 1.6; }
    .footer-links { display: flex; flex-wrap: wrap; gap: var(--space-xs) var(--space-lg); padding-block: var(--space-lg); border-block: var(--rule-thin) solid var(--color-on-brand-rule); }
    .footer-links button { min-height: 44px; padding: 0; border: 0; background: transparent; color: var(--color-on-brand-muted); font: 700 var(--text-sm)/1 var(--font-sans); cursor: pointer; white-space: nowrap; }
    .footer-legal { padding-top: var(--space-lg); color: var(--color-on-brand-muted); font-size: var(--text-xs); line-height: 1.5; }
    button:focus-visible { outline: 2px solid var(--brand-accent); outline-offset: 3px; }
    @media (hover: hover) and (pointer: fine) { .footer-links button:hover { color: var(--brand-accent); } }
    @media (min-width: 48rem) { .footer-mast { grid-template-columns: minmax(0, 1fr) minmax(18rem, 0.7fr); align-items: end; } .footer-mast p { justify-self: end; text-align: end; } }
  `],
})
export class FooterPublicoComponent {
  private router = inject(Router);

  go(path: string): void {
    this.router.navigate([path]);
  }
}
