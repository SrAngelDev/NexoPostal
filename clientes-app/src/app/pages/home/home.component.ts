import { Component, signal, HostListener, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LoginComponent } from '../login/login.component';
import { RegisterComponent } from '../register/register.component';
import { AuthService } from '../../services/auth.service';
import { PerfilService } from '../../services/perfil.service';
import { UsuarioService } from '../../services/usuario.service';
import { NotificacionService } from '../../services/notificacion.service';
import { ConfirmacionService } from '../../services/confirmacion.service';
import { EnviosService } from '../../services/envios.service';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, LoginComponent, RegisterComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements AfterViewInit, OnDestroy {
  trackingCode = signal('');
  scrolled = signal(false);
  private revealObserver?: IntersectionObserver;
  isSearching = signal(false);
  trackingResult = signal<any>(null);
  trackingError = signal<string>('');
  showLoginModal = signal(false);
  showRegisterModal = signal(false);
  showUserMenu = signal(false);
  showMobileMenu = signal(false);
  currentUser = signal<any>(null);
  perfilIncompleto = signal(false);
  camposFaltantes = signal<string[]>([]);

  constructor(
    private router: Router,
    private authService: AuthService,
    private perfilService: PerfilService,
    private usuarioService: UsuarioService,
    private notificacion: NotificacionService,
    private confirmacionService: ConfirmacionService,
    private enviosService: EnviosService,
    private elementRef: ElementRef
  ) {
    // Suscribirse al usuario actual
    this.authService.currentUser$.subscribe(user => {
      this.currentUser.set(user);
      if (user) {
        this.verificarPerfil();
      } else {
        this.perfilIncompleto.set(false);
        this.camposFaltantes.set([]);
      }
    });
  }

  // Cerrar el menú al hacer clic fuera
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node;
    const userMenu = this.elementRef.nativeElement.querySelector('.user-menu-container');
    const mobileMenu = this.elementRef.nativeElement.querySelector('.mobile-menu-container');

    if (this.showUserMenu() && userMenu && !userMenu.contains(target)) {
      this.closeUserMenu();
    }

    if (this.showMobileMenu() && mobileMenu && !mobileMenu.contains(target)) {
      this.closeMobileMenu();
    }
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.scrolled.set(window.scrollY > 8);
  }

  scrollToSection(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  ngAfterViewInit(): void {
    if (typeof IntersectionObserver === 'undefined') { return; }
    this.revealObserver = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          entry.target.classList.add('in-view');
          this.revealObserver?.unobserve(entry.target);
        }
      }
    }, { rootMargin: '0px 0px -80px 0px', threshold: 0.05 });

    const targets = this.elementRef.nativeElement.querySelectorAll('.reveal');
    targets.forEach((el: Element) => this.revealObserver!.observe(el));
  }

  ngOnDestroy(): void {
    this.revealObserver?.disconnect();
  }

  onTrackingSubmit(): void {
    const code = this.trackingCode().trim();
    
    if (code.length < 5) {
      this.notificacion.aviso('Código inválido', 'Introduce un código de seguimiento válido.');
      return;
    }

    this.isSearching.set(true);
    this.trackingResult.set(null);
    this.trackingError.set('');

    this.enviosService.consultarEnvio(code).subscribe({
      next: (resultado) => {
        this.trackingResult.set(resultado);
        this.isSearching.set(false);
      },
      error: (err) => {
        this.isSearching.set(false);
        if (err.status === 404) {
          this.trackingError.set('No se encontró ningún envío con ese código de seguimiento.');
        } else {
          this.trackingError.set('Error al consultar el seguimiento. Inténtalo de nuevo.');
        }
      }
    });
  }

  // Pasos del stepper (orden lógico del ciclo de vida del envío)
  trackingSteps = [
    { key: 'Admitido',   label: 'Admitido',     icon: 'inventory_2' },
    { key: 'EnTransito', label: 'En tránsito',   icon: 'local_shipping' },
    { key: 'EnReparto',  label: 'En reparto',    icon: 'two_wheeler' },
    { key: 'Entregado',  label: 'Entregado',     icon: 'home' }
  ];

  /** Devuelve true si el estado es de error (rojo) */
  isEstadoError(estado: string): boolean {
    return estado === 'Devuelto' || estado === 'Incidencia';
  }

  formatearEstado(estado: string): string {
    const estados: { [key: string]: string } = {
      'PendientePago': 'Pendiente de pago',
      'Admitido': 'Admitido',
      'EnTransito': 'En tránsito',
      'EnOficina': 'En oficina',
      'EnReparto': 'En reparto',
      'Entregado': 'Entregado',
      'Devuelto': 'Devuelto',
      'Incidencia': 'Incidencia'
    };
    return estados[estado] || estado;
  }

  /** Devuelve el índice del paso actual en el stepper */
  getStepIndex(estado: string): number {
    const mapa: { [key: string]: number } = {
      'PendientePago': -1,
      'Admitido': 0,
      'EnTransito': 1,
      'EnOficina': 2,
      'EnReparto': 2,
      'Entregado': 3,
      'Devuelto': 3,
      'Incidencia': -1
    };
    return mapa[estado] ?? -1;
  }

  /** Devuelve true si el estado es final (tick verde) */
  isEstadoFinal(estado: string): boolean {
    return estado === 'Entregado';
  }

  navigateToWizard(): void {
    this.router.navigate(['/nuevo-envio']);
  }

  navigateToCalculadora(): void {
    this.router.navigate(['/calculadora-tarifas']);
  }

  navigateToBuscadorOficinas(): void {
    this.router.navigate(['/buscador-oficinas']);
  }

  // Métodos del menú de usuario
  toggleUserMenu(): void {
    this.showUserMenu.set(!this.showUserMenu());
  }

  closeUserMenu(): void {
    this.showUserMenu.set(false);
  }

  // Menú móvil
  toggleMobileMenu(): void {
    this.showMobileMenu.set(!this.showMobileMenu());
    if (this.showMobileMenu()) {
      this.showUserMenu.set(false);
    }
  }

  closeMobileMenu(): void {
    this.showMobileMenu.set(false);
  }

  navigateToPanel(): void {
    this.closeUserMenu();
    this.closeMobileMenu();
    this.router.navigate(['/panel']);
  }

  navigateTo(path: string): void {
    this.closeMobileMenu();
    this.router.navigate([path]);
  }

  // Verificación de perfil completo
  // Comprueba datos de ambos microservicios: Ciudadano (perfil) e Identity (usuario)
  verificarPerfil(): void {
    forkJoin({
      perfil: this.perfilService.obtenerPerfil(),
      usuario: this.usuarioService.obtenerUsuario()
    }).subscribe({
      next: ({ perfil, usuario }) => {
        const faltantes: string[] = [];
        if (!perfil.dni) faltantes.push('DNI');
        // Teléfono puede estar en Identity (phoneNumber) o Ciudadano (telefono)
        if (!usuario.phoneNumber && !perfil.telefono) faltantes.push('Teléfono');
        if (!perfil.direccionPredeterminada) faltantes.push('Dirección');
        this.camposFaltantes.set(faltantes);
        this.perfilIncompleto.set(faltantes.length > 0);
      },
      error: () => {
        this.camposFaltantes.set(['DNI', 'Teléfono', 'Dirección']);
        this.perfilIncompleto.set(true);
      }
    });
  }

  cerrarAvisoPerfil(): void {
    this.perfilIncompleto.set(false);
  }

  // Métodos de autenticación
  openLoginModal(): void {
    this.closeMobileMenu();
    this.showLoginModal.set(true);
  }

  closeLoginModal(): void {
    this.showLoginModal.set(false);
  }

  openRegisterModal(): void {
    this.closeMobileMenu();
    this.showRegisterModal.set(true);
  }

  closeRegisterModal(): void {
    this.showRegisterModal.set(false);
  }

  switchToRegister(): void {
    this.closeLoginModal();
    this.closeMobileMenu();
    this.openRegisterModal();
  }

  switchToLogin(): void {
    this.closeRegisterModal();
    this.closeMobileMenu();
    this.openLoginModal();
  }

  async logout(): Promise<void> {
    this.closeUserMenu();
    this.closeMobileMenu();
    const ok = await this.confirmacionService.confirmar({
      titulo: 'Cerrar sesión',
      mensaje: '¿Estás seguro de que deseas cerrar sesión?',
      textoConfirmar: 'Cerrar sesión',
      tipo: 'peligro'
    });
    if (!ok) return;
    this.authService.logout();
    this.currentUser.set(null);
  }
}
