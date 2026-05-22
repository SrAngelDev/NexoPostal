import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  AdminEnviosService,
  AdminEnvioListItemDto,
  AdminEnvioDetalleDto,
  EstadoEnvio,
  EstadoInterno,
  ESTADO_PUBLICO_OPTIONS,
  ESTADO_INTERNO_OPTIONS,
  estadoPublicoLabel,
  estadoInternoLabel,
  ListarEnviosFiltros
} from '../../services/admin-envios.service';

@Component({
  selector: 'app-gestion-envios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-envios.component.html',
  styleUrl: './gestion-envios.component.css'
})
export class GestionEnviosComponent implements OnInit {
  private readonly api = inject(AdminEnviosService);
  private readonly router = inject(Router);

  readonly estadosPublicos = ESTADO_PUBLICO_OPTIONS;
  readonly estadosInternos = ESTADO_INTERNO_OPTIONS;
  readonly estadoPublicoLabel = estadoPublicoLabel;
  readonly estadoInternoLabel = estadoInternoLabel;

  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  envios = signal<AdminEnvioListItemDto[]>([]);

  // ── filtros ──
  filtroEstado = signal<EstadoEnvio | null>(null);
  filtroEstadoInterno = signal<EstadoInterno | null>(null);
  filtroFechaDesde = signal<string>('');
  filtroFechaHasta = signal<string>('');
  filtroQ = signal<string>('');
  filtroCp = signal<string>('');
  filtroPagado = signal<'todos' | 'pagados' | 'pendientes'>('todos');

  // ── modales ──
  detalle = signal<AdminEnvioDetalleDto | null>(null);
  modalEstadoAbierto = signal(false);
  modalAnularAbierto = signal(false);
  modalReabrirAbierto = signal(false);

  // form cambio estado
  estadoPublicoEdit = signal<EstadoEnvio>(EstadoEnvio.Admitido);
  estadoInternoEdit = signal<EstadoInterno>(EstadoInterno.PendienteRecogida);
  motivoEstado = signal<string>('');
  motivoAnular = signal<string>('');
  motivoReabrir = signal<string>('');

  // KPIs
  totalListados = computed(() => this.envios().length);
  totalEntregados = computed(() => this.envios().filter(e => e.estadoActual === EstadoEnvio.Entregado).length);
  totalEnTransito = computed(() => this.envios().filter(e =>
    e.estadoActual === EstadoEnvio.EnTransito ||
    e.estadoActual === EstadoEnvio.EnOficina ||
    e.estadoActual === EstadoEnvio.EnReparto
  ).length);
  totalIncidencias = computed(() => this.envios().filter(e => e.estadoActual === EstadoEnvio.Incidencia).length);
  totalDevueltos = computed(() => this.envios().filter(e => e.estadoActual === EstadoEnvio.Devuelto).length);

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.loading.set(true);
    this.error.set(null);
    const f: ListarEnviosFiltros = {
      estado: this.filtroEstado(),
      estadoInterno: this.filtroEstadoInterno(),
      fechaDesde: this.filtroFechaDesde() || null,
      fechaHasta: this.filtroFechaHasta() || null,
      q: this.filtroQ() || null,
      cp: this.filtroCp() || null,
      pagado: this.filtroPagado() === 'todos' ? null : this.filtroPagado() === 'pagados',
      limit: 500
    };
    this.api.listar(f).subscribe({
      next: data => { this.envios.set(data); this.loading.set(false); },
      error: err => {
        this.error.set(err?.error?.mensaje ?? 'Error al cargar envíos');
        this.loading.set(false);
      }
    });
  }

  limpiarFiltros(): void {
    this.filtroEstado.set(null);
    this.filtroEstadoInterno.set(null);
    this.filtroFechaDesde.set('');
    this.filtroFechaHasta.set('');
    this.filtroQ.set('');
    this.filtroCp.set('');
    this.filtroPagado.set('todos');
    this.cargar();
  }

  // ── detalle ──
  verDetalle(e: AdminEnvioListItemDto): void {
    this.api.obtener(e.numeroSeguimiento).subscribe({
      next: d => this.detalle.set(d),
      error: err => this.error.set(err?.error?.mensaje ?? 'Error al cargar detalle')
    });
  }

  cerrarDetalle(): void { this.detalle.set(null); }

  // ── cambiar estado ──
  abrirCambiarEstado(d: AdminEnvioDetalleDto): void {
    this.estadoPublicoEdit.set(d.estadoActual);
    this.estadoInternoEdit.set(d.estadoInternoActual);
    this.motivoEstado.set('');
    this.modalEstadoAbierto.set(true);
  }
  confirmarCambiarEstado(): void {
    const d = this.detalle();
    if (!d) return;
    this.saving.set(true);
    this.api.cambiarEstado(d.numeroSeguimiento, {
      estadoPublico: Number(this.estadoPublicoEdit()) as EstadoEnvio,
      estadoInterno: Number(this.estadoInternoEdit()) as EstadoInterno,
      motivo: this.motivoEstado() || null
    }).subscribe({
      next: nuevo => {
        this.saving.set(false);
        this.modalEstadoAbierto.set(false);
        this.detalle.set(nuevo);
        this.success.set(`Estado actualizado para ${nuevo.numeroSeguimiento}`);
        this.cargar();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.mensaje ?? 'Error al cambiar estado');
      }
    });
  }

  // ── anular ──
  abrirAnular(): void { this.motivoAnular.set(''); this.modalAnularAbierto.set(true); }
  confirmarAnular(): void {
    const d = this.detalle();
    if (!d) return;
    this.saving.set(true);
    this.api.anular(d.numeroSeguimiento, { motivo: this.motivoAnular() || null }).subscribe({
      next: nuevo => {
        this.saving.set(false);
        this.modalAnularAbierto.set(false);
        this.detalle.set(nuevo);
        this.success.set(`Envío ${nuevo.numeroSeguimiento} anulado`);
        this.cargar();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.mensaje ?? 'Error al anular');
      }
    });
  }

  // ── reabrir ──
  abrirReabrir(): void { this.motivoReabrir.set(''); this.modalReabrirAbierto.set(true); }
  confirmarReabrir(): void {
    const d = this.detalle();
    if (!d) return;
    this.saving.set(true);
    this.api.reabrir(d.numeroSeguimiento, { motivo: this.motivoReabrir() || null }).subscribe({
      next: nuevo => {
        this.saving.set(false);
        this.modalReabrirAbierto.set(false);
        this.detalle.set(nuevo);
        this.success.set(`Envío ${nuevo.numeroSeguimiento} reabierto`);
        this.cargar();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.mensaje ?? 'Error al reabrir');
      }
    });
  }

  estadoBadgeClass(e: EstadoEnvio): string {
    switch (e) {
      case EstadoEnvio.Entregado: return 'badge-success';
      case EstadoEnvio.EnReparto:
      case EstadoEnvio.EnTransito:
      case EstadoEnvio.EnOficina: return 'badge-info';
      case EstadoEnvio.Incidencia: return 'badge-warn';
      case EstadoEnvio.Devuelto: return 'badge-danger';
      case EstadoEnvio.PendientePago: return 'badge-muted';
      default: return 'badge-neutral';
    }
  }

  volver(): void { this.router.navigate(['/admin']); }
}
