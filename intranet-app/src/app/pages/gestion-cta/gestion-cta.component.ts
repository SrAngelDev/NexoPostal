import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { IntranetApiService, MisCtasInfo, CtaAsignacion, DashboardCta } from '../../services/intranet-api.service';
import { SignalrService, NotificacionSignalR } from '../../services/signalr.service';
import { IntranetNavbarComponent } from '../../components/intranet-navbar/intranet-navbar.component';

@Component({
  selector: 'app-gestion-cta',
  standalone: true,
  imports: [CommonModule, FormsModule, IntranetNavbarComponent],
  templateUrl: './gestion-cta.component.html',
  styleUrl: './gestion-cta.component.css'
})
export class GestionCtaComponent implements OnInit, OnDestroy {
  userName = '';
  userRole = '';

  misCtasInfo = signal<MisCtasInfo | null>(null);
  ctaSeleccionado = signal<CtaAsignacion | null>(null);
  dashboard = signal<DashboardCta | null>(null);
  loading = signal(true);
  error = signal('');

  // Notificaciones
  showNotificaciones = signal(false);

  constructor(
    private authService: AuthService,
    private intranetApi: IntranetApiService,
    public signalr: SignalrService,
    private router: Router
  ) {
    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
    this.userRole = user?.rol ?? '';
  }

  ngOnInit(): void {
    // OperarioCTA no necesita esta vista general — va directo a sus tareas
    if (this.userRole === 'OperarioCTA') {
      this.router.navigate(['/asignaciones'], { replaceUrl: true });
      return;
    }

    // Conectar SignalR
    this.signalr.conectar();

    // Cargar datos del CTA
    this.cargarDatos();
  }

  ngOnDestroy(): void {
    // No desconectamos SignalR aquí, se mantiene mientras la app esté abierta
  }

  cargarDatos(): void {
    this.loading.set(true);
    this.error.set('');

    this.intranetApi.obtenerMisCtas().subscribe({
      next: (info) => {
        this.misCtasInfo.set(info);
        // Seleccionar el primer CTA por defecto
        if (info.ctas.length > 0) {
          this.seleccionarCta(info.ctas[0]);
        } else {
          this.loading.set(false);
        }
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 404) {
          this.error.set('No estás asignado a ningún CTA. Contacta con un administrador.');
        } else {
          this.error.set('Error al cargar la información del CTA.');
        }
      }
    });
  }

  seleccionarCta(cta: CtaAsignacion): void {
    this.ctaSeleccionado.set(cta);
    this.loading.set(true);
    this.intranetApi.obtenerDashboardCta(cta.ctaId).subscribe({
      next: (dash) => {
        this.dashboard.set(dash);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  onCtaChange(ctaId: number): void {
    const info = this.misCtasInfo();
    if (!info) return;
    const cta = info.ctas.find(c => c.ctaId === ctaId);
    if (cta) this.seleccionarCta(cta);
  }

  toggleNotificaciones(): void {
    const nuevo = !this.showNotificaciones();
    this.showNotificaciones.set(nuevo);
    if (nuevo) {
      this.signalr.marcarComoLeidas();
    }
  }

  cerrarNotificaciones(): void {
    this.showNotificaciones.set(false);
  }

  volverDashboard(): void {
    this.router.navigate(['/']);
  }

  irAsignaciones(): void {
    this.router.navigate(['/asignaciones']);
  }

  irSeguimientoInterno(): void {
    this.router.navigate(['/seguimiento-interno']);
  }

  logout(): void {
    this.signalr.desconectar();
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  formatearFecha(fecha: string): string {
    if (!fecha) return '—';
    return new Date(fecha).toLocaleTimeString('es-ES', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
