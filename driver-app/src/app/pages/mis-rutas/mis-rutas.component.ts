import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DriverNavbarComponent } from '../../components/driver-navbar/driver-navbar.component';
import { RepartoService, RutaRepartoDetalle } from '../../services/reparto.service';

@Component({
  selector: 'app-mis-rutas',
  standalone: true,
  imports: [CommonModule, DriverNavbarComponent],
  templateUrl: './mis-rutas.component.html',
  styleUrl: './mis-rutas.component.css'
})
export class MisRutasComponent implements OnInit {
  cargando = signal(false);
  error = signal('');
  rutas = signal<RutaRepartoDetalle[]>([]);

  readonly rutasOrdenadas = computed(() => {
    const prioridad: Record<string, number> = {
      EnCurso: 0,
      Planificada: 1,
      CompletadaParcial: 2,
      Completada: 3,
      Cancelada: 4
    };
    return [...this.rutas()].sort((a, b) => {
      const pa = prioridad[a.estado] ?? 99;
      const pb = prioridad[b.estado] ?? 99;
      if (pa !== pb) return pa - pb;
      return a.codigo.localeCompare(b.codigo);
    });
  });

  constructor(private router: Router, private repartoService: RepartoService) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set('');
    this.repartoService.obtenerMiRuta().subscribe({
      next: (rutas) => {
        this.rutas.set(rutas ?? []);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        this.error.set(err.error?.message || 'No se pudieron cargar tus rutas asignadas.');
      }
    });
  }

  abrirRuta(id: number): void {
    this.router.navigate(['/ruta', id]);
  }

  volverDashboard(): void {
    this.router.navigate(['/']);
  }

  irEscaneo(): void {
    this.router.navigate(['/escaneo']);
  }

  resumenEntregas(ruta: RutaRepartoDetalle): { total: number; entregadas: number; pendientes: number; fallidas: number } {
    const entregas = ruta.entregas ?? [];
    const total = entregas.length;
    const entregadas = entregas.filter(e => e.estado === 'Entregado' || e.estado === 'EntregadoPuntoAlternativo').length;
    const fallidas = entregas.filter(e => e.estado === 'Ausente' || e.estado === 'DireccionIncorrecta' || e.estado === 'Rechazado').length;
    const pendientes = total - entregadas - fallidas;
    return { total, entregadas, pendientes, fallidas };
  }

  getEstadoClass(estado: string): string {
    switch (estado) {
      case 'Planificada': return 'estado-planificada';
      case 'EnCurso': return 'estado-en-curso';
      case 'Completada': return 'estado-completada';
      case 'CompletadaParcial': return 'estado-completada-parcial';
      case 'Cancelada': return 'estado-cancelada';
      default: return '';
    }
  }

  getEstadoLabel(estado: string): string {
    switch (estado) {
      case 'Planificada': return 'Planificada';
      case 'EnCurso': return 'En curso';
      case 'Completada': return 'Completada';
      case 'CompletadaParcial': return 'Completada parcial';
      case 'Cancelada': return 'Cancelada';
      default: return estado;
    }
  }
}
