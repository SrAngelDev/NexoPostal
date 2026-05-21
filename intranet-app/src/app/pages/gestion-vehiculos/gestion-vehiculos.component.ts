import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  AdminVehiculosService,
  VehiculoDto,
  CrearVehiculoDto,
  TipoVehiculo,
  TIPO_VEHICULO_OPTIONS,
  tipoVehiculoLabel
} from '../../services/admin-vehiculos.service';
import { AdminService, RepartidorAdminDto } from '../../services/admin.service';

interface FormState extends CrearVehiculoDto {
  id?: number;
}

const EMPTY_FORM: FormState = {
  matricula: '',
  tipo: TipoVehiculo.Furgoneta,
  marca: '',
  modelo: '',
  color: '',
  anioFabricacion: null,
  oficinaJsonId: null,
  notas: ''
};

@Component({
  selector: 'app-gestion-vehiculos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-vehiculos.component.html',
  styleUrl: './gestion-vehiculos.component.css'
})
export class GestionVehiculosComponent implements OnInit {
  private readonly api = inject(AdminVehiculosService);
  private readonly adminApi = inject(AdminService);
  private readonly router = inject(Router);

  readonly tipoOptions = TIPO_VEHICULO_OPTIONS;
  readonly tipoLabel = tipoVehiculoLabel;

  loading = signal(false);
  saving = signal(false);
  importando = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  incluirInactivos = signal(false);
  filtroTexto = signal('');
  filtroTipo = signal<TipoVehiculo | 'todos'>('todos');

  vehiculos = signal<VehiculoDto[]>([]);
  repartidores = signal<RepartidorAdminDto[]>([]);

  modalAbierto = signal(false);
  modoEdicion = signal(false);
  form = signal<FormState>({ ...EMPTY_FORM });

  modalAsignarAbierto = signal(false);
  vehiculoAsignando = signal<VehiculoDto | null>(null);
  repartidorSeleccionado = signal<number | null>(null);

  vehiculosFiltrados = computed(() => {
    const q = this.filtroTexto().trim().toLowerCase();
    const tipo = this.filtroTipo();
    let lista = this.vehiculos();
    if (tipo !== 'todos') {
      lista = lista.filter(v => v.tipo === tipo);
    }
    if (!q) return lista;
    return lista.filter(v =>
      v.matricula.toLowerCase().includes(q) ||
      (v.marca ?? '').toLowerCase().includes(q) ||
      (v.modelo ?? '').toLowerCase().includes(q) ||
      (v.repartidorAsignadoNombre ?? '').toLowerCase().includes(q)
    );
  });

  totalActivos = computed(() => this.vehiculos().filter(v => v.activo).length);
  totalInactivos = computed(() => this.vehiculos().filter(v => !v.activo).length);
  totalAsignados = computed(() => this.vehiculos().filter(v => v.activo && v.repartidorAsignadoId != null).length);
  totalLibres = computed(() => this.vehiculos().filter(v => v.activo && v.repartidorAsignadoId == null).length);

  repartidoresDisponibles = computed(() => {
    const vehAct = this.vehiculoAsignando();
    return this.repartidores().filter(r => r.activo).sort((a, b) =>
      a.nombreCompleto.localeCompare(b.nombreCompleto)
    );
  });

  ngOnInit(): void {
    this.cargar();
    this.cargarRepartidores();
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listar(this.incluirInactivos()).subscribe({
      next: data => {
        this.vehiculos.set(data);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.mensaje ?? 'Error al cargar vehículos');
        this.loading.set(false);
      }
    });
  }

  cargarRepartidores(): void {
    this.adminApi.listarRepartidores(undefined, false).subscribe({
      next: data => this.repartidores.set(data),
      error: () => { /* silencioso, sólo afecta a la asignación */ }
    });
  }

  toggleInactivos(): void {
    this.incluirInactivos.update(v => !v);
    this.cargar();
  }

  // ───── modal crear/editar ─────
  abrirCrear(): void {
    this.modoEdicion.set(false);
    this.form.set({ ...EMPTY_FORM });
    this.modalAbierto.set(true);
    this.error.set(null);
  }

  abrirEditar(v: VehiculoDto): void {
    this.modoEdicion.set(true);
    this.form.set({
      id: v.id,
      matricula: v.matricula,
      tipo: v.tipo,
      marca: v.marca ?? '',
      modelo: v.modelo ?? '',
      color: v.color ?? '',
      anioFabricacion: v.anioFabricacion ?? null,
      oficinaJsonId: v.oficinaJsonId ?? null,
      notas: v.notas ?? ''
    });
    this.modalAbierto.set(true);
    this.error.set(null);
  }

  cerrarModal(): void {
    this.modalAbierto.set(false);
    this.form.set({ ...EMPTY_FORM });
  }

  guardar(): void {
    const f = this.form();
    if (!f.matricula?.trim()) {
      this.error.set('La matrícula es obligatoria.');
      return;
    }

    const dto: CrearVehiculoDto = {
      matricula: f.matricula.trim().toUpperCase(),
      tipo: Number(f.tipo) as TipoVehiculo,
      marca: f.marca?.trim() || null,
      modelo: f.modelo?.trim() || null,
      color: f.color?.trim() || null,
      anioFabricacion: f.anioFabricacion ?? null,
      oficinaJsonId: f.oficinaJsonId ?? null,
      notas: f.notas?.trim() || null
    };

    this.saving.set(true);
    this.error.set(null);
    const obs = this.modoEdicion() && f.id != null
      ? this.api.actualizar(f.id, dto)
      : this.api.crear(dto);

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.cerrarModal();
        this.success.set(this.modoEdicion() ? 'Vehículo actualizado' : 'Vehículo creado');
        this.cargar();
      },
      error: err => {
        this.error.set(err?.error?.mensaje ?? 'Error al guardar vehículo');
        this.saving.set(false);
      }
    });
  }

  desactivar(v: VehiculoDto): void {
    if (!confirm(`¿Desactivar el vehículo "${v.matricula}"?`)) return;
    this.api.desactivar(v.id).subscribe({
      next: () => {
        this.success.set(`Vehículo "${v.matricula}" desactivado`);
        this.cargar();
      },
      error: err => this.error.set(err?.error?.mensaje ?? 'Error al desactivar')
    });
  }

  reactivar(v: VehiculoDto): void {
    this.api.reactivar(v.id).subscribe({
      next: () => {
        this.success.set(`Vehículo "${v.matricula}" reactivado`);
        this.cargar();
      },
      error: err => this.error.set(err?.error?.mensaje ?? 'Error al reactivar')
    });
  }

  // ───── modal asignar ─────
  abrirAsignar(v: VehiculoDto): void {
    this.vehiculoAsignando.set(v);
    this.repartidorSeleccionado.set(v.repartidorAsignadoId ?? null);
    this.modalAsignarAbierto.set(true);
    this.error.set(null);
  }

  cerrarAsignar(): void {
    this.modalAsignarAbierto.set(false);
    this.vehiculoAsignando.set(null);
    this.repartidorSeleccionado.set(null);
  }

  confirmarAsignacion(): void {
    const v = this.vehiculoAsignando();
    if (!v) return;
    const repId = this.repartidorSeleccionado();
    this.saving.set(true);
    this.api.asignar(v.id, repId === null || repId === undefined ? null : Number(repId)).subscribe({
      next: () => {
        this.saving.set(false);
        this.cerrarAsignar();
        this.success.set(repId ? 'Vehículo asignado correctamente' : 'Vehículo desasignado');
        this.cargar();
      },
      error: err => {
        this.error.set(err?.error?.mensaje ?? 'Error al asignar');
        this.saving.set(false);
      }
    });
  }

  desasignarRapido(v: VehiculoDto): void {
    if (!confirm(`¿Desasignar "${v.matricula}" de ${v.repartidorAsignadoNombre}?`)) return;
    this.api.asignar(v.id, null).subscribe({
      next: () => {
        this.success.set('Vehículo desasignado');
        this.cargar();
      },
      error: err => this.error.set(err?.error?.mensaje ?? 'Error al desasignar')
    });
  }

  // ───── importar ─────
  importar(): void {
    if (!confirm('Esto creará vehículos a partir de los repartidores que ya tienen matrícula. ¿Continuar?')) return;
    this.importando.set(true);
    this.api.importarDesdeRepartidores().subscribe({
      next: res => {
        this.importando.set(false);
        this.success.set(`Importados ${res.importados}, omitidos ${res.omitidos}`);
        this.cargar();
      },
      error: err => {
        this.importando.set(false);
        this.error.set(err?.error?.mensaje ?? 'Error al importar');
      }
    });
  }

  actualizarCampo<K extends keyof FormState>(campo: K, valor: FormState[K]): void {
    this.form.update(f => ({ ...f, [campo]: valor }));
  }

  setFiltroTipo(t: string): void {
    this.filtroTipo.set(t === 'todos' ? 'todos' : (Number(t) as TipoVehiculo));
  }

  volver(): void {
    this.router.navigate(['/admin']);
  }
}
