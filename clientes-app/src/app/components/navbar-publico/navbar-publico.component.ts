import { Component, Input, Output, EventEmitter, signal, HostListener, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

export type NavbarActiveLink = 'particulares' | 'empresas' | 'tarifas' | 'oficinas' | 'ayuda' | null;

@Component({
  selector: 'app-navbar-publico',
  standalone: true,
  imports: [CommonModule],
  template: `
    <nav class="navbar-publico">
      <div class="navbar-publico-inner">
        <!-- Logo -->
        <button class="navbar-brand" type="button" (click)="go('/')" aria-label="Ir al inicio">
          <span class="material-symbols-outlined navbar-brand-icon">local_shipping</span>
          <span class="navbar-brand-text">Nexo<span class="navbar-brand-accent">Postal</span></span>
        </button>

        <!-- Links navegación -->
        @if (showLinks) {
          <ul class="navbar-links">
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

        <!-- Acciones derecha -->
        <div class="navbar-actions">
          @if (backLabel) {
            <button type="button" class="navbar-back" (click)="onBackClick()">
              <span class="material-symbols-outlined">arrow_back</span>
              <span class="navbar-back-text">{{ backLabel }}</span>
            </button>
          }

          @if (showAuth) {
            @if (currentUser()) {
              <div class="navbar-user" #userMenu>
                <button type="button" class="navbar-user-pill" (click)="toggleUserMenu()">
                  <span class="navbar-user-avatar"><span class="material-symbols-outlined">person</span></span>
                  <span class="navbar-user-name">{{ currentUser()?.user }}</span>
                  <span class="material-symbols-outlined navbar-user-chev" [class.open]="showUserMenu()">expand_more</span>
                </button>
                @if (showUserMenu()) {
                  <div class="navbar-dropdown">
                    <div class="navbar-dropdown-header">
                      <p class="navbar-dropdown-name">{{ currentUser()?.user }}</p>
                      <p class="navbar-dropdown-role">Cliente</p>
                    </div>
                    <button type="button" class="navbar-dropdown-item" (click)="go('/panel')">
                      <span class="material-symbols-outlined">dashboard</span> Panel de usuario
                    </button>
                    <hr class="navbar-dropdown-sep" />
                    <button type="button" class="navbar-dropdown-item danger" (click)="logout()">
                      <span class="material-symbols-outlined">logout</span> Cerrar sesión
                    </button>
                  </div>
                }
              </div>
            } @else {
              <button type="button" class="navbar-cta" (click)="go('/')">
                Ir al inicio
              </button>
            }
          }

          <ng-content></ng-content>
        </div>
      </div>
    </nav>
  `,
  styles: [`
    .navbar-publico {
      background: linear-gradient(135deg, #0F172A 0%, #1A237E 55%, #283593 100%);
      color: #fff;
      height: 70px;
      position: sticky;
      top: 0;
      z-index: 50;
      box-shadow: 0 8px 24px -16px rgba(0, 0, 0, 0.55);
      border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    }
    .navbar-publico-inner {
      max-width: 80rem;
      margin: 0 auto;
      padding: 0 1.25rem;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
    }
    .navbar-brand {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      background: transparent;
      border: 0;
      color: inherit;
      cursor: pointer;
      font-size: 1.4rem;
      font-weight: 700;
      letter-spacing: -0.01em;
      padding: 0;
    }
    .navbar-brand-icon { font-size: 1.85rem; color: #FFC107; }
    .navbar-brand-accent { color: #FFC107; }

    .navbar-links {
      display: none;
      gap: 1.75rem;
      list-style: none;
      margin: 0;
      padding: 0;
    }
    @media (min-width: 768px) {
      .navbar-links { display: flex; }
    }
    .navbar-links a {
      display: inline-block;
      cursor: pointer;
      font-size: 0.875rem;
      color: rgba(255, 255, 255, 0.85);
      padding: 0.25rem 0;
      transition: color 0.15s ease, border-color 0.15s ease;
      border-bottom: 2px solid transparent;
    }
    .navbar-links a:hover { color: #fff; }
    .navbar-links a.is-active {
      color: #fff;
      font-weight: 600;
      border-bottom-color: #FFC107;
    }

    .navbar-actions {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
    .navbar-cta {
      padding: 0.5rem 1.25rem;
      border-radius: 0.625rem;
      background: #FFC107;
      color: #1A237E;
      font-weight: 600;
      font-size: 0.875rem;
      border: 0;
      cursor: pointer;
      transition: background 0.15s ease, transform 0.15s ease;
    }
    .navbar-cta:hover { background: #FFB300; transform: translateY(-1px); }

    .navbar-back {
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;
      background: transparent;
      border: 0;
      color: rgba(255, 255, 255, 0.9);
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      padding: 0.5rem 0.75rem;
      border-radius: 0.5rem;
      transition: color 0.15s ease, background 0.15s ease;
    }
    .navbar-back:hover {
      color: #FFC107;
      background: rgba(255, 255, 255, 0.06);
    }
    .navbar-back-text { display: none; }
    @media (min-width: 640px) { .navbar-back-text { display: inline; } }

    .navbar-user { position: relative; }
    .navbar-user-pill {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      background: rgba(255, 255, 255, 0.08);
      border: 1px solid rgba(255, 255, 255, 0.12);
      color: #fff;
      padding: 0.4rem 0.65rem 0.4rem 0.4rem;
      border-radius: 999px;
      cursor: pointer;
      font-size: 0.875rem;
      transition: background 0.15s ease;
    }
    .navbar-user-pill:hover { background: rgba(255, 255, 255, 0.14); }
    .navbar-user-avatar {
      width: 28px;
      height: 28px;
      background: #FFC107;
      color: #1A237E;
      border-radius: 999px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }
    .navbar-user-avatar .material-symbols-outlined { font-size: 18px; }
    .navbar-user-name { font-weight: 500; max-width: 8rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .navbar-user-chev { font-size: 1.1rem; transition: transform 0.2s ease; }
    .navbar-user-chev.open { transform: rotate(180deg); }

    .navbar-dropdown {
      position: absolute;
      top: calc(100% + 0.5rem);
      right: 0;
      min-width: 220px;
      background: #fff;
      color: #0F172A;
      border-radius: 0.875rem;
      box-shadow: 0 20px 40px -16px rgba(15, 23, 42, 0.35), 0 4px 12px -8px rgba(15, 23, 42, 0.25);
      padding: 0.4rem;
      animation: dropdown-in 0.18s ease;
    }
    @keyframes dropdown-in {
      from { opacity: 0; transform: translateY(-6px); }
      to { opacity: 1; transform: translateY(0); }
    }
    .navbar-dropdown-header { padding: 0.5rem 0.75rem 0.4rem; }
    .navbar-dropdown-name { font-weight: 600; font-size: 0.875rem; margin: 0; }
    .navbar-dropdown-role { font-size: 0.75rem; color: #64748B; margin: 0; }
    .navbar-dropdown-sep { border: 0; border-top: 1px solid #EDF1F7; margin: 0.4rem 0; }
    .navbar-dropdown-item {
      display: flex;
      align-items: center;
      gap: 0.55rem;
      width: 100%;
      background: transparent;
      border: 0;
      padding: 0.5rem 0.75rem;
      border-radius: 0.5rem;
      cursor: pointer;
      font-size: 0.875rem;
      color: inherit;
      text-align: left;
      transition: background 0.12s ease;
    }
    .navbar-dropdown-item:hover { background: #F4F6FB; }
    .navbar-dropdown-item.danger { color: #B91C1C; }
    .navbar-dropdown-item.danger:hover { background: #FEF2F2; }
    .navbar-dropdown-item .material-symbols-outlined { font-size: 1.15rem; color: #1A237E; }
    .navbar-dropdown-item.danger .material-symbols-outlined { color: #B91C1C; }
  `]
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

  private router = inject(Router);
  private auth = inject(AuthService);
  private elementRef = inject(ElementRef);

  currentUser = signal<any>(null);
  showUserMenu = signal(false);

  constructor() {
    this.auth.currentUser$.subscribe(u => this.currentUser.set(u));
  }

  go(route: string): void {
    this.showUserMenu.set(false);
    this.router.navigate([route]);
  }

  toggleUserMenu(): void {
    this.showUserMenu.update(v => !v);
  }

  onBackClick(): void {
    if (this.backRoute) {
      this.router.navigate([this.backRoute]);
    } else {
      this.backClick.emit();
    }
  }

  logout(): void {
    this.auth.logout();
    this.showUserMenu.set(false);
    this.router.navigate(['/']);
  }

  @HostListener('document:click', ['$event'])
  onDocClick(event: MouseEvent): void {
    if (!this.showUserMenu()) return;
    const target = event.target as Node;
    const menu = this.elementRef.nativeElement.querySelector('.navbar-user');
    if (menu && !menu.contains(target)) {
      this.showUserMenu.set(false);
    }
  }
}
