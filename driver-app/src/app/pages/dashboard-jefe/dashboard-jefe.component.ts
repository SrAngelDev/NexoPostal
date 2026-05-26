import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { SignalrService } from '../../services/signalr.service';
import { DriverNavbarComponent } from '../../components/driver-navbar/driver-navbar.component';

interface DashboardReparto {
  rutasHoy: number;
  rutasEnCurso: number;
  entregasPendientes: number;
  entregasCompletadas: number;
  entregasFallidas: number;
  repartidoresActivos: number;
  tasaEntregaExitosa: number;
}

@Component({
  selector: 'app-dashboard-jefe',
  standalone: true,
  imports: [CommonModule, DriverNavbarComponent],
  templateUrl: './dashboard-jefe.component.html',
  styleUrl: './dashboard-jefe.component.css'
})
export class DashboardJefeComponent implements OnInit {
  stats = signal<DashboardReparto | null>(null);
  cargando = signal(false);
  error = signal<string | null>(null);
  userName = '';

  private readonly API = '/api/nexopostal/reparto';

  constructor(
    private http: HttpClient,
    private router: Router,
    private authService: AuthService,
    private signalr: SignalrService
  ) {
    this.userName = this.authService.getCurrentUser()?.user ?? '';
  }

  ngOnInit(): void {
    this.cargarDashboard();
  }

  cargarDashboard(): void {
    this.cargando.set(true);
    this.error.set(null);

    this.http.get<DashboardReparto>(`${this.API}/dashboard`).subscribe({
      next: (data) => {
        this.stats.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.error.set('No se pudieron cargar las métricas.');
        this.cargando.set(false);
        console.error('Error cargando dashboard:', err);
      }
    });
  }

  porcentajeEntregas(): number {
    const s = this.stats();
    if (!s) return 0;
    return s.tasaEntregaExitosa;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  ir(ruta: string): void {
    this.router.navigate([ruta]);
  }
}
