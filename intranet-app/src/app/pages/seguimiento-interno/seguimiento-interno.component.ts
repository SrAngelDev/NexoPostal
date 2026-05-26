import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import {
  EnviosInternoService,
  EnvioInternoDetallado,
  EnvioResumenInterno,
  ActualizarEstadoInternoRequest,
  ESTADOS_INTERNOS
} from '../../services/envios-interno.service';
import { BarcodeScannerComponent } from '../../components/barcode-scanner/barcode-scanner.component';
import { IntranetNavbarComponent } from '../../components/intranet-navbar/intranet-navbar.component';

@Component({
  selector: 'app-seguimiento-interno',
  standalone: true,
  imports: [CommonModule, FormsModule, BarcodeScannerComponent, IntranetNavbarComponent],
  templateUrl: './seguimiento-interno.component.html',
  styleUrl: './seguimiento-interno.component.css'
})
export class SeguimientoInternoComponent {
  // ─── Usuario ───
  userName = '';
  userRole = '';

  // ─── Búsqueda ───
  searchQuery = '';
  searchLoading = signal(false);
  searchError = signal('');

  // ─── Detalle ───
  envioDetalle = signal<EnvioInternoDetallado | null>(null);

  // ─── Listado ───
  envios = signal<EnvioResumenInterno[]>([]);
  listadoLoading = signal(false);

  // ─── Filtros ───
  filtroEstado = '';
  filtroCP = '';

  // ─── Actualizar estado ───
  showEstadoModal = signal(false);
  nuevoEstado = '';
  observaciones = '';
  actualizandoEstado = signal(false);
  estadoError = signal('');

  // ─── Escáner de cámara ───
  mostrarScanner = signal(false);

  // ─── Pestaña activa ───
  tabActiva = signal<'buscar' | 'listar'>('buscar');

  // ─── Estados agrupados ───
  estadosInternos = ESTADOS_INTERNOS;
  gruposEstados: { grupo: string; estados: typeof ESTADOS_INTERNOS }[] = [];

  constructor(
    private authService: AuthService,
    private enviosService: EnviosInternoService,
    private router: Router
  ) {
    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
    this.userRole = user?.rol ?? '';

    // Agrupar estados para el selector
    const grupos = new Map<string, typeof ESTADOS_INTERNOS>();
    for (const estado of ESTADOS_INTERNOS) {
      if (!grupos.has(estado.grupo)) {
        grupos.set(estado.grupo, []);
      }
      grupos.get(estado.grupo)!.push(estado);
    }
    this.gruposEstados = Array.from(grupos.entries()).map(([grupo, estados]) => ({ grupo, estados }));
  }

  // ─── Navegación ───
  volverDashboard(): void {
    this.router.navigate(['/']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  cambiarTab(tab: 'buscar' | 'listar'): void {
    this.tabActiva.set(tab);
    this.searchError.set('');
    this.estadoError.set('');
  }

  // ─── Búsqueda ───
  buscar(): void {
    const q = this.searchQuery.trim();
    if (!q) {
      this.searchError.set('Introduce un número de expedición o seguimiento');
      return;
    }

    this.searchLoading.set(true);
    this.searchError.set('');
    this.envioDetalle.set(null);

    // Detectar el tipo de código
    const esExpedicion = q.toUpperCase().startsWith('NXI-');

    const observable = esExpedicion
      ? this.enviosService.obtenerPorExpedicion(q)
      : this.enviosService.obtenerPorSeguimiento(q);

    observable.subscribe({
      next: (envio) => {
        this.envioDetalle.set(envio);
        this.searchLoading.set(false);
      },
      error: (err) => {
        this.searchLoading.set(false);
        if (err.status === 404) {
          this.searchError.set('No se encontró ningún envío con ese código');
        } else {
          this.searchError.set('Error al buscar el envío. Inténtalo de nuevo.');
        }
      }
    });
  }

  toggleScanner(): void {
    this.mostrarScanner.update(v => !v);
    if (!this.mostrarScanner()) return;
    // Limpiar estado anterior al abrir
    this.searchError.set('');
    this.envioDetalle.set(null);
  }

  onCodigoEscaneado(codigo: string): void {
    this.searchQuery = codigo;
    this.mostrarScanner.set(false);
    this.buscar();
  }

  limpiarBusqueda(): void {
    this.searchQuery = '';
    this.envioDetalle.set(null);
    this.searchError.set('');
  }

  // ─── Listado ───
  cargarListado(): void {
    this.listadoLoading.set(true);
    const filtros: { estadoInterno?: string; codigoPostal?: string } = {};
    if (this.filtroEstado) filtros.estadoInterno = this.filtroEstado;
    if (this.filtroCP.trim()) filtros.codigoPostal = this.filtroCP.trim();

    this.enviosService.listarEnvios(filtros).subscribe({
      next: (envios) => {
        this.envios.set(envios);
        this.listadoLoading.set(false);
      },
      error: () => {
        this.envios.set([]);
        this.listadoLoading.set(false);
      }
    });
  }

  verDetalle(expedicion: string): void {
    this.searchQuery = expedicion;
    this.tabActiva.set('buscar');
    this.buscar();
  }

  // ─── Actualizar estado ───
  abrirModalEstado(): void {
    this.nuevoEstado = '';
    this.observaciones = '';
    this.estadoError.set('');
    this.showEstadoModal.set(true);
  }

  cerrarModalEstado(): void {
    this.showEstadoModal.set(false);
  }

  confirmarCambioEstado(): void {
    const detalle = this.envioDetalle();
    if (!detalle || !this.nuevoEstado) return;

    this.actualizandoEstado.set(true);
    this.estadoError.set('');

    const request: ActualizarEstadoInternoRequest = {
      nuevoEstadoInterno: this.nuevoEstado,
      observaciones: this.observaciones || undefined
    };

    this.enviosService.actualizarEstado(detalle.numeroExpedicion, request).subscribe({
      next: (envioActualizado) => {
        this.envioDetalle.set(envioActualizado);
        this.actualizandoEstado.set(false);
        this.showEstadoModal.set(false);
      },
      error: (err) => {
        this.actualizandoEstado.set(false);
        this.estadoError.set(err.error?.error || 'Error al actualizar el estado');
      }
    });
  }

  // ─── Helpers de presentación ───
  formatearEstado(estado: string): string {
    return this.enviosService.formatearEstadoInterno(estado);
  }

  getEstadoClase(estado: string): string {
    return this.enviosService.getEstadoClase(estado);
  }

  formatearFecha(fecha: string): string {
    if (!fecha) return '—';
    return new Date(fecha).toLocaleDateString('es-ES', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatearMoneda(valor: number): string {
    return valor.toFixed(2) + ' €';
  }
}
