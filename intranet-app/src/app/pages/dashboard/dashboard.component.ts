import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { SignalrService } from '../../services/signalr.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, OnDestroy {
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
  isOperarioLogistico = false;
  isOperarioJefe = false;

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
    this.isOperarioLogistico = this.authService.isOperarioLogistico();
    this.isOperarioJefe = this.authService.isOperarioJefe();
    this.userRoleLabel = this.getRoleLabel();
  }

  ngOnInit(): void {
    this.signalrService.conectar();
    if (this.isAdmin) {
      this.router.navigate(['/admin']);
      return;
    }
  }

  private getRoleLabel(): string {
    switch (this.userRole) {
      case 'OperarioOficina': return 'Operario de Oficina';
      case 'OperarioLogistico': return 'Operario Logístico';
      case 'OperarioJefe': return 'Operario Jefe';
      case 'Admin': return 'Administrador';
      default: return this.userRole;
    }
  }

  ngOnDestroy(): void {
    this.signalrService.desconectar();
  }

  toggleNotificaciones(): void {
    this.showNotificaciones.update(v => !v);
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
