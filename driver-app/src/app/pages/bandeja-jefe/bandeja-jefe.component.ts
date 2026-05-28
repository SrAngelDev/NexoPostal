import { Component, OnInit, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  RepartoService,
  PaqueteBandejaJefe,
  RepartidorPerfil,
  RutaResumen
} from '../../services/reparto.service';
import { AuthService } from '../../services/auth.service';
import { SignalrService } from '../../services/signalr.service';
import { DriverNavbarComponent } from '../../components/driver-navbar/driver-navbar.component';

type ModoAsignacion = 'nueva' | 'existente';

@Component({
  selector: 'app-bandeja-jefe',
  standalone: true,
  imports: [CommonModule, FormsModule, DriverNavbarComponent],
  templateUrl: './bandeja-jefe.component.html',
  styleUrl: './bandeja-jefe.component.css'
})
export class BandejaJefeComponent implements OnInit {
  pendientes = signal<PaqueteBandejaJefe[]>([]);
  repartidores = signal<RepartidorPerfil[]>([]);
  rutasPlanificadas = signal<RutaResumen[]>([]);

  cargando = signal(false);
  procesando = signal(false);
  error = signal<string | null>(null);
  mensaje = signal<string | null>(null);

  seleccion = signal<Set<number>>(new Set<number>());
  mostrarModal = signal(false);
  modo = signal<ModoAsignacion>('nueva');

  // Form nueva ruta (signals para que puedeAsignar() reaccione automáticamente)
  repartidorIdSeleccionado = signal<number | null>(null);
  fechaRuta = signal(new Date().toISOString().split('T')[0]);
  observacionesRuta = signal('');

  // Form ruta existente
  rutaExistenteIdSeleccionada = signal<number | null>(null);

  userName = '';

  readonly seleccionados = computed(() => {
    const ids = this.seleccion();
    return this.pendientes().filter(p => ids.has(p.id));
  });

  readonly puedeAsignar = computed(() => {
    if (this.modo() === 'nueva') {
      return this.repartidorIdSeleccionado() != null && this.seleccion().size > 0;
    }
    return this.rutaExistenteIdSeleccionada() != null && this.seleccion().size > 0;
  });

  constructor(
    private repartoService: RepartoService,
    private authService: AuthService,
    private router: Router,
    private signalr: SignalrService
  ) {
    this.userName = this.authService.getCurrentUser()?.user ?? '';

    // Refrescar bandeja al recibir notificación de nuevo paquete disponible.
    effect(() => {
      const notifs = this.signalr.notificaciones();
      const ultima = notifs[0];
      if (ultima?.evento === 'PaqueteEnBandeja' && !ultima.leida) {
        this.refrescar();
      }
    });
  }

  ngOnInit(): void {
    this.refrescar();
  }

  refrescar(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.mensaje.set(null);

    this.repartoService.obtenerBandeja(undefined, false).subscribe({
      next: (data) => {
        this.pendientes.set(data ?? []);
        this.cargando.set(false);
      },
      error: (err) => {
        console.error('Error cargando bandeja:', err);
        this.error.set('No se pudo cargar la bandeja.');
        this.cargando.set(false);
      }
    });

    this.repartoService.obtenerRepartidores(true).subscribe({
      next: (data) => this.repartidores.set(data ?? []),
      error: (err) => console.error('Error cargando repartidores:', err)
    });

    const hoy = new Date().toISOString().split('T')[0];
    this.repartoService.obtenerRutas(hoy).subscribe({
      next: (data) => this.rutasPlanificadas.set((data ?? []).filter(r => r.estado === 'Planificada')),
      error: (err) => console.error('Error cargando rutas:', err)
    });
  }

  toggleSeleccion(id: number): void {
    const set = new Set(this.seleccion());
    if (set.has(id)) set.delete(id);
    else set.add(id);
    this.seleccion.set(set);
  }

  estaSeleccionado(id: number): boolean {
    return this.seleccion().has(id);
  }

  seleccionarTodos(): void {
    const ids = this.pendientes().map(p => p.id);
    this.seleccion.set(new Set(ids));
  }

  limpiarSeleccion(): void {
    this.seleccion.set(new Set());
  }

  abrirModal(modo: ModoAsignacion): void {
    if (this.seleccion().size === 0) {
      this.error.set('Selecciona al menos un paquete.');
      return;
    }
    this.modo.set(modo);
    this.mostrarModal.set(true);
    this.error.set(null);
  }

  cerrarModal(): void {
    this.mostrarModal.set(false);
    this.repartidorIdSeleccionado.set(null);
    this.rutaExistenteIdSeleccionada.set(null);
    this.observacionesRuta.set('');
  }

  async confirmar(): Promise<void> {
    if (!this.puedeAsignar() || this.procesando()) return;

    this.procesando.set(true);
    this.error.set(null);

    try {
      let rutaId: number;

      if (this.modo() === 'nueva') {
        const repartidor = this.repartidores().find(r => r.id === this.repartidorIdSeleccionado());
        if (!repartidor) throw new Error('Selecciona un repartidor.');

        const obs = this.observacionesRuta().trim();
        const ruta = await this.repartoService.crearRuta({
          repartidorId: repartidor.id,
          fechaReparto: this.fechaRuta(),
          oficinaOrigenJsonId: repartidor.oficinaJsonId,
          oficinaOrigenNombre: repartidor.oficinaNombre,
          observaciones: obs.length > 0 ? obs : undefined
        }).toPromise().then(r => {
          if (!r) throw new Error('Sin respuesta del servidor.');
          return r;
        });

        rutaId = ruta.id;
      } else {
        rutaId = this.rutaExistenteIdSeleccionada()!;
      }

      const pendientes = [...this.seleccion()];
      let ok = 0;
      const errores: string[] = [];

      for (const pendienteId of pendientes) {
        try {
          await this.repartoService.asignarPendienteARuta(pendienteId, { rutaRepartoId: rutaId }).toPromise();
          ok++;
        } catch (err: any) {
          const msg = err?.error?.message ?? err?.message ?? 'Error desconocido';
          errores.push(`#${pendienteId}: ${msg}`);
        }
      }

      const total = pendientes.length;
      if (errores.length === 0) {
        this.mensaje.set(`${ok}/${total} paquetes asignados a la ruta correctamente.`);
      } else {
        this.error.set(`${ok}/${total} OK. Errores: ${errores.join(' · ')}`);
      }

      this.cerrarModal();
      this.limpiarSeleccion();
      this.refrescar();
    } catch (err: any) {
      const msg = err?.error?.message ?? err?.message ?? 'No se pudo completar la operación.';
      this.error.set(msg);
    } finally {
      this.procesando.set(false);
    }
  }

  volver(): void {
    this.router.navigate(['/dashboard-jefe']);
  }
}
