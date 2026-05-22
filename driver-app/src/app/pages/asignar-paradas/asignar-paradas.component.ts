import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  RepartoService,
  EntregaPendienteAsignacion,
  RutaResumen
} from '../../services/reparto.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-asignar-paradas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './asignar-paradas.component.html',
  styleUrl: './asignar-paradas.component.css'
})
export class AsignarParadasComponent implements OnInit {
  entregas = signal<EntregaPendienteAsignacion[]>([]);
  rutasDisponibles = signal<RutaResumen[]>([]);
  cargando = signal(false);
  error = signal<string | null>(null);
  mensaje = signal<string | null>(null);

  // selección por entregaId
  seleccion = new Map<number, number | null>();
  procesando = new Set<number>();

  constructor(
    private repartoService: RepartoService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.mensaje.set(null);

    const hoy = new Date().toISOString().slice(0, 10);

    this.repartoService.obtenerEntregasPendientesAsignacion().subscribe({
      next: (data) => {
        this.entregas.set(data);
        this.seleccion.clear();
        data.forEach(e => this.seleccion.set(e.entregaId, null));
      },
      error: (err) => {
        this.error.set('No se pudieron cargar las entregas pendientes.');
        console.error(err);
      }
    });

    this.repartoService.obtenerRutas(hoy).subscribe({
      next: (rutas) => {
        // Solo rutas planificadas son válidas como destino (pendientes aún no salidas)
        this.rutasDisponibles.set(rutas.filter(r => r.estado === 'Planificada'));
        this.cargando.set(false);
      },
      error: (err) => {
        this.error.set('No se pudieron cargar las rutas del día.');
        this.cargando.set(false);
        console.error(err);
      }
    });
  }

  rutasParaEntrega(entrega: EntregaPendienteAsignacion): RutaResumen[] {
    // Solo rutas distintas a la actual y en la misma oficina (asumiendo origen=oficina destino).
    return this.rutasDisponibles().filter(r => r.id !== entrega.rutaActualId);
  }

  setSeleccion(entregaId: number, rutaIdStr: string): void {
    const rutaId = rutaIdStr ? Number(rutaIdStr) : null;
    this.seleccion.set(entregaId, rutaId);
  }

  asignar(entrega: EntregaPendienteAsignacion): void {
    const nuevaRutaId = this.seleccion.get(entrega.entregaId);
    if (!nuevaRutaId) return;

    this.procesando.add(entrega.entregaId);
    this.mensaje.set(null);
    this.error.set(null);

    this.repartoService.reasignarEntrega(entrega.entregaId, nuevaRutaId).subscribe({
      next: () => {
        this.procesando.delete(entrega.entregaId);
        this.mensaje.set(`Entrega ${entrega.numeroExpedicion} reasignada correctamente.`);
        // Quitar la entrega de la lista
        this.entregas.set(this.entregas().filter(e => e.entregaId !== entrega.entregaId));
        this.seleccion.delete(entrega.entregaId);
      },
      error: (err) => {
        this.procesando.delete(entrega.entregaId);
        this.error.set(err?.error?.message ?? 'Error al reasignar la entrega.');
        console.error(err);
      }
    });
  }

  isProcesando(id: number): boolean {
    return this.procesando.has(id);
  }

  puedeAsignar(id: number): boolean {
    return !!this.seleccion.get(id) && !this.isProcesando(id);
  }

  volver(): void {
    this.router.navigate(['/dashboard-jefe']);
  }
}
