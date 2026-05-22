import {
  Component,
  Input,
  Output,
  EventEmitter,
  signal,
  HostListener,
  ElementRef,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

export type NavbarActiveLink =
  | 'particulares'
  | 'empresas'
  | 'tarifas'
  | 'oficinas'
  | 'ayuda'
  | null;

/**
 * Navbar pública unificada para nexopostal.es.
 * Replica el navbar de la home (escritorio + hoja móvil con hamburguesa).
 */
@Component({
  selector: 'app-navbar-publico',
  standalone: true,
  imports: [CommonModule],
  template: `
    <nav class="home-nav" [class.scrolled]="scrolled()">
      <div class="home-nav-inner">
        <!-- Logo -->
        <button class="home-brand" type="button" (click)="go('/')" aria-label="Ir al inicio">
          <span class="brand-icon material-symbols-outlined">local_shipping</span>
          <span class="brand-text">Nexo<span class="brand-accent">Postal</span></span>
        </button>

        <!-- Links escritorio -->
        @if (showLinks) {
          <ul class="home-nav-links">
            <li>
              <a (click)="go('/particulares')" [class.is-active]="activeLink === 'particulares'">Particulares</a>
            </li>
            <li>
              <a (click)="go('/empresas')" [class.is-active]="activeLink === 'empresas'">Empresas</a>
            </li>
            <li>
              <a (click)="go('/calculadora-tarifas')" [class.is-active]="activeLink === 'tarifas'">Tarifas</a>
            </li>
            <li>
              <a (click)="go('/buscador-oficinas')" [class.is-active]="activeLink === 'oficinas'">Oficinas</a>
            </li>
            <li>
              <a (click)="go('/ayuda')" [class.is-active]="activeLink === 'ayuda'">Ayuda</a>
            </li>
          </ul>
        }

        <div class="home-nav-actions">
          <!-- Botón "Volver" opcional (uso en páginas internas) -->
          @if (backLabel) {
            <button type="button" class="navbar-back" (click)="onBackClick()">
              <span class="material-symbols-outlined">arrow_back</span>
              <span class="navbar-back-text">{{ backLabel }}</span>
            </button>
          }

          @if (showAuth) {
            @if (currentUser()) {
              <!-- Usuario logueado -->
              <div class="user-menu-container">
                <button class="user-pill" type="button" (click)="toggleUserMenu()">
                  <span class="user-avatar"><span class="material-symbols-outlined">person</span></span>
                  <span class="user-name">{{ currentUser().user }}</span>
                  <span class="material-symbols-outlined chev" [class.open]="showUserMenu()">expand_more</span>
                </button>
                @if (showUserMenu()) {
                  <div class="dropdown fade-in">
                    <div class="dropdown-header">
                      <p class="dropdown-name">{{ currentUser().user }}</p>
                      <p class="dropdown-role">Cliente</p>
                    </div>
                    <button class="dropdown-item" type="button" (click)="navigateToPanel()">
                      <span class="material-symbols-outlined">dashboard</span> Panel de usuario
                    </button>
                    <hr class="dropdown-sep" />
                    <button class="dropdown-item danger" type="button" (click)="logout()">
                      <span class="material-symbols-outlined">logout</span> Cerrar sesión
                    </button>
                  </div>
                }
              </div>
            } @else {
              <!-- Usuario anónimo (sólo escritorio) -->
              <div class="auth-buttons">
                <button class="btn-link" type="button" (click)="onLoginClick()">Iniciar sesión</button>
                <button class="btn-accent btn-sm" type="button" (click)="onRegisterClick()">Registrarse</button>
              </div>
            }
          }

          <!-- Slot extra (botones específicos de cada página) -->
          <ng-content></ng-content>

          <!-- Hamburguesa móvil -->
          @if (showLinks || showAuth) {
            <div class="mobile-menu-container">
              <button
                class="mobile-toggle"
                type="button"
                (click)="toggleMobileMenu()"
                [attr.aria-expanded]="showMobileMenu()"
                aria-label="Menú">
                <span class="material-symbols-outlined">{{ showMobileMenu() ? 'close' : 'menu' }}</span>
              </button>
              @if (showMobileMenu()) {
                <div class="mobile-sheet scale-in">
                  @if (currentUser()) {
                    <div class="dropdown-header">
                      <p class="dropdown-name">{{ currentUser().user }}</p>
                      <p class="dropdown-role">Cliente</p>
                    </div>
                  }
                  @if (showLinks) {
                    <button class="dropdown-item" type="button" (click)="go('/particulares')">
                      <span class="material-symbols-outlined">person</span> Particulares
                    </button>
                    <button class="dropdown-item" type="button" (click)="go('/empresas')">
                      <span class="material-symbols-outlined">business</span> Empresas
                    </button>
                    <button class="dropdown-item" type="button" (click)="go('/calculadora-tarifas')">
                      <span class="material-symbols-outlined">calculate</span> Tarifas
                    </button>
                    <button class="dropdown-item" type="button" (click)="go('/buscador-oficinas')">
                      <span class="material-symbols-outlined">storefront</span> Oficinas
                    </button>
                    <button class="dropdown-item" type="button" (click)="go('/ayuda')">
                      <span class="material-symbols-outlined">help</span> Ayuda
                    </button>
                  }
                  @if (showAuth) {
                    <hr class="dropdown-sep" />
                    @if (currentUser()) {
                      <button class="dropdown-item" type="button" (click)="navigateToPanel()">
                        <span class="material-symbols-outlined">dashboard</span> Panel de usuario
                      </button>
                      <button class="dropdown-item danger" type="button" (click)="logout()">
                        <span class="material-symbols-outlined">logout</span> Cerrar sesión
                      </button>
                    } @else {
                      <button class="dropdown-item" type="button" (click)="onLoginClick()">
                        <span class="material-symbols-outlined">login</span> Iniciar sesión
                      </button>
                      <button class="dropdown-item highlight" type="button" (click)="onRegisterClick()">
                        <span class="material-symbols-outlined">person_add</span> Registrarse
                      </button>
                    }
                  }
                </div>
              }
            </div>
          }
        </div>
      </div>
    </nav>
  `,
  styles: [`
    :host { display: block; }

    /* ============ NAVBAR ============ */
    .home-nav {
      position: sticky; top: 0; z-index: 50;
      background: var(--brand-primary-strong, linear-gradient(135deg, #0F172A 0%, #1A237E 55%, #283593 100%));
      color: #fff;
      border-bottom: 1px solid rgba(255,255,255,0.06);
      transition: background var(--dur, 0.2s) var(--ease, ease), border-color var(--dur, 0.2s) var(--ease, ease);
    }
    .home-nav.scrolled {
      background: color-mix(in srgb, var(--brand-primary-strong, #1A237E) 80%, transparent);
      backdrop-filter: saturate(150%) blur(14px);
      -webkit-backdrop-filter: saturate(150%) blur(14px);
      box-shadow: 0 8px 24px -16px rgba(0,0,0,0.55);
    }
    .home-nav-inner {
      max-width: 1280px; margin: 0 auto;
      padding: 0 1.5rem; height: 72px;
      display: flex; align-items: center; justify-content: space-between; gap: 1.25rem;
    }
    .home-brand {
      display: inline-flex; align-items: center; gap: 0.55rem;
      background: transparent; border: 0; color: inherit;
      font: 800 1.35rem var(--font-sans, system-ui); letter-spacing: -0.02em;
      cursor: pointer;
    }
    .home-brand .brand-icon { font-size: 1.85rem; color: var(--brand-accent, #FFC107); }
    .brand-accent { color: var(--brand-accent, #FFC107); }

    .home-nav-links {
      display: none; align-items: center; gap: 1.75rem;
      list-style: none; margin: 0; padding: 0;
    }
    .home-nav-links a {
      color: rgba(255,255,255,0.86);
      font: 500 0.9rem var(--font-sans, system-ui);
      cursor: pointer;
      position: relative;
      padding: 0.25rem 0;
      transition: color var(--dur, 0.2s) var(--ease, ease);
    }
    .home-nav-links a::after {
      content: ''; position: absolute; left: 0; right: 0; bottom: -4px;
      height: 2px; background: var(--brand-accent, #FFC107);
      transform: scaleX(0); transform-origin: center;
      transition: transform var(--dur, 0.2s) var(--ease, ease);
    }
    .home-nav-links a:hover { color: #fff; }
    .home-nav-links a:hover::after,
    .home-nav-links a.is-active::after { transform: scaleX(1); }
    .home-nav-links a.is-active { color: #fff; font-weight: 600; }
    @media (min-width: 1024px) { .home-nav-links { display: flex; } }

    .home-nav-actions { display: flex; align-items: center; gap: 0.6rem; color: #fff; }

    /* En móvil sólo mostramos la hamburguesa: ocultamos user-pill y botones de auth */
    .user-menu-container,
    .auth-buttons { display: none; }
    @media (min-width: 1024px) {
      .user-menu-container { display: inline-block; position: relative; }
      .auth-buttons { display: inline-flex; gap: 0.5rem; align-items: center; }
    }
    .btn-link {
      background: transparent; border: 0; color: #fff;
      font: 600 0.875rem var(--font-sans, system-ui);
      padding: 0.45rem 0.75rem; cursor: pointer;
      border-radius: var(--radius-md, 0.5rem);
      transition: background var(--dur, 0.2s) var(--ease, ease);
    }
    .btn-link:hover { background: rgba(255,255,255,0.1); }
    .btn-accent {
      background: var(--brand-accent, #FFC107);
      color: var(--brand-primary, #1A237E);
      border: 0; cursor: pointer;
      font: 700 0.875rem var(--font-sans, system-ui);
      padding: 0.5rem 1.1rem;
      border-radius: var(--radius-md, 0.5rem);
      transition: background var(--dur, 0.2s) var(--ease, ease), transform var(--dur, 0.2s) var(--ease, ease);
    }
    .btn-accent:hover { background: #FFB300; transform: translateY(-1px); }
    .btn-sm { padding: 0.45rem 0.95rem; font-size: 0.85rem; }

    /* Botón "Volver" */
    .navbar-back {
      display: inline-flex; align-items: center; gap: 0.4rem;
      background: transparent; border: 0;
      color: rgba(255,255,255,0.9);
      font: 500 0.875rem var(--font-sans, system-ui);
      cursor: pointer;
      padding: 0.5rem 0.75rem;
      border-radius: var(--radius-md, 0.5rem);
      transition: color var(--dur, 0.2s) var(--ease, ease), background var(--dur, 0.2s) var(--ease, ease);
    }
    .navbar-back:hover { color: var(--brand-accent, #FFC107); background: rgba(255,255,255,0.08); }
    .navbar-back-text { display: none; }
    @media (min-width: 640px) { .navbar-back-text { display: inline; } }

    /* User pill */
    .user-pill {
      display: inline-flex; align-items: center; gap: 0.55rem;
      background: rgba(255,255,255,0.08);
      border: 1px solid rgba(255,255,255,0.14);
      color: #fff;
      padding: 0.3rem 0.85rem 0.3rem 0.35rem;
      border-radius: var(--radius-pill, 999px);
      cursor: pointer;
      transition: background var(--dur, 0.2s) var(--ease, ease), border-color var(--dur, 0.2s) var(--ease, ease);
    }
    .user-pill:hover { background: rgba(255,255,255,0.14); border-color: rgba(255,255,255,0.28); }
    .user-avatar {
      width: 32px; height: 32px; border-radius: 50%;
      background: var(--gradient-accent, linear-gradient(135deg, #FFC107, #FFB300));
      color: var(--brand-primary, #1A237E);
      display: inline-flex; align-items: center; justify-content: center;
    }
    .user-avatar .material-symbols-outlined { font-size: 1.1rem; font-variation-settings: 'FILL' 1; }
    .user-name {
      font: 600 0.85rem var(--font-sans, system-ui);
      max-width: 10rem;
      overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    }
    .user-pill .chev { font-size: 1.1rem; opacity: 0.85; transition: transform 0.2s ease; }
    .user-pill .chev.open { transform: rotate(180deg); }

    .dropdown {
      position: absolute; right: 0; top: calc(100% + 0.5rem);
      min-width: 240px;
      background: var(--surface, #fff);
      color: var(--text, #0F172A);
      border: 1px solid var(--border-soft, #E5E7EB);
      border-radius: var(--radius-md, 0.5rem);
      box-shadow: var(--shadow-lg, 0 20px 40px -16px rgba(15,23,42,0.35), 0 4px 12px -8px rgba(15,23,42,0.25));
      padding: 0.4rem;
      z-index: 60;
      animation: dropdown-in 0.18s ease;
    }
    @keyframes dropdown-in {
      from { opacity: 0; transform: translateY(-6px); }
      to { opacity: 1; transform: translateY(0); }
    }
    .dropdown-header { padding: 0.7rem 0.85rem 0.65rem; border-bottom: 1px solid var(--border-soft, #E5E7EB); margin-bottom: 0.35rem; }
    .dropdown-name { font: 700 0.9rem var(--font-sans, system-ui); color: var(--text, #0F172A); margin: 0; }
    .dropdown-role { font-size: 0.75rem; color: var(--text-muted, #64748B); margin: 0.15rem 0 0; }
    .dropdown-sep { border: 0; height: 1px; background: var(--border-soft, #E5E7EB); margin: 0.35rem 0; }
    .dropdown-item {
      width: 100%;
      display: flex; align-items: center; gap: 0.65rem;
      padding: 0.6rem 0.75rem;
      background: transparent; border: 0;
      border-radius: var(--radius-sm, 0.35rem);
      font: 500 0.875rem var(--font-sans, system-ui);
      color: var(--text, #0F172A);
      cursor: pointer; text-align: left;
      transition: background var(--dur, 0.2s) var(--ease, ease), color var(--dur, 0.2s) var(--ease, ease);
    }
    .dropdown-item:hover { background: var(--surface-muted, #F4F6FB); color: var(--brand-primary, #1A237E); }
    :host-context(.dark) .dropdown-item:hover { color: var(--brand-accent, #FFC107); }
    .dropdown-item .material-symbols-outlined { font-size: 1.2rem; color: var(--text-muted, #64748B); }
    .dropdown-item:hover .material-symbols-outlined { color: inherit; }
    .dropdown-item.danger { color: var(--danger, #B91C1C); }
    .dropdown-item.danger:hover { background: var(--danger-soft, #FEF2F2); color: var(--danger, #B91C1C); }
    .dropdown-item.highlight { color: var(--brand-primary, #1A237E); font-weight: 700; }
    :host-context(.dark) .dropdown-item.highlight { color: var(--brand-accent, #FFC107); }

    /* Mobile menu */
    .mobile-menu-container { position: relative; display: inline-flex; }
    .mobile-toggle {
      width: 40px; height: 40px; border-radius: 999px;
      border: 1px solid rgba(255,255,255,0.22);
      background: transparent; color: #fff;
      display: inline-flex; align-items: center; justify-content: center;
      cursor: pointer;
      transition: background var(--dur, 0.2s) var(--ease, ease), border-color var(--dur, 0.2s) var(--ease, ease);
    }
    .mobile-toggle:hover { background: rgba(255,255,255,0.12); border-color: rgba(255,255,255,0.4); }
    @media (min-width: 1024px) { .mobile-menu-container { display: none; } }
    .mobile-sheet {
      position: absolute; right: 0; top: calc(100% + 0.5rem);
      min-width: 260px; max-width: 90vw;
      background: var(--surface, #fff);
      color: var(--text, #0F172A);
      border: 1px solid var(--border-soft, #E5E7EB);
      border-radius: var(--radius-md, 0.5rem);
      box-shadow: var(--shadow-lg, 0 20px 40px -16px rgba(15,23,42,0.35));
      padding: 0.4rem;
      z-index: 60;
      animation: dropdown-in 0.18s ease;
    }

    @media (max-width: 640px) {
      .home-nav-inner { padding: 0 1rem; height: 64px; }
      .home-brand { font-size: 1.15rem; }
      .home-brand .brand-icon { font-size: 1.5rem; }
      .user-name { max-width: 7rem; }
    }
  `],
})
export class NavbarPublicoComponent {
  @Input() activeLink: NavbarActiveLink = null;
  @Input() showLinks = true;
  @Input() showAuth = true;
  /** Si se define, muestra un botón "← {backLabel}" a la izquierda de las acciones. */
  @Input() backLabel: string | null = null;
  /** Ruta a navegar al pulsar el botón back. Si se omite, se emite el evento backClick. */
  @Input() backRoute: string | null = '/';

  @Output() backClick = new EventEmitter<void>();
  @Output() loginClick = new EventEmitter<void>();
  @Output() registerClick = new EventEmitter<void>();

  private router = inject(Router);
  private auth = inject(AuthService);
  private elementRef = inject(ElementRef);

  currentUser = signal<any>(null);
  showUserMenu = signal(false);
  showMobileMenu = signal(false);
  scrolled = signal(false);

  constructor() {
    this.auth.currentUser$.subscribe(u => this.currentUser.set(u));
  }

  go(route: string): void {
    this.showUserMenu.set(false);
    this.showMobileMenu.set(false);
    this.router.navigate([route]);
  }

  toggleUserMenu(): void {
    this.showUserMenu.update(v => !v);
    if (this.showUserMenu()) this.showMobileMenu.set(false);
  }

  toggleMobileMenu(): void {
    this.showMobileMenu.update(v => !v);
    if (this.showMobileMenu()) this.showUserMenu.set(false);
  }

  navigateToPanel(): void {
    this.showUserMenu.set(false);
    this.showMobileMenu.set(false);
    this.router.navigate(['/panel']);
  }

  onBackClick(): void {
    if (this.backClick.observed) {
      this.backClick.emit();
      return;
    }
    if (this.backRoute) {
      this.router.navigate([this.backRoute]);
    }
  }

  onLoginClick(): void {
    this.showMobileMenu.set(false);
    if (this.loginClick.observed) {
      this.loginClick.emit();
    } else {
      // Fallback: la página home aloja los modales de auth
      this.router.navigate(['/'], { queryParams: { auth: 'login' } });
    }
  }

  onRegisterClick(): void {
    this.showMobileMenu.set(false);
    if (this.registerClick.observed) {
      this.registerClick.emit();
    } else {
      this.router.navigate(['/'], { queryParams: { auth: 'register' } });
    }
  }

  logout(): void {
    this.auth.logout();
    this.showUserMenu.set(false);
    this.showMobileMenu.set(false);
    this.router.navigate(['/']);
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.scrolled.set(window.scrollY > 8);
  }

  @HostListener('document:click', ['$event'])
  onDocClick(event: MouseEvent): void {
    const target = event.target as Node;
    const host: HTMLElement = this.elementRef.nativeElement;

    if (this.showUserMenu()) {
      const userMenu = host.querySelector('.user-menu-container');
      if (userMenu && !userMenu.contains(target)) {
        this.showUserMenu.set(false);
      }
    }

    if (this.showMobileMenu()) {
      const mobileMenu = host.querySelector('.mobile-menu-container');
      if (mobileMenu && !mobileMenu.contains(target)) {
        this.showMobileMenu.set(false);
      }
    }
  }
}
