import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { AdminService, DashboardAdminDto, DashboardCtaDto } from '../../services/admin.service';
import { SignalrService } from '../../services/signalr.service';
import { IntranetNavbarComponent } from '../../components/intranet-navbar/intranet-navbar.component';

@Component({
  selector: 'app-admin-panel',
  standalone: true,
  imports: [CommonModule, IntranetNavbarComponent],
  templateUrl: './admin-panel.component.html',
  styleUrl: './admin-panel.component.css'
})
export class AdminPanelComponent implements OnInit, OnDestroy {
  userName = '';
  loading = signal(true);
  error = signal('');
  dashboard = signal<DashboardAdminDto | null>(null);
  selectedCta = signal<DashboardCtaDto | null>(null);

  showNotificaciones = signal(false);
  notificaciones = computed(() => this.signalrService.notificaciones());
  noLeidas = computed(() => this.signalrService.notificacionesNoLeidas());
  conectado = computed(() => this.signalrService.conectado());

  // Computed stats
  totalTareasActivas = computed(() => {
    const d = this.dashboard();
    if (!d) return 0;
    return d.tareasPendientesGlobal + d.tareasEnProgresoGlobal;
  });

  porcentajeOperariosActivos = computed(() => {
    const d = this.dashboard();
    if (!d || d.totalOperarios === 0) return 0;
    return Math.round((d.operariosActivos / d.totalOperarios) * 100);
  });

  totalIncidencias = computed(() => {
    const d = this.dashboard();
    if (!d) return 0;
    return d.incidenciasAbiertasGlobal + d.incidenciasEnRevisionGlobal;
  });

  private refreshInterval: any;

  constructor(
    private authService: AuthService,
    private adminService: AdminService,
    private router: Router,
    public signalrService: SignalrService
  ) {
    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
  }

  ngOnInit(): void {
    this.signalrService.conectar();
    this.cargarDashboard();
    // Auto-refresh cada 30 segundos
    this.refreshInterval = setInterval(() => this.cargarDashboard(), 30000);
  }

  ngOnDestroy(): void {
    this.signalrService.desconectar();
    if (this.refreshInterval) clearInterval(this.refreshInterval);
  }

  cargarDashboard(): void {
    this.adminService.obtenerDashboardGlobal().subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.loading.set(false);
        this.error.set('');
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set('Error al cargar las estadísticas. Verifica la conexión.');
        console.error('Error cargando dashboard admin:', err);
      }
    });
  }

  seleccionarCta(cta: DashboardCtaDto): void {
    this.selectedCta.set(this.selectedCta()?.ctaId === cta.ctaId ? null : cta);
  }

  toggleNotificaciones(): void {
    this.showNotificaciones.update(v => !v);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
