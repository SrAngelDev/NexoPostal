import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

interface DashboardReparto {
  totalRepartidores: number;
  rutasPlanificadas: number;
  rutasEnCurso: number;
  rutasCompletadas: number;
  totalEntregas: number;
  entregasRealizadas: number;
  entregasPendientes: number;
  entregasIncidencia: number;
}

@Component({
  selector: 'app-dashboard-jefe',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard-jefe.component.html',
  styleUrl: './dashboard-jefe.component.css'
})
export class DashboardJefeComponent implements OnInit {
  stats = signal<DashboardReparto | null>(null);
  cargando = signal(false);
  error = signal<string | null>(null);

  private readonly API = '/api/reparto';

  constructor(private http: HttpClient, private router: Router) {}

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
    if (!s || s.totalEntregas === 0) return 0;
    return Math.round((s.entregasRealizadas / s.totalEntregas) * 100);
  }

  volver(): void {
    this.router.navigate(['/']);
  }
}
