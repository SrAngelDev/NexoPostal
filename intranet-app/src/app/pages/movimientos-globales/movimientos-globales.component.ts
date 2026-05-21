import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { VistasGlobalesService, MovimientoGlobalDto } from '../../services/vistas-globales.service';
import { AdminService, CtaResumenDto } from '../../services/admin.service';

const ESTADOS = ['', 'Programado', 'EnTransito', 'Recibido', 'Cancelado'];

@Component({
  selector: 'app-movimientos-globales',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './movimientos-globales.component.html',
  styleUrl: './movimientos-globales.component.css'
})
export class MovimientosGlobalesComponent implements OnInit {
  readonly ESTADOS = ESTADOS;

  movimientos = signal<MovimientoGlobalDto[]>([]);
  ctas = signal<CtaResumenDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  filtroEstado = signal('');
  filtroOrigen = signal<number | null>(null);
  filtroDestino = signal<number | null>(null);

  resumen = computed(() => {
    const list = this.movimientos();
    return {
      total: list.length,
      programados: list.filter(m => m.estado === 'Programado').length,
      enTransito: list.filter(m => m.estado === 'EnTransito').length,
      recibidos: list.filter(m => m.estado === 'Recibido').length,
      urgentes: list.filter(m => m.esUrgente).length
    };
  });

  constructor(
    private svc: VistasGlobalesService,
    private admin: AdminService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.admin.listarCtasAdmin().subscribe({
      next: (ctas) => this.ctas.set(ctas),
      error: () => {}
    });
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.listarMovimientosGlobales({
      estado: this.filtroEstado() || undefined,
      ctaOrigenId: this.filtroOrigen() ?? undefined,
      ctaDestinoId: this.filtroDestino() ?? undefined
    }).subscribe({
      next: (data) => { this.movimientos.set(data); this.loading.set(false); },
      error: () => { this.error.set('No se pudieron cargar los movimientos.'); this.loading.set(false); }
    });
  }

  limpiarFiltros(): void {
    this.filtroEstado.set('');
    this.filtroOrigen.set(null);
    this.filtroDestino.set(null);
    this.cargar();
  }

  volver(): void { this.router.navigate(['/admin']); }

  badgeClass(estado: string): string {
    switch (estado) {
      case 'Programado': return 'badge-info';
      case 'EnTransito': return 'badge-warning';
      case 'Recibido': return 'badge-ok';
      case 'Cancelado': return 'badge-off';
      default: return 'badge-ok';
    }
  }

  iconTransporte(t: string): string {
    switch (t) {
      case 'Terrestre': return 'local_shipping';
      case 'Aereo': return 'flight';
      case 'Maritimo': return 'directions_boat';
      default: return 'route';
    }
  }
}
