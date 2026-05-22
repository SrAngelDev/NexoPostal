import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminTarifasService, TarifaBandaDto, EditarTarifaBandaBulkItemDto } from '../../services/admin-tarifas.service';

interface FilaEditable extends TarifaBandaDto {
  precioEditado: number;
  modificado: boolean;
}

const SERIES_LABEL: Record<string, string> = {
  LocalEstandar: 'Local · Estándar',
  LocalPremium: 'Local · Premium',
  PeninsulaEstandar: 'Península · Estándar',
  PeninsulaPremium: 'Península · Premium'
};

const SERIES_ORDER = ['LocalEstandar', 'LocalPremium', 'PeninsulaEstandar', 'PeninsulaPremium'];

@Component({
  selector: 'app-gestion-tarifas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-tarifas.component.html',
  styleUrl: './gestion-tarifas.component.css'
})
export class GestionTarifasComponent implements OnInit {
  private readonly tarifasService = inject(AdminTarifasService);
  private readonly router = inject(Router);

  readonly seriesOrder = SERIES_ORDER;
  readonly seriesLabel = SERIES_LABEL;

  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  filas = signal<FilaEditable[]>([]);

  hayCambios = computed(() => this.filas().some(f => f.modificado));
  totalCambios = computed(() => this.filas().filter(f => f.modificado).length);

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set(null);
    this.tarifasService.listar().subscribe({
      next: data => {
        const filas: FilaEditable[] = data.map(d => ({ ...d, precioEditado: d.precioBase, modificado: false }));
        this.filas.set(filas);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.error ?? 'Error al cargar tarifas');
        this.loading.set(false);
      }
    });
  }

  filasPorSerie(serie: string): FilaEditable[] {
    return this.filas()
      .filter(f => f.serie === serie)
      .sort((a, b) => a.ordenBanda - b.ordenBanda);
  }

  onPrecioCambio(id: number, valor: number): void {
    this.filas.update(filas =>
      filas.map(f =>
        f.id === id
          ? { ...f, precioEditado: valor, modificado: Math.abs(valor - f.precioBase) > 0.001 }
          : f
      )
    );
    this.success.set(null);
  }

  descartar(): void {
    this.filas.update(filas => filas.map(f => ({ ...f, precioEditado: f.precioBase, modificado: false })));
    this.success.set(null);
  }

  guardar(): void {
    const cambios = this.filas().filter(f => f.modificado);
    if (cambios.length === 0) return;

    const invalidos = cambios.filter(f => !(f.precioEditado > 0));
    if (invalidos.length > 0) {
      this.error.set('Hay precios inválidos (deben ser > 0).');
      return;
    }

    const items: EditarTarifaBandaBulkItemDto[] = cambios.map(f => ({ id: f.id, precioBase: f.precioEditado }));
    this.saving.set(true);
    this.error.set(null);
    this.success.set(null);
    this.tarifasService.editarBulk(items).subscribe({
      next: data => {
        const filas: FilaEditable[] = data.map(d => ({ ...d, precioEditado: d.precioBase, modificado: false }));
        this.filas.set(filas);
        this.saving.set(false);
        this.success.set(`Se actualizaron ${data.length} tarifas correctamente.`);
      },
      error: err => {
        this.error.set(err?.error?.error ?? 'Error al guardar cambios');
        this.saving.set(false);
      }
    });
  }

  restaurarDefaults(): void {
    if (!confirm('¿Restaurar todos los precios a los valores por defecto del sistema? Esta acción no se puede deshacer.')) return;
    this.saving.set(true);
    this.error.set(null);
    this.success.set(null);
    this.tarifasService.resetDefaults().subscribe({
      next: data => {
        const filas: FilaEditable[] = data.map(d => ({ ...d, precioEditado: d.precioBase, modificado: false }));
        this.filas.set(filas);
        this.saving.set(false);
        this.success.set('Tarifas restauradas a los valores por defecto.');
      },
      error: err => {
        this.error.set(err?.error?.error ?? 'Error al restaurar defaults');
        this.saving.set(false);
      }
    });
  }

  volver(): void {
    this.router.navigate(['/admin']);
  }
}
