import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  AdminService,
  CtaResumenDto,
  CrearCtaDto,
  EditarCtaDto
} from '../../services/admin.service';

const AREAS_ZONALES = ['Noroeste', 'Norte', 'Noreste', 'Centro', 'Este', 'Sur', 'Insular'];

@Component({
  selector: 'app-gestion-ctas-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-ctas-admin.component.html',
  styleUrl: './gestion-ctas-admin.component.css'
})
export class GestionCtasAdminComponent implements OnInit {
  readonly AREAS = AREAS_ZONALES;

  ctas = signal<CtaResumenDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  actionError = signal<string | null>(null);
  filtroTexto = signal('');
  incluirInactivos = signal(true);

  kpiActivos        = computed(() => this.ctas().filter(c => c.activo !== false).length);
  kpiInactivos      = computed(() => this.ctas().filter(c => c.activo === false).length);
  kpiNodosAereos    = computed(() => this.ctas().filter(c => c.esNodoAereo).length);
  kpiNodosMaritimos = computed(() => this.ctas().filter(c => c.esNodoMaritimo).length);

  modoModal = signal<'crear' | 'editar' | null>(null);
  ctaEditando = signal<CtaResumenDto | null>(null);
  saving = signal(false);
  modalError = signal<string | null>(null);
  form = signal<CrearCtaDto>(this.formVacio());

  ctasFiltrados = computed(() => {
    const filtro = this.filtroTexto().toLowerCase().trim();
    const items = this.ctas();
    return items.filter(c => {
      if (!this.incluirInactivos() && c.activo === false) return false;
      if (!filtro) return true;
      return c.codigo.toLowerCase().includes(filtro)
          || c.nombre.toLowerCase().includes(filtro)
          || (c.ciudad ?? '').toLowerCase().includes(filtro)
          || (c.provincia ?? '').toLowerCase().includes(filtro);
    });
  });

  constructor(private admin: AdminService, private router: Router) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set(null);
    this.admin.listarCtasAdmin().subscribe({
      next: (lista) => {
        this.ctas.set(lista);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar la lista de CTAs.');
        this.loading.set(false);
      }
    });
  }

  volver(): void {
    this.router.navigate(['/admin']);
  }

  // ─── Crear ───
  abrirCrear(): void {
    this.form.set(this.formVacio());
    this.modalError.set(null);
    this.modoModal.set('crear');
  }

  abrirEditar(c: CtaResumenDto): void {
    this.admin.obtenerCtaDetalle(c.id).subscribe({
      next: (detalle) => {
        this.form.set({
          codigo: detalle.codigo,
          nombre: detalle.nombre,
          area: detalle.area,
          provincia: detalle.provincia,
          ciudad: detalle.ciudad,
          direccion: detalle.direccion,
          codigoPostal: detalle.codigoPostal,
          esNodoAereo: detalle.esNodoAereo,
          esNodoMaritimo: detalle.esNodoMaritimo
        });
        this.ctaEditando.set(c);
        this.modalError.set(null);
        this.modoModal.set('editar');
      },
      error: () => {
        this.actionError.set('No se pudo cargar el detalle del CTA.');
      }
    });
  }

  cerrarModal(): void {
    this.modoModal.set(null);
    this.ctaEditando.set(null);
    this.modalError.set(null);
  }

  actualizarForm<K extends keyof CrearCtaDto>(campo: K, valor: CrearCtaDto[K]): void {
    this.form.set({ ...this.form(), [campo]: valor });
  }

  guardar(): void {
    const dto = this.form();
    const modo = this.modoModal();
    if (!modo) return;

    if (!dto.nombre?.trim()) { this.modalError.set('El nombre es obligatorio.'); return; }
    if (!dto.area?.trim()) { this.modalError.set('El área zonal es obligatoria.'); return; }
    if (modo === 'crear' && !dto.codigo?.trim()) { this.modalError.set('El código es obligatorio.'); return; }

    this.saving.set(true);
    this.modalError.set(null);

    if (modo === 'crear') {
      this.admin.crearCta(dto).subscribe({
        next: () => { this.saving.set(false); this.cerrarModal(); this.cargar(); },
        error: (err) => {
          this.saving.set(false);
          this.modalError.set(err?.error?.message ?? 'No se pudo crear el CTA.');
        }
      });
    } else {
      const id = this.ctaEditando()?.id;
      if (!id) { this.saving.set(false); return; }
      const editDto: EditarCtaDto = {
        nombre: dto.nombre,
        area: dto.area,
        provincia: dto.provincia,
        ciudad: dto.ciudad,
        direccion: dto.direccion,
        codigoPostal: dto.codigoPostal,
        esNodoAereo: dto.esNodoAereo,
        esNodoMaritimo: dto.esNodoMaritimo
      };
      this.admin.editarCta(id, editDto).subscribe({
        next: () => { this.saving.set(false); this.cerrarModal(); this.cargar(); },
        error: (err) => {
          this.saving.set(false);
          this.modalError.set(err?.error?.message ?? 'No se pudo editar el CTA.');
        }
      });
    }
  }

  desactivar(c: CtaResumenDto): void {
    if (!confirm(`¿Desactivar el CTA ${c.codigo} – ${c.nombre}?\n\nNo se podrá si tiene operarios, tareas o movimientos activos.`)) return;
    this.actionError.set(null);
    this.admin.desactivarCta(c.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.actionError.set(err?.error?.message ?? 'No se pudo desactivar el CTA.')
    });
  }

  reactivar(c: CtaResumenDto): void {
    this.actionError.set(null);
    this.admin.reactivarCta(c.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.actionError.set(err?.error?.message ?? 'No se pudo reactivar el CTA.')
    });
  }

  private formVacio(): CrearCtaDto {
    return {
      codigo: '',
      nombre: '',
      area: 'Centro',
      provincia: '',
      ciudad: '',
      direccion: '',
      codigoPostal: '',
      esNodoAereo: false,
      esNodoMaritimo: false
    };
  }
}
