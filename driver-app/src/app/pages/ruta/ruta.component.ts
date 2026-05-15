import { CommonModule } from '@angular/common';
import { Component, computed, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import {
  EntregaPaquete,
  EstadoEntregaConfirmacion,
  ESTADOS_CONFIRMACION,
  FinalizarRutaRequest,
  RegistrarEntregaRequest,
  RepartoService,
  RutaRepartoDetalle
} from '../../services/reparto.service';
import { RepartoOfflineQueueService } from '../../services/reparto-offline-queue.service';

@Component({
  selector: 'app-ruta',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ruta.component.html',
  styleUrl: './ruta.component.css'
})
export class RutaComponent implements OnInit, OnDestroy {
  userName = '';
  userRole = '';

  cargandoRuta = signal(false);
  cargandoEntregas = signal(false);
  enviandoConfirmacion = signal(false);
  sincronizando = signal(false);
  procesandoRutaAccion = signal(false);

  error = signal('');
  mensaje = signal('');

  online = signal(navigator.onLine);
  gpsActivo = signal(false);

  ruta = signal<RutaRepartoDetalle | null>(null);
  entregas = signal<EntregaPaquete[]>([]);
  entregaSeleccionadaId = signal<number | null>(null);
  ultimaUbicacion = signal<{ latitud: number; longitud: number; fecha: string } | null>(null);
  fotoPreview = signal<string | null>(null);

  estadoSeleccionado: EstadoEntregaConfirmacion = 'Entregado';
  receptorNombre = '';
  receptorDni = '';
  observaciones = '';
  firmaDigital = '';
  fotoEntrega = '';
  observacionesFinRuta = '';

  readonly estadosConfirmacion = ESTADOS_CONFIRMACION;

  readonly entregaSeleccionada = computed(() => {
    const id = this.entregaSeleccionadaId();
    if (id == null) return null;
    return this.entregas().find(e => e.id === id) ?? null;
  });

  readonly resumen = computed(() => {
    const all = this.entregas();
    const total = all.length;
    const completadas = all.filter(e => e.estado === 'Entregado' || e.estado === 'EntregadoPuntoAlternativo').length;
    const fallidas = all.filter(e => e.estado === 'Ausente' || e.estado === 'DireccionIncorrecta' || e.estado === 'Rechazado').length;
    const pendientes = total - completadas - fallidas;

    return {
      total,
      completadas,
      fallidas,
      pendientes,
      progreso: total > 0 ? Math.round((completadas / total) * 100) : 0
    };
  });

  private gpsTimer: ReturnType<typeof setInterval> | null = null;

  constructor(
    private authService: AuthService,
    private router: Router,
    private repartoService: RepartoService,
    private offlineQueue: RepartoOfflineQueueService
  ) {
    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
    this.userRole = user?.rol ?? '';
  }

  ngOnInit(): void {
    window.addEventListener('online', this.onOnline);
    window.addEventListener('offline', this.onOffline);

    this.cargarRutaActiva();
    this.sincronizarPendientes();
  }

  ngOnDestroy(): void {
    this.detenerGps();
    window.removeEventListener('online', this.onOnline);
    window.removeEventListener('offline', this.onOffline);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  volverDashboard(): void {
    this.router.navigate(['/']);
  }

  irEscaneo(): void {
    this.router.navigate(['/escaneo']);
  }

  recargar(): void {
    this.cargarRutaActiva();
  }

  iniciarRuta(): void {
    const ruta = this.ruta();
    if (!ruta || ruta.estado !== 'Planificada' || this.procesandoRutaAccion()) return;

    this.procesandoRutaAccion.set(true);
    this.error.set('');

    this.repartoService.iniciarRuta(ruta.id).subscribe({
      next: (actualizada) => {
        this.ruta.set(actualizada);
        this.mensaje.set(`Ruta ${actualizada.codigo} iniciada.`);
        this.iniciarGps(actualizada.id);
        this.cargarEntregas(actualizada.id);
        this.procesandoRutaAccion.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'No se pudo iniciar la ruta.');
        this.procesandoRutaAccion.set(false);
      }
    });
  }

  finalizarRuta(): void {
    const ruta = this.ruta();
    if (!ruta || ruta.estado !== 'EnCurso' || this.procesandoRutaAccion()) return;

    this.procesandoRutaAccion.set(true);
    this.error.set('');

    const request: FinalizarRutaRequest = {
      observaciones: this.observacionesFinRuta || undefined
    };

    this.repartoService.finalizarRuta(ruta.id, request).subscribe({
      next: (actualizada) => {
        this.ruta.set(actualizada);
        this.mensaje.set(`Ruta ${actualizada.codigo} finalizada con estado ${actualizada.estado}.`);
        this.observacionesFinRuta = '';
        this.detenerGps();
        this.cargarEntregas(actualizada.id);
        this.procesandoRutaAccion.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'No se pudo finalizar la ruta.');
        this.procesandoRutaAccion.set(false);
      }
    });
  }

  seleccionarEntrega(entrega: EntregaPaquete): void {
    this.entregaSeleccionadaId.set(entrega.id);
    this.receptorNombre = entrega.receptorNombre ?? entrega.nombreDestinatario;
    this.receptorDni = entrega.receptorDni ?? '';
    this.observaciones = entrega.observaciones ?? '';
    this.firmaDigital = entrega.firmaDigital ?? '';
    this.fotoEntrega = entrega.fotoEntrega ?? '';
    this.fotoPreview.set(null);
    this.sugerirEstadoInicial(entrega.estado);
  }

  confirmarEntrega(): void {
    const entrega = this.entregaSeleccionada();
    if (!entrega) return;

    this.enviandoConfirmacion.set(true);
    this.error.set('');
    this.mensaje.set('');

    this.obtenerPosicionActual()
      .then((coords) => {
        const fallbackCoords = this.ultimaUbicacion();
        const request: RegistrarEntregaRequest = {
          estado: this.estadoSeleccionado,
          receptorNombre: this.receptorNombre || undefined,
          receptorDni: this.receptorDni || undefined,
          observaciones: this.observaciones || undefined,
          firmaDigital: this.firmaDigital || undefined,
          fotoEntrega: this.fotoEntrega || undefined,
          latitud: coords?.latitud ?? fallbackCoords?.latitud,
          longitud: coords?.longitud ?? fallbackCoords?.longitud
        };

        if (!this.online()) {
          this.encolarConfirmacionLocal(entrega.id, request);
          return;
        }

        this.repartoService.confirmarEntrega(entrega.id, request).subscribe({
          next: (actualizada) => {
            this.reemplazarEntrega(actualizada);
            this.mensaje.set(`Entrega ${actualizada.numeroExpedicion} registrada correctamente.`);
            this.limpiarFormularioConfirmacion();
            this.enviandoConfirmacion.set(false);
          },
          error: () => {
            this.encolarConfirmacionLocal(entrega.id, request);
          }
        });
      })
      .catch(() => {
        this.enviandoConfirmacion.set(false);
        this.error.set('No se pudo obtener la ubicación actual.');
      });
  }

  onFotoSeleccionada(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      this.fotoEntrega = '';
      this.fotoPreview.set(null);
      return;
    }

    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    this.fotoEntrega = `${timestamp}_${file.name}`.slice(0, 200);

    const reader = new FileReader();
    reader.onload = () => {
      this.fotoPreview.set(typeof reader.result === 'string' ? reader.result : null);
    };
    reader.readAsDataURL(file);
  }

  async sincronizarPendientes(): Promise<void> {
    if (!this.online() || this.sincronizando()) return;

    this.sincronizando.set(true);
    const result = await this.offlineQueue.procesarPendientes(this.repartoService);

    if (result.procesados > 0) {
      this.mensaje.set(`Sincronización completada. Enviados ${result.procesados} eventos pendientes.`);
      const ruta = this.ruta();
      if (ruta) this.cargarEntregas(ruta.id);
    }

    this.sincronizando.set(false);
  }

  estadoClase(estado: string): string {
    if (estado === 'Entregado' || estado === 'EntregadoPuntoAlternativo') return 'estado-ok';
    if (estado === 'Ausente' || estado === 'DireccionIncorrecta' || estado === 'Rechazado') return 'estado-error';
    if (estado === 'EnCamino') return 'estado-info';
    return 'estado-pendiente';
  }

  pendientesCola(): number {
    return this.offlineQueue.pendientes();
  }

  private cargarRutaActiva(): void {
    this.cargandoRuta.set(true);
    this.error.set('');

    this.repartoService.obtenerMiRuta().subscribe({
      next: (rutas) => {
        const rutaActiva =
          rutas.find(r => r.estado === 'EnCurso') ??
          rutas.find(r => r.estado === 'Planificada') ??
          rutas[0] ??
          null;

        this.ruta.set(rutaActiva);

        if (!rutaActiva) {
          this.entregas.set([]);
          this.detenerGps();
          this.cargandoRuta.set(false);
          return;
        }

        this.cargarEntregas(rutaActiva.id);
        if (rutaActiva.estado === 'EnCurso') {
          this.iniciarGps(rutaActiva.id);
        } else {
          this.detenerGps();
        }
      },
      error: (err) => {
        this.cargandoRuta.set(false);
        this.error.set(err.error?.message || 'No se pudo cargar la ruta asignada.');
      }
    });
  }

  private cargarEntregas(rutaId: number): void {
    this.cargandoEntregas.set(true);

    this.repartoService.obtenerEntregas(rutaId).subscribe({
      next: (entregas) => {
        this.entregas.set([...entregas].sort((a, b) => a.ordenEnRuta - b.ordenEnRuta));

        const selectedId = this.entregaSeleccionadaId();
        if (selectedId != null && !entregas.some(e => e.id === selectedId)) {
          this.entregaSeleccionadaId.set(null);
        }

        this.cargandoEntregas.set(false);
        this.cargandoRuta.set(false);
      },
      error: (err) => {
        this.cargandoEntregas.set(false);
        this.cargandoRuta.set(false);
        this.error.set(err.error?.message || 'No se pudieron cargar las entregas de la ruta.');
      }
    });
  }

  private iniciarGps(rutaId: number): void {
    if (!('geolocation' in navigator)) {
      this.gpsActivo.set(false);
      return;
    }

    this.detenerGps();
    this.gpsActivo.set(true);

    this.enviarUbicacion(rutaId);
    this.gpsTimer = setInterval(() => this.enviarUbicacion(rutaId), 30000);
  }

  private detenerGps(): void {
    if (this.gpsTimer) {
      clearInterval(this.gpsTimer);
      this.gpsTimer = null;
    }
    this.gpsActivo.set(false);
  }

  private enviarUbicacion(rutaId: number): void {
    this.obtenerPosicionActual().then((coords) => {
      if (!coords) return;

      this.ultimaUbicacion.set({
        ...coords,
        fecha: new Date().toISOString()
      });

      const request = {
        latitud: coords.latitud,
        longitud: coords.longitud,
        rutaId
      };

      if (!this.online()) {
        this.offlineQueue.encolarUbicacion({ request });
        return;
      }

      this.repartoService.registrarUbicacion(request).subscribe({
        error: () => {
          this.offlineQueue.encolarUbicacion({ request });
        }
      });
    });
  }

  private obtenerPosicionActual(): Promise<{ latitud: number; longitud: number } | null> {
    return new Promise((resolve) => {
      if (!('geolocation' in navigator)) {
        resolve(null);
        return;
      }

      navigator.geolocation.getCurrentPosition(
        (pos) => resolve({ latitud: pos.coords.latitude, longitud: pos.coords.longitude }),
        () => resolve(null),
        { enableHighAccuracy: true, timeout: 8000, maximumAge: 15000 }
      );
    });
  }

  private reemplazarEntrega(actualizada: EntregaPaquete): void {
    this.entregas.update(list => list.map(e => (e.id === actualizada.id ? actualizada : e)));
  }

  private encolarConfirmacionLocal(entregaId: number, request: RegistrarEntregaRequest): void {
    this.offlineQueue.encolarConfirmacion({ entregaId, request });

    this.entregas.update(list =>
      list.map(e =>
        e.id === entregaId
          ? {
              ...e,
              estado: request.estado,
              fechaIntento: new Date().toISOString(),
              receptorNombre: request.receptorNombre,
              receptorDni: request.receptorDni,
              observaciones: this.mergeObservaciones(request.observaciones, 'Pendiente de sincronizar'),
              latitudEntrega: request.latitud,
              longitudEntrega: request.longitud,
              firmaDigital: request.firmaDigital,
              fotoEntrega: request.fotoEntrega
            }
          : e
      )
    );

    this.mensaje.set('Sin conexión: confirmación guardada en cola para reintento automático.');
    this.limpiarFormularioConfirmacion();
    this.enviandoConfirmacion.set(false);
  }

  private mergeObservaciones(base?: string, extra?: string): string | undefined {
    const cleanBase = (base ?? '').trim();
    const cleanExtra = (extra ?? '').trim();

    if (!cleanBase && !cleanExtra) return undefined;
    if (!cleanBase) return cleanExtra;
    if (!cleanExtra) return cleanBase;

    return `${cleanBase} | ${cleanExtra}`;
  }

  private limpiarFormularioConfirmacion(): void {
    this.estadoSeleccionado = 'Entregado';
    this.receptorNombre = '';
    this.receptorDni = '';
    this.observaciones = '';
    this.firmaDigital = '';
    this.fotoEntrega = '';
    this.fotoPreview.set(null);
    this.enviandoConfirmacion.set(false);
  }

  private sugerirEstadoInicial(estadoEntrega: string): void {
    if (estadoEntrega === 'EnCamino' || estadoEntrega === 'Pendiente') {
      this.estadoSeleccionado = 'Entregado';
      return;
    }

    if (estadoEntrega === 'Ausente') {
      this.estadoSeleccionado = 'EntregadoPuntoAlternativo';
      return;
    }

    if (this.estadosConfirmacion.some(e => e.valor === estadoEntrega)) {
      this.estadoSeleccionado = estadoEntrega as EstadoEntregaConfirmacion;
      return;
    }

    this.estadoSeleccionado = 'Entregado';
  }

  private readonly onOnline = () => {
    this.online.set(true);
    this.sincronizarPendientes();
  };

  private readonly onOffline = () => {
    this.online.set(false);
  };
}
