import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  computed,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { DriverNavbarComponent } from '../../components/driver-navbar/driver-navbar.component';
import {
  EntregaPaquete,
  EstadoEntregaConfirmacion,
  ESTADOS_CONFIRMACION,
  FinalizarRutaRequest,
  RegistrarEntregaRequest,
  RepartoService,
  RutaRepartoDetalle
} from '../../services/reparto.service';
import type { CircleMarker, LatLngExpression, Map, Polyline } from 'leaflet';

type PosicionGps = {
  latitud: number;
  longitud: number;
  fecha: string;
};

type FuenteGps = 'watch' | 'heartbeat' | 'manual' | 'retry' | 'inicio' | 'foreground';
type NavegadorExterno = 'google' | 'waze';

@Component({
  selector: 'app-ruta',
  standalone: true,
  imports: [CommonModule, FormsModule, DriverNavbarComponent],
  templateUrl: './ruta.component.html',
  styleUrl: './ruta.component.css'
})
export class RutaComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('mapCanvas') mapCanvas?: ElementRef<HTMLDivElement>;

  userName = '';
  userRole = '';

  cargandoRuta = signal(false);
  cargandoEntregas = signal(false);
  enviandoConfirmacion = signal(false);
  procesandoRutaAccion = signal(false);

  error = signal('');
  mensaje = signal('');

  gpsActivo = signal(false);
  seguimientoSegundoPlano = signal(false);
  mapaDisponible = signal(false);

  ruta = signal<RutaRepartoDetalle | null>(null);
  entregas = signal<EntregaPaquete[]>([]);
  entregaSeleccionadaId = signal<number | null>(null);
  ultimaUbicacion = signal<PosicionGps | null>(null);
  historialUbicaciones = signal<PosicionGps[]>([]);
  ultimaSincronizacionGps = signal<string | null>(null);
  estadoSeleccionado: EstadoEntregaConfirmacion = 'Entregado';
  receptorNombre = '';
  receptorDni = '';
  observaciones = '';
  observacionesFinRuta = '';

  readonly estadosConfirmacion = ESTADOS_CONFIRMACION;

  readonly siguienteParada = computed(() => {
    return this.entregas().find(e => e.estado === 'Pendiente' || e.estado === 'EnCamino') ?? null;
  });

  readonly entregaSeleccionada = computed(() => {
    const id = this.entregaSeleccionadaId();
    if (id == null) return null;
    return this.entregas().find(e => e.id === id) ?? null;
  });

  readonly distanciaSiguienteParadaKm = computed(() => {
    const siguiente = this.siguienteParada();
    const gps = this.ultimaUbicacion();

    if (!siguiente || !gps || siguiente.latitudEntrega == null || siguiente.longitudEntrega == null) {
      return null;
    }

    return this.calcularDistanciaKm(
      gps.latitud,
      gps.longitud,
      siguiente.latitudEntrega,
      siguiente.longitudEntrega
    );
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

  private leafletModule: typeof import('leaflet') | null = null;
  private mapa: Map | null = null;
  private marcadorRepartidor: CircleMarker | null = null;
  private marcadorDestinoSeleccionado: CircleMarker | null = null;
  private capaHistorial: Polyline | null = null;
  private marcadoresEntrega: CircleMarker[] = [];

  private gpsHeartbeatTimer: ReturnType<typeof setInterval> | null = null;
  private gpsRetryTimer: ReturnType<typeof setTimeout> | null = null;
  private watchId: number | null = null;
  private ultimoEnvioGpsMs = 0;
  private erroresGpsConsecutivos = 0;

  private readonly centroMapaDefecto: LatLngExpression = [40.4168, -3.7038];
  private readonly intervaloGpsActivoMs = 20000;
  private readonly intervaloGpsSegundoPlanoMs = 60000;
  private readonly maxHistorialUbicaciones = 80;

  constructor(
    private authService: AuthService,
    private router: Router,
    private repartoService: RepartoService
  ) {
    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
    this.userRole = user?.rol ?? '';
  }

  ngAfterViewInit(): void {
    this.solicitarInicializacionMapa();
  }

  ngOnInit(): void {
    this.seguimientoSegundoPlano.set(document.visibilityState === 'hidden');

    document.addEventListener('visibilitychange', this.onVisibilityChange);

    this.cargarRutaActiva();
  }

  ngOnDestroy(): void {
    this.detenerGps();
    this.destruirMapa();

    document.removeEventListener('visibilitychange', this.onVisibilityChange);
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

    this.sugerirEstadoInicial(entrega.estado);
    this.actualizarMapa();
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
          latitud: coords?.latitud ?? fallbackCoords?.latitud,
          longitud: coords?.longitud ?? fallbackCoords?.longitud
        };

        this.repartoService.confirmarEntrega(entrega.id, request).subscribe({
          next: (actualizada) => {
            this.reemplazarEntrega(actualizada);
            this.mensaje.set(`Entrega ${actualizada.numeroExpedicion} registrada correctamente.`);
            this.limpiarFormularioConfirmacion();
            this.enviandoConfirmacion.set(false);
          },
          error: (err) => {
            this.error.set(err.error?.message || 'No se pudo registrar la entrega. Inténtalo de nuevo.');
            this.enviandoConfirmacion.set(false);
          }
        });
      })
      .catch(() => {
        this.enviandoConfirmacion.set(false);
        this.error.set('No se pudo obtener la ubicación actual.');
      });
  }

  abrirNavegacionSiguiente(navegador: NavegadorExterno = 'google'): void {
    const entrega = this.siguienteParada();
    if (!entrega) return;

    this.abrirNavegacion(entrega, navegador);
  }

  abrirNavegacionSeleccionada(navegador: NavegadorExterno = 'google'): void {
    const entrega = this.entregaSeleccionada();
    if (!entrega) return;

    this.abrirNavegacion(entrega, navegador);
  }

  centrarEnMiPosicion(): void {
    const gps = this.ultimaUbicacion();
    if (gps && this.mapa) {
      this.mapa.setView([gps.latitud, gps.longitud], 16);
      return;
    }

    const ruta = this.ruta();
    if (ruta) {
      this.enviarUbicacion(ruta.id, 'manual', true);
    }
  }

  centrarMapaEnRuta(): void {
    this.actualizarMapa();
  }

  estadoClase(estado: string): string {
    if (estado === 'Entregado' || estado === 'EntregadoPuntoAlternativo') return 'estado-ok';
    if (estado === 'Ausente' || estado === 'DireccionIncorrecta' || estado === 'Rechazado') return 'estado-error';
    if (estado === 'EnCamino') return 'estado-info';
    return 'estado-pendiente';
  }

  private cargarRutaActiva(): void {
    // Solo mostrar spinner de carga la primera vez (sin ruta ya visible).
    // Si hay ruta activa, mantener el map en DOM durante la actualización.
    if (!this.ruta()) {
      this.cargandoRuta.set(true);
    }
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
          this.historialUbicaciones.set([]);

          this.detenerGps();
          this.destruirMapa();
          this.cargandoRuta.set(false);
          return;
        }

        this.solicitarInicializacionMapa();
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

        this.solicitarInicializacionMapa();
        this.actualizarMapa();

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
      this.error.set('El dispositivo no soporta geolocalización.');
      return;
    }

    this.detenerGps();

    this.gpsActivo.set(true);
    this.erroresGpsConsecutivos = 0;
    this.ultimoEnvioGpsMs = 0;

    this.activarWatchGps(rutaId);
    this.programarHeartbeatGps(rutaId);
    this.enviarUbicacion(rutaId, 'inicio', true);
  }

  private activarWatchGps(rutaId: number): void {
    if (this.watchId != null) {
      navigator.geolocation.clearWatch(this.watchId);
      this.watchId = null;
    }

    this.watchId = navigator.geolocation.watchPosition(
      (pos) => {
        this.procesarPosicionGps(rutaId, pos.coords.latitude, pos.coords.longitude, 'watch');
      },
      (error) => {
        if (error.code === error.PERMISSION_DENIED) {
          this.error.set('Permiso de ubicación denegado. Actívalo para mantener el tracking.');
          this.detenerGps();
          return;
        }

        this.programarReintentoGps(rutaId);
      },
      {
        enableHighAccuracy: !this.seguimientoSegundoPlano(),
        timeout: 12000,
        maximumAge: this.seguimientoSegundoPlano() ? 45000 : 15000
      }
    );
  }

  private programarHeartbeatGps(rutaId: number): void {
    if (this.gpsHeartbeatTimer) {
      clearInterval(this.gpsHeartbeatTimer);
      this.gpsHeartbeatTimer = null;
    }

    const intervalo = this.seguimientoSegundoPlano()
      ? this.intervaloGpsSegundoPlanoMs
      : this.intervaloGpsActivoMs * 2;

    this.gpsHeartbeatTimer = setInterval(() => {
      this.enviarUbicacion(rutaId, 'heartbeat');
    }, intervalo);
  }

  private enviarUbicacion(rutaId: number, fuente: FuenteGps = 'manual', forzar = false): void {
    this.obtenerPosicionActual().then((coords) => {
      if (!coords) return;

      this.procesarPosicionGps(rutaId, coords.latitud, coords.longitud, fuente, forzar);
    });
  }

  private procesarPosicionGps(
    rutaId: number,
    latitud: number,
    longitud: number,
    fuente: FuenteGps,
    forzar = false
  ): void {
    this.actualizarPosicionLocal(latitud, longitud);

    const now = Date.now();
    const intervaloMinimo = this.seguimientoSegundoPlano()
      ? this.intervaloGpsSegundoPlanoMs
      : this.intervaloGpsActivoMs;

    const haPasadoIntervalo = now - this.ultimoEnvioGpsMs >= intervaloMinimo;
    if (!forzar && !haPasadoIntervalo) {
      return;
    }

    this.ultimoEnvioGpsMs = now;
    this.enviarPosicionServidor(rutaId, latitud, longitud, fuente);
  }

  private actualizarPosicionLocal(latitud: number, longitud: number): void {
    const fecha = new Date().toISOString();
    const posicion: PosicionGps = { latitud, longitud, fecha };

    this.ultimaUbicacion.set(posicion);
    this.historialUbicaciones.update((actual) => {
      const ultima = actual[actual.length - 1];
      if (ultima) {
        const distanciaMetros = this.calcularDistanciaKm(
          ultima.latitud,
          ultima.longitud,
          latitud,
          longitud
        ) * 1000;

        if (distanciaMetros < 15) {
          return actual;
        }
      }

      const next = [...actual, posicion];
      if (next.length <= this.maxHistorialUbicaciones) {
        return next;
      }

      return next.slice(next.length - this.maxHistorialUbicaciones);
    });

    this.actualizarMapa();
  }

  private enviarPosicionServidor(
    rutaId: number,
    latitud: number,
    longitud: number,
    fuente: FuenteGps
  ): void {
    const request = {
      latitud,
      longitud,
      rutaId,
      tipoUbicacion: this.seguimientoSegundoPlano() ? 'SegundoPlano' : 'GPSActivo',
      descripcion: this.descripcionTracking(fuente)
    };

    this.repartoService.registrarUbicacion(request).subscribe({
      next: () => {
        this.erroresGpsConsecutivos = 0;
        this.ultimaSincronizacionGps.set(new Date().toISOString());
      },
      error: () => {
        this.erroresGpsConsecutivos++;
        this.programarReintentoGps(rutaId);
      }
    });
  }

  private descripcionTracking(fuente: FuenteGps): string {
    if (fuente === 'retry') return 'Reintento automático de ubicación';
    if (fuente === 'heartbeat') return 'Heartbeat de seguimiento de ruta';
    if (this.seguimientoSegundoPlano()) return 'Seguimiento de ruta en segundo plano';
    return 'Seguimiento de ruta en curso';
  }

  private programarReintentoGps(rutaId: number): void {
    if (this.gpsRetryTimer) {
      return;
    }

    const nivel = Math.min(this.erroresGpsConsecutivos, 5);
    const delayMs = Math.min(120000, 4000 * 2 ** nivel);

    this.gpsRetryTimer = setTimeout(() => {
      this.gpsRetryTimer = null;

      if (!this.gpsActivo()) {
        return;
      }

      this.enviarUbicacion(rutaId, 'retry', true);
    }, delayMs);
  }

  private detenerGps(): void {
    if (this.gpsHeartbeatTimer) {
      clearInterval(this.gpsHeartbeatTimer);
      this.gpsHeartbeatTimer = null;
    }

    if (this.gpsRetryTimer) {
      clearTimeout(this.gpsRetryTimer);
      this.gpsRetryTimer = null;
    }

    if (this.watchId != null) {
      navigator.geolocation.clearWatch(this.watchId);
      this.watchId = null;
    }

    this.gpsActivo.set(false);
    this.seguimientoSegundoPlano.set(false);
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
        {
          enableHighAccuracy: !this.seguimientoSegundoPlano(),
          timeout: this.seguimientoSegundoPlano() ? 12000 : 8000,
          maximumAge: this.seguimientoSegundoPlano() ? 45000 : 15000
        }
      );
    });
  }

  private async inicializarMapa(): Promise<void> {
    // Si el mapa ya existe pero su contenedor fue removido del DOM (p.ej. por
    // re-renderizado Angular), destruirlo para forzar la re-inicialización.
    if (this.mapa) {
      const container = this.mapa.getContainer();
      if (!container || container !== this.mapCanvas?.nativeElement) {
        this.destruirMapa();
      } else {
        return; // mapa válido, nada que hacer
      }
    }

    if (!this.mapCanvas?.nativeElement) {
      return;
    }

    try {
      if (!this.leafletModule) {
        const mod: any = await import('leaflet');
        // Leaflet es CJS: con interop de esbuild a veces L.map vive en .default
        this.leafletModule = (mod?.map ? mod : mod?.default) as typeof import('leaflet');
      }
      const L = this.leafletModule;

      if (!L || typeof L.map !== 'function') {
        throw new Error('Leaflet no expone L.map (interop fallido)');
      }

      this.mapa = L.map(this.mapCanvas.nativeElement, {
        zoomControl: false,
        attributionControl: true
      }).setView(this.centroMapaDefecto, 6);

      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
      }).addTo(this.mapa);

      L.control.zoom({ position: 'bottomright' }).addTo(this.mapa);

      this.mapaDisponible.set(true);
      this.actualizarMapa();

      setTimeout(() => this.mapa?.invalidateSize(), 0);
    } catch (err) {
      console.error('[ruta] No se pudo inicializar Leaflet:', err);
      this.mapaDisponible.set(false);
    }
  }

  private solicitarInicializacionMapa(): void {
    setTimeout(() => {
      void this.inicializarMapa();
    }, 0);
  }

  private destruirMapa(): void {
    this.marcadoresEntrega.forEach((m) => m.remove());
    this.marcadoresEntrega = [];

    this.marcadorDestinoSeleccionado?.remove();
    this.marcadorDestinoSeleccionado = null;

    this.marcadorRepartidor?.remove();
    this.marcadorRepartidor = null;

    this.capaHistorial?.remove();
    this.capaHistorial = null;

    if (this.mapa) {
      this.mapa.remove();
      this.mapa = null;
    }

    this.mapaDisponible.set(false);
  }

  private actualizarMapa(): void {
    if (!this.mapa || !this.leafletModule) {
      return;
    }

    const L = this.leafletModule;
    const puntos: [number, number][] = [];

    this.marcadoresEntrega.forEach((m) => m.remove());
    this.marcadoresEntrega = [];

    this.marcadorDestinoSeleccionado?.remove();
    this.marcadorDestinoSeleccionado = null;

    this.capaHistorial?.remove();
    this.capaHistorial = null;

    const gps = this.ultimaUbicacion();
    if (gps) {
      const currentPoint: [number, number] = [gps.latitud, gps.longitud];
      puntos.push(currentPoint);

      if (!this.marcadorRepartidor) {
        this.marcadorRepartidor = L.circleMarker(currentPoint, {
          radius: 8,
          color: '#1d4ed8',
          fillColor: '#3b82f6',
          fillOpacity: 0.9,
          weight: 2
        }).addTo(this.mapa);
        this.marcadorRepartidor.bindTooltip('Tu ubicación actual');
      } else {
        this.marcadorRepartidor.setLatLng(currentPoint);
      }
    } else {
      this.marcadorRepartidor?.remove();
      this.marcadorRepartidor = null;
    }

    const entregasConCoords = this.entregas().filter(
      (e) => e.latitudEntrega != null && e.longitudEntrega != null
    );

    for (const entrega of entregasConCoords) {
      const point: [number, number] = [entrega.latitudEntrega!, entrega.longitudEntrega!];
      puntos.push(point);

      const marker = L.circleMarker(point, {
        radius: 6,
        color: '#0f172a',
        fillColor: this.colorEstadoMapa(entrega.estado),
        fillOpacity: 0.85,
        weight: 1.5
      }).addTo(this.mapa);

      marker.bindTooltip(`#${entrega.ordenEnRuta} · ${entrega.nombreDestinatario} (${entrega.estado})`);
      this.marcadoresEntrega.push(marker);
    }

    const seleccionada = this.entregaSeleccionada();
    if (seleccionada?.latitudEntrega != null && seleccionada.longitudEntrega != null) {
      const selectedPoint: [number, number] = [
        seleccionada.latitudEntrega,
        seleccionada.longitudEntrega
      ];
      puntos.push(selectedPoint);

      this.marcadorDestinoSeleccionado = L.circleMarker(selectedPoint, {
        radius: 10,
        color: '#7c2d12',
        fillColor: '#fb923c',
        fillOpacity: 0.8,
        weight: 2
      }).addTo(this.mapa);

      this.marcadorDestinoSeleccionado.bindTooltip('Parada seleccionada');
    }

    const historial = this.historialUbicaciones().map((p) => [p.latitud, p.longitud] as [number, number]);
    if (historial.length > 1) {
      this.capaHistorial = L.polyline(historial, {
        color: '#0284c7',
        weight: 3,
        opacity: 0.75,
        dashArray: '6 8'
      }).addTo(this.mapa);

      puntos.push(...historial);
    }

    if (puntos.length === 0) {
      this.mapa.setView(this.centroMapaDefecto, 6);
      return;
    }

    if (puntos.length === 1) {
      this.mapa.setView(puntos[0], 15);
      return;
    }

    this.mapa.fitBounds(L.latLngBounds(puntos), {
      padding: [24, 24],
      maxZoom: 16
    });
  }

  private colorEstadoMapa(estado: string): string {
    if (estado === 'Entregado' || estado === 'EntregadoPuntoAlternativo') return '#16a34a';
    if (estado === 'Ausente' || estado === 'DireccionIncorrecta' || estado === 'Rechazado') return '#dc2626';
    if (estado === 'EnCamino') return '#2563eb';
    return '#64748b';
  }

  private abrirNavegacion(entrega: EntregaPaquete, navegador: NavegadorExterno): void {
    const direccion = `${entrega.direccionEntrega}, ${entrega.codigoPostal} ${entrega.ciudad}`.trim();
    const encoded = encodeURIComponent(direccion);

    let url = '';
    if (navegador === 'waze') {
      if (entrega.latitudEntrega != null && entrega.longitudEntrega != null) {
        url = `https://waze.com/ul?ll=${entrega.latitudEntrega},${entrega.longitudEntrega}&navigate=yes`;
      } else {
        url = `https://waze.com/ul?q=${encoded}&navigate=yes`;
      }
    } else {
      if (entrega.latitudEntrega != null && entrega.longitudEntrega != null) {
        url = `https://www.google.com/maps/dir/?api=1&destination=${entrega.latitudEntrega},${entrega.longitudEntrega}&travelmode=driving`;
      } else {
        url = `https://www.google.com/maps/dir/?api=1&destination=${encoded}&travelmode=driving`;
      }
    }

    window.open(url, '_blank', 'noopener,noreferrer');
  }

  private calcularDistanciaKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const toRad = (value: number) => (value * Math.PI) / 180;
    const radioTierraKm = 6371;

    const dLat = toRad(lat2 - lat1);
    const dLon = toRad(lon2 - lon1);

    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) * Math.sin(dLon / 2);

    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return radioTierraKm * c;
  }

  private reemplazarEntrega(actualizada: EntregaPaquete): void {
    this.entregas.update(list => list.map(e => (e.id === actualizada.id ? actualizada : e)));
    this.actualizarMapa();
  }

  private limpiarFormularioConfirmacion(): void {
    this.estadoSeleccionado = 'Entregado';
    this.receptorNombre = '';
    this.receptorDni = '';
    this.observaciones = '';
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

  private readonly onVisibilityChange = () => {
    const hidden = document.visibilityState === 'hidden';
    this.seguimientoSegundoPlano.set(hidden);

    const ruta = this.ruta();
    if (ruta?.estado === 'EnCurso' && this.gpsActivo()) {
      this.activarWatchGps(ruta.id);
      this.programarHeartbeatGps(ruta.id);
      this.enviarUbicacion(ruta.id, hidden ? 'manual' : 'foreground', true);
    }
  };
}
