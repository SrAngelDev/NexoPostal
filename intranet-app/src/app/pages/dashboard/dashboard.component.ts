import { Component, OnInit, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { SignalrService } from '../../services/signalr.service';
import { IntranetNavbarComponent } from '../../components/intranet-navbar/intranet-navbar.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, IntranetNavbarComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  userName = '';
  userRole = '';
  userRoleLabel = '';

  showNotificaciones = signal(false);

  notificaciones = computed(() => this.signalrService.notificaciones());
  noLeidas = computed(() => this.signalrService.notificacionesNoLeidas());
  conectado = computed(() => this.signalrService.conectado());

  // Role checks
  isAdmin = false;
  isOperarioOficina = false;
  isOperarioCTA = false;
  isSupervisor = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    public signalrService: SignalrService
  ) {
    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
    this.userRole = user?.rol ?? '';
    this.isAdmin = this.authService.isAdmin();
    this.isOperarioOficina = this.authService.isOperarioOficina();
    this.isOperarioCTA = this.authService.isOperarioCTA();
    this.isSupervisor = this.authService.isSupervisor();
    this.userRoleLabel = this.getRoleLabel();
  }

  ngOnInit(): void {
    if (this.isAdmin) {
      this.router.navigate(['/admin']);
      return;
    }
  }

  private getRoleLabel(): string {
    switch (this.userRole) {
      case 'OperarioOficina': return 'Operario de Oficina';
      case 'OperarioCTA': return 'Operario CTA';
      case 'Supervisor': return 'Supervisor';
      case 'Admin': return 'Administrador';
      default: return this.userRole;
    }
  }

  toggleNotificaciones(): void {
    const abrir = !this.showNotificaciones();
    this.showNotificaciones.set(abrir);
    // Al abrir el panel, todas las notificaciones se consideran leídas.
    if (abrir) this.signalrService.marcarComoLeidas();
  }

  limpiarNotificaciones(): void {
    this.signalrService.limpiarNotificaciones();
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
