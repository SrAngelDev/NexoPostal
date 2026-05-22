import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminOficinasService, OficinaPostalAdminDto, CrearOficinaPostalDto } from '../../services/admin-oficinas.service';

interface FormState extends CrearOficinaPostalDto {
  id?: number;
}

const EMPTY_FORM: FormState = {
  nombre: '',
  direccion: '',
  codigoPostal: '',
  ciudad: '',
  provincia: '',
  telefono: '',
  horario: 'Lu-Vi: 09:00-14:00',
  servicios: '',
  latitud: null,
  longitud: null
};

@Component({
  selector: 'app-gestion-oficinas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-oficinas.component.html',
  styleUrl: './gestion-oficinas.component.css'
})
export class GestionOficinasComponent implements OnInit {
  private readonly api = inject(AdminOficinasService);
  private readonly router = inject(Router);

  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  incluirInactivas = signal(false);
  filtroTexto = signal('');

  oficinas = signal<OficinaPostalAdminDto[]>([]);

  modalAbierto = signal(false);
  modoEdicion = signal(false);
  form = signal<FormState>({ ...EMPTY_FORM });

  oficinasFiltradas = computed(() => {
    const q = this.filtroTexto().trim().toLowerCase();
    const todas = this.oficinas();
    if (!q) return todas;
    return todas.filter(o =>
      o.nombre.toLowerCase().includes(q) ||
      o.direccion.toLowerCase().includes(q) ||
      o.codigoPostal.includes(q) ||
      o.ciudad.toLowerCase().includes(q) ||
      (o.provincia ?? '').toLowerCase().includes(q)
    );
  });

  totalActivas = computed(() => this.oficinas().filter(o => o.activo).length);
  totalInactivas = computed(() => this.oficinas().filter(o => !o.activo).length);

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listar(this.incluirInactivas()).subscribe({
      next: data => {
        this.oficinas.set(data);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.mensaje ?? err?.error?.error ?? 'Error al cargar oficinas');
        this.loading.set(false);
      }
    });
  }

  toggleInactivas(): void {
    this.incluirInactivas.update(v => !v);
    this.cargar();
  }

  // ───── modal ─────
  abrirCrear(): void {
    this.modoEdicion.set(false);
    this.form.set({ ...EMPTY_FORM });
    this.modalAbierto.set(true);
    this.error.set(null);
  }

  abrirEditar(o: OficinaPostalAdminDto): void {
    this.modoEdicion.set(true);
    this.form.set({
      id: o.id,
      nombre: o.nombre,
      direccion: o.direccion,
      codigoPostal: o.codigoPostal,
      ciudad: o.ciudad,
      provincia: o.provincia ?? '',
      telefono: o.telefono ?? '',
      horario: o.horario ?? '',
      servicios: o.servicios ?? '',
      latitud: o.latitud ?? null,
      longitud: o.longitud ?? null
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
    if (!f.nombre?.trim() || !f.direccion?.trim() || !f.codigoPostal?.trim() || !f.ciudad?.trim()) {
      this.error.set('Nombre, dirección, código postal y ciudad son obligatorios.');
      return;
    }

    const dto: CrearOficinaPostalDto = {
      nombre: f.nombre.trim(),
      direccion: f.direccion.trim(),
      codigoPostal: f.codigoPostal.trim(),
      ciudad: f.ciudad.trim(),
      provincia: f.provincia?.trim() || null,
      telefono: f.telefono?.trim() || null,
      horario: f.horario?.trim() || null,
      servicios: f.servicios?.trim() || null,
      latitud: f.latitud,
      longitud: f.longitud
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
        this.success.set(this.modoEdicion() ? 'Oficina actualizada' : 'Oficina creada');
        this.cargar();
      },
      error: err => {
        this.error.set(err?.error?.mensaje ?? 'Error al guardar oficina');
        this.saving.set(false);
      }
    });
  }

  desactivar(o: OficinaPostalAdminDto): void {
    if (!confirm(`¿Desactivar la oficina "${o.nombre}"? Quedará oculta para repartidores y ciudadanos.`)) return;
    this.api.desactivar(o.id).subscribe({
      next: () => {
        this.success.set(`Oficina "${o.nombre}" desactivada`);
        this.cargar();
      },
      error: err => this.error.set(err?.error?.mensaje ?? 'Error al desactivar')
    });
  }

  reactivar(o: OficinaPostalAdminDto): void {
    this.api.reactivar(o.id).subscribe({
      next: () => {
        this.success.set(`Oficina "${o.nombre}" reactivada`);
        this.cargar();
      },
      error: err => this.error.set(err?.error?.mensaje ?? 'Error al reactivar')
    });
  }

  actualizarCampo<K extends keyof FormState>(campo: K, valor: FormState[K]): void {
    this.form.update(f => ({ ...f, [campo]: valor }));
  }

  volver(): void {
    this.router.navigate(['/admin']);
  }
}
