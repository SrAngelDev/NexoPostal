import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { VistasGlobalesService, IncidenciaGlobalDto } from '../../services/vistas-globales.service';
import { AdminService, CtaResumenDto } from '../../services/admin.service';

const ESTADOS = ['', 'Abierta', 'EnRevision', 'Resuelta', 'Cerrada'];
const TIPOS = ['', 'PaqueteDañado', 'PaqueteExtraviado', 'PaqueteFueraDeTareas', 'ErrorDirección', 'Otro'];

@Component({
  selector: 'app-incidencias-globales',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './incidencias-globales.component.html',
  styleUrl: './incidencias-globales.component.css'
})
export class IncidenciasGlobalesComponent implements OnInit {
  readonly ESTADOS = ESTADOS;
  readonly TIPOS = TIPOS;

  incidencias = signal<IncidenciaGlobalDto[]>([]);
  ctas = signal<CtaResumenDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  filtroEstado = signal('');
  filtroCtaId = signal<number | null>(null);
  filtroTipo = signal('');

  resumen = computed(() => {
    const list = this.incidencias();
    return {
      total: list.length,
      abiertas: list.filter(i => i.estado === 'Abierta').length,
      enRevision: list.filter(i => i.estado === 'EnRevision').length,
      resueltas: list.filter(i => i.estado === 'Resuelta').length,
      cerradas: list.filter(i => i.estado === 'Cerrada').length
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
      error: () => { /* sin CTAs no podemos filtrar pero sí mostrar lista */ }
    });
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.listarIncidenciasGlobales({
      estado: this.filtroEstado() || undefined,
      ctaId: this.filtroCtaId() ?? undefined,
      tipo: this.filtroTipo() || undefined
    }).subscribe({
      next: (data) => {
        this.incidencias.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar las incidencias.');
        this.loading.set(false);
      }
    });
  }

  limpiarFiltros(): void {
    this.filtroEstado.set('');
    this.filtroCtaId.set(null);
    this.filtroTipo.set('');
    this.cargar();
  }

  volver(): void {
    this.router.navigate(['/admin']);
  }

  badgeClass(estado: string): string {
    switch (estado) {
      case 'Abierta': return 'badge-danger';
      case 'EnRevision': return 'badge-warning';
      case 'Resuelta': return 'badge-info';
      case 'Cerrada': return 'badge-off';
      default: return 'badge-ok';
    }
  }
}
