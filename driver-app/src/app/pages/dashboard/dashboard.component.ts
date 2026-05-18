import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent {
  userName = '';
  userRole = '';
  userRoleLabel = '';

  isJefeReparto = false;

  constructor(private authService: AuthService, private router: Router) {
    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
    this.userRole = user?.rol ?? '';
    this.isJefeReparto = this.authService.isJefeReparto();
    this.userRoleLabel = this.getRoleLabel();
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
