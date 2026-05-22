import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminService, RepartidorAdminDto, EditarRepartidorDto, OficinaJsonResumen } from '../../services/admin.service';

const TIPOS_VEHICULO = ['Bicicleta', 'Moto', 'Furgoneta', 'Camion'];

@Component({
  selector: 'app-gestion-repartidores',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-repartidores.component.html',
  styleUrl: './gestion-repartidores.component.css'
})
export class GestionRepartidoresComponent implements OnInit {
  readonly tiposVehiculo = TIPOS_VEHICULO;

  repartidores = signal<RepartidorAdminDto[]>([]);
  oficinas     = signal<OficinaJsonResumen[]>([]);
  loading      = signal(false);
  error        = signal<string | null>(null);
  actionError  = signal<string | null>(null);

  kpiActivos   = computed(() => this.repartidores().filter(r => r.activo).length);
  kpiInactivos = computed(() => this.repartidores().filter(r => !r.activo).length);
  kpiRutasHoy  = computed(() => this.repartidores().reduce((acc, r) => acc + (r.rutasHoy ?? 0), 0));

  filtroOficina      = signal<number | undefined>(undefined);
  filtroInactivos    = signal(false);
  filtroTexto        = signal('');

  // Edición
  editando        = signal<RepartidorAdminDto | null>(null);
  savingEdicion   = signal(false);
  edicionError    = signal<string | null>(null);
  formEdicion     = signal<EditarRepartidorDto>({
    nombreCompleto: '',
    telefono: '',
    oficinaJsonId: 0,
    oficinaNombre: '',
    tipoVehiculo: 'Furgoneta',
    matriculaVehiculo: ''
  });

  // Lista filtrada en cliente por texto libre
  repartidoresFiltrados = computed(() => {
    const q = this.filtroTexto().trim().toLowerCase();
    if (!q) return this.repartidores();
    return this.repartidores().filter(r =>
      r.nombreCompleto.toLowerCase().includes(q) ||
      r.codigoEmpleado.toLowerCase().includes(q) ||
      r.oficinaNombre.toLowerCase().includes(q)
    );
  });

  constructor(private adminService: AdminService, private router: Router) {}

  ngOnInit(): void {
    this.cargar();
    this.cargarOficinas();
  }

  private cargarOficinas(): void {
    this.adminService.obtenerTodasOficinas().subscribe({
      next: (lista) => {
        const ordenadas = [...lista].sort((a, b) => a.nombre.localeCompare(b.nombre));
        this.oficinas.set(ordenadas);
      },
      error: () => { /* silencioso: el select mostrará vacío */ }
    });
  }

  cambiarOficina(oficinaJsonId: number | string | null): void {
    const id = oficinaJsonId == null ? 0 : Number(oficinaJsonId);
    const oficina = this.oficinas().find(o => o.id === id);
    this.formEdicion.update(f => ({
      ...f,
      oficinaJsonId: id,
      oficinaNombre: oficina?.nombre ?? ''
    }));
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set(null);
    this.actionError.set(null);
    this.adminService.listarRepartidores(this.filtroOficina(), this.filtroInactivos()).subscribe({
      next: (lista) => {
        this.repartidores.set(lista);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar la lista de repartidores.');
        this.loading.set(false);
      }
    });
  }

  toggleInactivos(): void {
    this.filtroInactivos.update(v => !v);
    this.cargar();
  }

  volver(): void {
    this.router.navigate(['/admin']);
  }

  // ─── Edición ───

  iniciarEdicion(r: RepartidorAdminDto): void {
    this.edicionError.set(null);
    this.editando.set(r);
    this.formEdicion.set({
      nombreCompleto: r.nombreCompleto,
      telefono: r.telefono ?? '',
      oficinaJsonId: r.oficinaJsonId,
      oficinaNombre: r.oficinaNombre,
      tipoVehiculo: r.tipoVehiculo,
      matriculaVehiculo: ''
    });
  }

  cancelarEdicion(): void {
    this.editando.set(null);
    this.edicionError.set(null);
  }

  actualizarFormEdicion<K extends keyof EditarRepartidorDto>(campo: K, valor: EditarRepartidorDto[K]): void {
    this.formEdicion.update(f => ({ ...f, [campo]: valor }));
  }

  guardarEdicion(): void {
    const target = this.editando();
    if (!target) return;
    const dto = this.formEdicion();

    if (!dto.nombreCompleto.trim()) {
      this.edicionError.set('El nombre es obligatorio.');
      return;
    }
    if (!dto.oficinaJsonId || dto.oficinaJsonId <= 0) {
      this.edicionError.set('El ID de oficina debe ser válido.');
      return;
    }
    if (!dto.oficinaNombre.trim()) {
      this.edicionError.set('El nombre de oficina es obligatorio.');
      return;
    }

    this.savingEdicion.set(true);
    this.edicionError.set(null);

    this.adminService.editarRepartidor(target.id, {
      ...dto,
      telefono: dto.telefono?.trim() || undefined,
      matriculaVehiculo: dto.matriculaVehiculo?.trim() || undefined
    }).subscribe({
      next: (actualizado) => {
        // Reemplazar en la lista
        this.repartidores.update(list => list.map(r => r.id === actualizado.id ? actualizado : r));
        this.savingEdicion.set(false);
        this.editando.set(null);
      },
      error: (err) => {
        const msg = err.error?.message ?? 'Error al actualizar el repartidor.';
        this.edicionError.set(msg);
        this.savingEdicion.set(false);
      }
    });
  }

  // ─── Desactivar / Reactivar ───

  desactivar(r: RepartidorAdminDto): void {
    if (!confirm(`¿Desactivar al repartidor ${r.nombreCompleto}?\nNo podrá iniciar sesión ni recibir rutas, pero conservará su historial.`)) return;

    this.actionError.set(null);
    this.adminService.desactivarRepartidor(r.id).subscribe({
      next: () => {
        r.activo = false;
        this.repartidores.set([...this.repartidores()]);
      },
      error: (err) => {
        const msg = err.error?.message ?? 'Error al desactivar.';
        this.actionError.set(msg);
      }
    });
  }

  reactivar(r: RepartidorAdminDto): void {
    this.actionError.set(null);
    this.adminService.reactivarRepartidor(r.id).subscribe({
      next: () => {
        r.activo = true;
        this.repartidores.set([...this.repartidores()]);
      },
      error: (err) => {
        const msg = err.error?.message ?? 'Error al reactivar.';
        this.actionError.set(msg);
      }
    });
  }
}
