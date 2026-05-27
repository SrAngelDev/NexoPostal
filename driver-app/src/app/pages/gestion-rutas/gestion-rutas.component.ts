import { Component, OnInit, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { RepartoService, RutaResumen } from '../../services/reparto.service';
import { DriverNavbarComponent } from '../../components/driver-navbar/driver-navbar.component';

@Component({
  selector: 'app-gestion-rutas',
  standalone: true,
  imports: [CommonModule, FormsModule, DriverNavbarComponent],
  templateUrl: './gestion-rutas.component.html',
  styleUrl: './gestion-rutas.component.css'
})
export class GestionRutasComponent implements OnInit {
  rutas = signal<RutaResumen[]>([]);
  cargando = signal(false);
  error = signal<string | null>(null);

  // Filtros
  fechaFiltro = signal<string>(new Date().toISOString().split('T')[0]);
  estadoFiltro = signal<string>('todas');

  // Modal de acción
  rutaAccion = signal<RutaResumen | null>(null);
  tipoAccion = signal<'cancelar' | 'reactivar' | null>(null);
  procesando = signal(false);
  errorAccion = signal<string | null>(null);

  rutasFiltradas = computed(() => {
    const estado = this.estadoFiltro();
    if (estado === 'todas') return this.rutas();
    return this.rutas().filter(r => r.estado.toLowerCase() === estado.toLowerCase());
  });

  constructor(
    private repartoService: RepartoService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.cargarRutas();
  }

  cargarRutas(): void {
    this.cargando.set(true);
    this.error.set(null);

    const fecha = this.fechaFiltro();
    this.repartoService.obtenerRutas(fecha || undefined).subscribe({
      next: (rutas) => {
        this.rutas.set(rutas);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar las rutas. Inténtalo de nuevo.');
        this.cargando.set(false);
      }
    });
  }

  onFechaChange(): void {
    this.cargarRutas();
  }

  verDetalle(id: number): void {
    this.router.navigate(['/detalle-ruta', id]);
  }

  abrirAccion(ruta: RutaResumen, accion: 'cancelar' | 'reactivar', event: MouseEvent): void {
    event.stopPropagation();
    this.rutaAccion.set(ruta);
    this.tipoAccion.set(accion);
    this.errorAccion.set(null);
  }

  cerrarModal(): void {
    this.rutaAccion.set(null);
    this.tipoAccion.set(null);
    this.errorAccion.set(null);
  }

  confirmarAccion(): void {
    const ruta = this.rutaAccion();
    const accion = this.tipoAccion();
    if (!ruta || !accion) return;

    this.procesando.set(true);
    this.errorAccion.set(null);

    const obs = accion === 'cancelar'
      ? this.repartoService.cancelarRuta(ruta.id)
      : this.repartoService.reactivarRuta(ruta.id);

    obs.subscribe({
      next: () => {
        this.procesando.set(false);
        this.cerrarModal();
        this.cargarRutas();
      },
      error: (err) => {
        this.procesando.set(false);
        const msg = err?.error?.message ?? (accion === 'cancelar'
          ? 'No se pudo cancelar la ruta.'
          : 'No se pudo reactivar la ruta.');
        this.errorAccion.set(msg);
      }
    });
  }

  puedeCancel(ruta: RutaResumen): boolean {
    return ruta.estado === 'Planificada';
  }

  puedeReactivar(ruta: RutaResumen): boolean {
    return ruta.estado === 'Cancelada';
  }

  volver(): void {
    this.router.navigate(['/']);
  }

  getEstadoClass(estado: string): string {
    switch (estado?.toLowerCase()) {
      case 'planificada': return 'estado-planificada';
      case 'encurso': return 'estado-en-curso';
      case 'completada': return 'estado-completada';
      case 'completadaparcial': return 'estado-completada-parcial';
      case 'cancelada': return 'estado-cancelada';
      default: return '';
    }
  }

  getEstadoLabel(estado: string): string {
    switch (estado?.toLowerCase()) {
      case 'encurso': return 'En curso';
      case 'completadaparcial': return 'Completada parcial';
      default: return estado;
    }
  }
}
