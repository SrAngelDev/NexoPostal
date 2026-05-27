import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import {
  IntranetApiService,
  MisCtasInfo,
  CtaAsignacion,
  OperarioResumen,
  OperarioDetalle
} from '../../services/intranet-api.service';
import { SignalrService } from '../../services/signalr.service';
import { IntranetNavbarComponent } from '../../components/intranet-navbar/intranet-navbar.component';

@Component({
  selector: 'app-gestion-equipo',
  standalone: true,
  imports: [CommonModule, FormsModule, IntranetNavbarComponent],
  templateUrl: './gestion-equipo.component.html',
  styleUrl: './gestion-equipo.component.css'
})
export class GestionEquipoComponent implements OnInit {
  userName = '';
  userRole = '';

  misCtasInfo = signal<MisCtasInfo | null>(null);
  ctaSeleccionado = signal<CtaAsignacion | null>(null);
  operarios = signal<OperarioResumen[]>([]);
  operarioDetalle = signal<OperarioDetalle | null>(null);

  loading = signal(true);
  loadingDetalle = signal(false);
  error = signal('');
  errorDetalle = signal('');

  showModal = signal(false);
  confirmDesactivar = signal(false);
  desactivando = signal(false);
  confirmReactivar = signal(false);
  reactivando = signal(false);

  showNotificaciones = signal(false);

  activosCount = computed(() => this.operarios().filter(o => o.activo).length);
  inactivosCount = computed(() => this.operarios().filter(o => !o.activo).length);

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
    this.signalr.conectar();
    this.cargarDatos();
  }

  cargarDatos(): void {
    this.loading.set(true);
    this.error.set('');

    this.intranetApi.obtenerMisCtas().subscribe({
      next: (info) => {
        this.misCtasInfo.set(info);
        if (info.ctas.length > 0) {
          this.seleccionarCta(info.ctas[0]);
        } else {
          this.loading.set(false);
        }
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Error al cargar la información del CTA.');
      }
    });
  }

  seleccionarCta(cta: CtaAsignacion): void {
    this.ctaSeleccionado.set(cta);
    this.loading.set(true);
    this.operarios.set([]);

    this.intranetApi.obtenerOperariosCta(cta.ctaId).subscribe({
      next: (operarios) => {
        this.operarios.set(operarios);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Error al cargar los operarios del CTA.');
      }
    });
  }

  onCtaChange(event: Event): void {
    const ctaId = +(event.target as HTMLSelectElement).value;
    const info = this.misCtasInfo();
    if (!info) return;
    const cta = info.ctas.find(c => c.ctaId === ctaId);
    if (cta) this.seleccionarCta(cta);
  }

  verDetalle(operario: OperarioResumen): void {
    this.operarioDetalle.set(null);
    this.errorDetalle.set('');
    this.confirmDesactivar.set(false);
    this.showModal.set(true);
    this.loadingDetalle.set(true);

    this.intranetApi.obtenerOperarioDetalle(operario.id).subscribe({
      next: (detalle) => {
        this.operarioDetalle.set(detalle);
        this.loadingDetalle.set(false);
      },
      error: () => {
        this.loadingDetalle.set(false);
        this.errorDetalle.set('Error al cargar los detalles del operario.');
      }
    });
  }

  cerrarModal(): void {
    this.showModal.set(false);
    this.operarioDetalle.set(null);
    this.confirmDesactivar.set(false);
    this.confirmReactivar.set(false);
    this.errorDetalle.set('');
  }

  pedirConfirmacionDesactivar(): void {
    this.confirmDesactivar.set(true);
  }

  cancelarConfirmacion(): void {
    this.confirmDesactivar.set(false);
    this.confirmReactivar.set(false);
  }

  desactivarOperario(): void {
    const detalle = this.operarioDetalle();
    if (!detalle) return;
    this.desactivando.set(true);

    this.intranetApi.desactivarOperario(detalle.id).subscribe({
      next: () => {
        this.desactivando.set(false);
        this.cerrarModal();
        const cta = this.ctaSeleccionado();
        if (cta) this.seleccionarCta(cta);
      },
      error: () => {
        this.desactivando.set(false);
        this.errorDetalle.set('No se pudo desactivar el operario. Inténtalo de nuevo.');
      }
    });
  }

  pedirConfirmacionReactivar(): void {
    this.confirmReactivar.set(true);
  }

  reactivarOperario(): void {
    const detalle = this.operarioDetalle();
    if (!detalle) return;
    this.reactivando.set(true);

    this.intranetApi.reactivarOperario(detalle.id).subscribe({
      next: () => {
        this.reactivando.set(false);
        this.cerrarModal();
        const cta = this.ctaSeleccionado();
        if (cta) this.seleccionarCta(cta);
      },
      error: () => {
        this.reactivando.set(false);
        this.errorDetalle.set('No se pudo reactivar el operario. Inténtalo de nuevo.');
      }
    });
  }

  rolLabel(rol: string): string {
    const map: Record<string, string> = {
      OperarioCTA: 'Operario CTA',
      Supervisor: 'Supervisor',
      Admin: 'Administrador'
    };
    return map[rol] ?? rol;
  }

  toggleNotificaciones(): void {
    const nuevo = !this.showNotificaciones();
    this.showNotificaciones.set(nuevo);
    if (nuevo) this.signalr.marcarComoLeidas();
  }

  cerrarNotificaciones(): void {
    this.showNotificaciones.set(false);
  }

  volverDashboard(): void {
    this.router.navigate(['/']);
  }

  logout(): void {
    this.signalr.desconectar();
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
