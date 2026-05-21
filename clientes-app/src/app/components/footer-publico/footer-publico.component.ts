import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

/**
 * Footer público unificado para nexopostal.es.
 * Replica el footer de la home.
 */
@Component({
  selector: 'app-footer-publico',
  standalone: true,
  imports: [CommonModule],
  template: `
    <footer class="footer">
      <div class="footer-inner">
        <div class="footer-cols">
          <div class="footer-brand">
            <div class="footer-logo">
              <span class="material-symbols-outlined">local_shipping</span>
              <span>Nexo<span class="brand-accent">Postal</span></span>
            </div>
            <p>Operador logístico nacional líder en tecnología y rapidez.</p>
          </div>
          <div class="footer-col">
            <h4>Servicios</h4>
            <a (click)="go('/nuevo-envio')">Enviar paquete</a>
            <a (click)="go('/calculadora-tarifas')">Calculadora</a>
            <a (click)="go('/buscador-oficinas')">Oficinas</a>
          </div>
          <div class="footer-col">
            <h4>Compañía</h4>
            <a (click)="go('/particulares')">Particulares</a>
            <a (click)="go('/empresas')">Empresas</a>
            <a (click)="go('/ayuda')">Ayuda</a>
          </div>
          <div class="footer-col">
            <h4>Legal</h4>
            <a (click)="go('/politica-privacidad')">Política de privacidad</a>
            <a (click)="go('/terminos-uso')">Términos de uso</a>
          </div>
        </div>
        <div class="footer-bottom">
          <p>&copy; 2026 NexoPostal S.A. Todos los derechos reservados.</p>
        </div>
      </div>
    </footer>
  `,
  styles: [`
    :host { display: block; }

    .footer {
      background: #0B1220;
      color: #94A3B8;
      padding: 4rem 1.5rem 2rem;
    }
    :host-context(.dark) .footer { background: #060A14; }

    .footer-inner { max-width: 1180px; margin: 0 auto; }
    .footer-cols {
      display: grid; gap: 2.5rem;
      grid-template-columns: 1.4fr repeat(3, 1fr);
    }
    @media (max-width: 768px) { .footer-cols { grid-template-columns: 1fr 1fr; } }
    @media (max-width: 480px) { .footer-cols { grid-template-columns: 1fr; } }

    .footer-brand p { font-size: 0.9rem; max-width: 26ch; color: #94A3B8; margin: 0; }
    .footer-logo {
      display: inline-flex; align-items: center; gap: 0.5rem;
      font: 800 1.15rem var(--font-sans, system-ui); color: #fff;
      margin-bottom: 0.85rem;
    }
    .footer-logo .material-symbols-outlined { color: var(--brand-accent, #FFC107); }
    .brand-accent { color: var(--brand-accent, #FFC107); }

    .footer-col h4 {
      color: #fff;
      font: 700 0.95rem var(--font-sans, system-ui);
      margin: 0 0 0.85rem;
    }
    .footer-col a {
      display: block; padding: 0.35rem 0;
      color: #94A3B8; font-size: 0.875rem;
      cursor: pointer;
      transition: color var(--dur, 0.2s) var(--ease, ease);
    }
    .footer-col a:hover { color: var(--brand-accent, #FFC107); }

    .footer-bottom {
      border-top: 1px solid rgba(255,255,255,0.08);
      margin-top: 3rem; padding-top: 1.5rem; text-align: center;
      font-size: 0.85rem; color: #64748B;
    }
  `],
})
export class FooterPublicoComponent {
  private router = inject(Router);

  go(path: string): void {
    this.router.navigate([path]);
  }
}
