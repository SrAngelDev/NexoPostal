import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { SignalrService } from '../../services/signalr.service';
import { NotificationBellComponent } from '../../components/notification-bell/notification-bell.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, NotificationBellComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  userName = '';
  userRole = '';
  userRoleLabel = '';

  constructor(
    private authService: AuthService,
    private router: Router,
    private signalr: SignalrService
  ) {
    // El JefeReparto no opera entregas: lo enviamos a su panel.
    if (this.authService.isJefeReparto()) {
      this.router.navigate(['/dashboard-jefe']);
      return;
    }

    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
    this.userRole = user?.rol ?? '';
    this.userRoleLabel = this.getRoleLabel();
  }

  ngOnInit(): void {
    if (this.authService.getToken()) {
      this.signalr.iniciar();
    }
  }

  private getRoleLabel(): string {
    switch (this.userRole) {
      case 'Repartidor': return 'Repartidor';
      case 'JefeReparto': return 'Jefe de Reparto';
      default: return this.userRole;
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
