import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  RepartoService,
  RepartidorPerfil,
  EditarRepartidorRequest,
  VehiculoFlota
} from '../../services/reparto.service';
import { AuthService } from '../../services/auth.service';
import { DriverNavbarComponent } from '../../components/driver-navbar/driver-navbar.component';

@Component({
  selector: 'app-mis-repartidores',
  standalone: true,
  imports: [CommonModule, FormsModule, DriverNavbarComponent],
  templateUrl: './mis-repartidores.component.html',
  styleUrl: './mis-repartidores.component.css'
})
export class MisRepartidoresComponent implements OnInit {
  repartidores = signal<RepartidorPerfil[]>([]);
  cargando = signal(false);
  procesando = signal(false);
  error = signal<string | null>(null);
  mensaje = signal<string | null>(null);

  // Filtros
  incluirInactivos = signal(false);
  busqueda = signal('');

  // Modal edición
  mostrarModal = signal(false);
  enEdicion = signal<RepartidorPerfil | null>(null);
  form = signal<EditarRepartidorRequest>({
    nombreCompleto: '',
    telefono: '',
    oficinaJsonId: 0,
    oficinaNombre: '',
    tipoVehiculo: 'Furgoneta',
    matriculaVehiculo: ''
  });

  // Flota de vehículos para el selector
  vehiculosFlota = signal<VehiculoFlota[]>([]);
  cargandoFlota = signal(false);
  vehiculoSeleccionadoId = signal<number | null>(null);

  userName = '';

  readonly filtrados = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const lista = this.repartidores();
    if (!q) return lista;
    return lista.filter(r =>
      r.nombreCompleto.toLowerCase().includes(q) ||
      r.codigoEmpleado.toLowerCase().includes(q) ||
      (r.telefono ?? '').toLowerCase().includes(q)
    );
  });

  readonly totalActivos = computed(() => this.repartidores().filter(r => r.activo).length);
  readonly totalInactivos = computed(() => this.repartidores().filter(r => !r.activo).length);

  constructor(
    private repartoService: RepartoService,
    private authService: AuthService,
    private router: Router
  ) {
    this.userName = this.authService.getCurrentUser()?.user ?? '';
  }

  ngOnInit(): void {
    this.cargar();
  }

  volver(): void {
    this.router.navigate(['/dashboard-jefe']);
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.repartoService.listarMisRepartidores(this.incluirInactivos()).subscribe({
      next: (data) => {
        this.repartidores.set(data ?? []);
        this.cargando.set(false);
      },
      error: (err) => {
        console.error('Error cargando repartidores:', err);
        this.error.set('No se pudo cargar la lista de repartidores.');
        this.cargando.set(false);
      }
    });
  }

  toggleInactivos(): void {
    this.incluirInactivos.set(!this.incluirInactivos());
    this.cargar();
  }

  esJefe(r: RepartidorPerfil): boolean {
    return (r.rol ?? '').toLowerCase() === 'jefereparto';
  }

  // ─── Modal edición ───

  abrirEdicion(r: RepartidorPerfil): void {
    this.enEdicion.set(r);
    this.form.set({
      nombreCompleto: r.nombreCompleto,
      telefono: r.telefono ?? '',
      oficinaJsonId: r.oficinaJsonId,
      oficinaNombre: r.oficinaNombre,
      tipoVehiculo: r.tipoVehiculo || 'Furgoneta',
      matriculaVehiculo: r.matriculaVehiculo ?? ''
    });
    this.mensaje.set(null);
    this.error.set(null);
    // Intentar preseleccionar el vehículo actual en la flota
    this.vehiculoSeleccionadoId.set(null);
    this.mostrarModal.set(true);
    // Cargar flota al abrir el modal
    this.cargandoFlota.set(true);
    this.repartoService.listarVehiculosFlota().subscribe({
      next: (lista) => {
        this.vehiculosFlota.set(lista);
        // Preseleccionar si la matrícula actual coincide con algún vehículo de la flota
        const match = lista.find(v => v.matricula === r.matriculaVehiculo);
        this.vehiculoSeleccionadoId.set(match?.id ?? null);
        this.cargandoFlota.set(false);
      },
      error: () => {
        this.vehiculosFlota.set([]);
        this.cargandoFlota.set(false);
      }
    });
  }

  cerrarModal(): void {
    if (this.procesando()) return;
    this.mostrarModal.set(false);
    this.enEdicion.set(null);
    this.vehiculosFlota.set([]);
    this.vehiculoSeleccionadoId.set(null);
  }

  onVehiculoSeleccionado(idStr: string): void {
    const id = idStr ? +idStr : null;
    this.vehiculoSeleccionadoId.set(id);
    if (!id) {
      // "Sin vehículo"
      this.form.update(f => ({ ...f, tipoVehiculo: 'Furgoneta', matriculaVehiculo: '' }));
      return;
    }
    const v = this.vehiculosFlota().find(x => x.id === id);
    if (v) {
      this.form.update(f => ({ ...f, tipoVehiculo: v.tipo, matriculaVehiculo: v.matricula }));
    }
  }

  actualizarCampo<K extends keyof EditarRepartidorRequest>(key: K, value: EditarRepartidorRequest[K]): void {
    this.form.update(f => ({ ...f, [key]: value }));
  }

  guardarEdicion(): void {
    const target = this.enEdicion();
    if (!target) return;
    const dto = this.form();

    if (!dto.nombreCompleto.trim()) {
      this.error.set('El nombre es obligatorio.');
      return;
    }

    // Incluir vehiculoId si hay flota disponible:
    // - vehículo seleccionado (>0) para asignar
    // - null si no se cargó flota (no tocar asignación)
    const vehiculoId = this.vehiculosFlota().length > 0
      ? (this.vehiculoSeleccionadoId() ?? 0)   // 0 = desasignar
      : undefined;                              // undefined = no cambiar

    const request: EditarRepartidorRequest = { ...dto, vehiculoId };

    this.procesando.set(true);
    this.error.set(null);
    this.repartoService.editarRepartidor(target.id, request).subscribe({
      next: (actualizado) => {
        this.repartidores.update(list =>
          list.map(r => r.id === actualizado.id ? actualizado : r)
        );
        this.procesando.set(false);
        this.mostrarModal.set(false);
        this.enEdicion.set(null);
        this.mensaje.set('Cambios guardados.');
        setTimeout(() => this.mensaje.set(null), 3000);
      },
      error: (err) => {
        console.error('Error editando repartidor:', err);
        this.error.set(err?.error?.message ?? 'No se pudo guardar la edición.');
        this.procesando.set(false);
      }
    });
  }

  // ─── Activar / desactivar ───

  desactivar(r: RepartidorPerfil): void {
    if (!confirm(`¿Desactivar al repartidor "${r.nombreCompleto}"?\nNo se podrá hacer si tiene rutas en curso o planificadas.`)) {
      return;
    }
    this.procesando.set(true);
    this.repartoService.desactivarRepartidor(r.id).subscribe({
      next: () => {
        this.procesando.set(false);
        this.mensaje.set(`Repartidor "${r.nombreCompleto}" desactivado.`);
        setTimeout(() => this.mensaje.set(null), 3000);
        this.cargar();
      },
      error: (err) => {
        console.error('Error desactivando:', err);
        this.error.set(err?.error?.message ?? 'No se pudo desactivar el repartidor.');
        this.procesando.set(false);
      }
    });
  }

  reactivar(r: RepartidorPerfil): void {
    this.procesando.set(true);
    this.repartoService.reactivarRepartidor(r.id).subscribe({
      next: () => {
        this.procesando.set(false);
        this.mensaje.set(`Repartidor "${r.nombreCompleto}" reactivado.`);
        setTimeout(() => this.mensaje.set(null), 3000);
        this.cargar();
      },
      error: (err) => {
        console.error('Error reactivando:', err);
        this.error.set(err?.error?.message ?? 'No se pudo reactivar el repartidor.');
        this.procesando.set(false);
      }
    });
  }
}
