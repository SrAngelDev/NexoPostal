import { Component, OnDestroy, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import * as L from 'leaflet';
import { RepartoService, RutaRepartoDetalle, EntregaPaquete, UbicacionActiva } from '../../services/reparto.service';
import { DriverNavbarComponent } from '../../components/driver-navbar/driver-navbar.component';

// Colores por estado de entrega
const ESTADO_COLORES: Record<string, string> = {
  Pendiente:                    '#1976d2',
  EnCamino:                     '#f57f17',
  Entregado:                    '#2e7d32',
  EntregadoPuntoAlternativo:    '#388e3c',
  Ausente:                      '#e65100',
  DireccionIncorrecta:          '#6a1b9a',
  Rechazado:                    '#b71c1c',
  DevueltoAOficina:             '#546e7a',
};

const ESTADO_LABELS: Record<string, string> = {
  Pendiente:                    'Pendiente',
  EnCamino:                     'En camino',
  Entregado:                    'Entregado',
  EntregadoPuntoAlternativo:    'Punto alternativo',
  Ausente:                      'Ausente',
  DireccionIncorrecta:          'Dir. incorrecta',
  Rechazado:                    'Rechazado',
  DevueltoAOficina:             'Devuelto oficina',
};

@Component({
  selector: 'app-detalle-ruta',
  standalone: true,
  imports: [CommonModule, DriverNavbarComponent],
  templateUrl: './detalle-ruta.component.html',
  styleUrl: './detalle-ruta.component.css'
})
export class DetalleRutaComponent implements OnInit, OnDestroy {
  private map: L.Map | null = null;
  private markersLayer: L.LayerGroup = L.layerGroup();
  private driverLayer: L.LayerGroup = L.layerGroup();
  private routeLayer: L.LayerGroup = L.layerGroup();
  private pollHandle: ReturnType<typeof setInterval> | null = null;
  private rutaEstadoPollHandle: ReturnType<typeof setInterval> | null = null;
  private geocodedCoords = new Map<number, { lat: number; lng: number }>();

  ruta = signal<RutaRepartoDetalle | null>(null);
  cargando = signal(true);
  error = signal<string | null>(null);
  ubicacionDriver = signal<UbicacionActiva | null>(null);
  entregaExpandida = signal<number | null>(null);
  geocodificando = signal(false);

  rutaId = 0;

  navSubtitle = computed(() => {
    const r = this.ruta();
    if (!r) return '';
    return `${r.codigo} · ${this.getEstadoLabel(r.estado)}`;
  });

  stats = computed(() => {
    const r = this.ruta();
    if (!r) return { total: 0, entregados: 0, pendientes: 0, fallidos: 0, enCamino: 0 };
    const entregas = r.entregas;
    return {
      total:      entregas.length,
      entregados: entregas.filter(e => e.estado === 'Entregado' || e.estado === 'EntregadoPuntoAlternativo').length,
      pendientes: entregas.filter(e => e.estado === 'Pendiente').length,
      enCamino:   entregas.filter(e => e.estado === 'EnCamino').length,
      fallidos:   entregas.filter(e => ['Ausente','DireccionIncorrecta','Rechazado','DevueltoAOficina'].includes(e.estado)).length,
    };
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private repartoService: RepartoService,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.rutaId = idParam ? +idParam : 0;

    this.repartoService.obtenerRutaDetalle(this.rutaId).subscribe({
      next: async (ruta) => {
        this.ruta.set(ruta);
        this.cargando.set(false);
        setTimeout(() => this.initMap(), 0);
        // Geocodificar paradas sin coordenadas (pendientes) y re-renderizar el mapa
        await this.geocodeAll(ruta.entregas);
      },
      error: () => {
        this.error.set('No se pudo cargar el detalle de la ruta.');
        this.cargando.set(false);
      }
    });
  }

  private initMap(): void {
    const ruta = this.ruta();
    if (!ruta) return;

    this.map = L.map('mapa-ruta', { center: [40.4168, -3.7038], zoom: 6 });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '© OpenStreetMap'
    }).addTo(this.map);

    this.markersLayer.addTo(this.map);
    this.driverLayer.addTo(this.map);
    this.routeLayer.addTo(this.map);

    this.renderEntregas(ruta.entregas);

    if (ruta.estado === 'EnCurso') {
      this.pollDriverLocation();
      this.pollHandle = setInterval(() => this.pollDriverLocation(), 15000);
      // Polling de estados de entregas: el supervisor ve cambios sin recargar
      this.rutaEstadoPollHandle = setInterval(() => this.pollRutaEstado(), 15000);
    }
  }

  /** Devuelve las coordenadas de display: reales si se entregó con GPS, o geocodificadas de la dirección. */
  private getDisplayCoords(entrega: EntregaPaquete): { lat: number; lng: number } | null {
    if (entrega.latitudEntrega != null && entrega.longitudEntrega != null) {
      return { lat: entrega.latitudEntrega, lng: entrega.longitudEntrega };
    }
    return this.geocodedCoords.get(entrega.id) ?? null;
  }

  /**
   * Geocodifica mediante Nominatim las entregas que no tienen coordenadas.
   * Las coords reales (LatitudEntrega) se graban solo al confirmar la entrega;
   * las pendientes necesitan geocodificación de la dirección para mostrarse en el mapa.
   */
  private async geocodeAll(entregas: EntregaPaquete[]): Promise<void> {
    const sinCoords = entregas.filter(
      e => e.latitudEntrega == null && !this.geocodedCoords.has(e.id)
    );
    if (sinCoords.length === 0) return;

    this.geocodificando.set(true);
    try {
      for (const entrega of sinCoords) {
        // Eliminamos piso/puerta (3º 2ª, bajos, ático…) — Nominatim solo conoce calle+número.
        // DireccionEntrega suele ser "Calle X, N, Piso Puerta" → tomamos los 2 primeros segmentos.
        const partes = entrega.direccionEntrega.split(',');
        const calleNumero = partes.slice(0, 2).map(p => p.trim()).join(', ');
        const q = `${calleNumero}, ${entrega.ciudad}, España`;
        try {
          let results = await firstValueFrom(
            this.http.get<any[]>('/api/nexopostal/nominatim/search', {
              params: { q, format: 'json', limit: '1', countrycodes: 'es' }
            })
          );
          // Fallback: si la calle no se encuentra, intentar solo con CP + ciudad
          if (!results?.length && entrega.codigoPostal) {
            const qFallback = `${entrega.codigoPostal}, ${entrega.ciudad}, España`;
            results = await firstValueFrom(
              this.http.get<any[]>('/api/nexopostal/nominatim/search', {
                params: { q: qFallback, format: 'json', limit: '1', countrycodes: 'es' }
              })
            );
          }
          if (results?.length) {
            this.geocodedCoords.set(entrega.id, {
              lat: parseFloat(results[0].lat),
              lng: parseFloat(results[0].lon)
            });
            // Renderizar progresivamente al ir obteniendo coordenadas
            this.renderEntregas(this.ruta()!.entregas);
          }
        } catch {
          // Ignorar fallos individuales (dirección no encontrada)
        }
        // Respetar el rate-limit de Nominatim: máx 1 petición/segundo
        await new Promise(r => setTimeout(r, 1100));
      }
    } finally {
      this.geocodificando.set(false);
    }
  }

  private renderEntregas(entregas: EntregaPaquete[]): void {
    if (!this.map) return;
    this.markersLayer.clearLayers();
    this.routeLayer.clearLayers();

    const ordenadas = [...entregas]
      .filter(e => this.getDisplayCoords(e) != null)
      .sort((a, b) => a.ordenEnRuta - b.ordenEnRuta);

    if (ordenadas.length === 0) return;

    const polylinePoints: L.LatLngExpression[] = [];

    for (const entrega of ordenadas) {
      const coords = this.getDisplayCoords(entrega)!;
      const { lat, lng } = coords;
      const esAproximado = entrega.latitudEntrega == null; // geocodificado, no confirmado
      polylinePoints.push([lat, lng]);

      const color = ESTADO_COLORES[entrega.estado] ?? '#607d8b';
      const orden = entrega.ordenEnRuta;

      const icon = L.divIcon({
        className: '',
        html: `<div class="map-marker${esAproximado ? ' map-marker-approx' : ''}" style="background:${color}">${orden}</div>`,
        iconSize: [28, 28],
        iconAnchor: [14, 14],
        popupAnchor: [0, -16]
      });

      const label = ESTADO_LABELS[entrega.estado] ?? entrega.estado;
      const aproxTag = esAproximado ? '<br><small style="color:#888">📍 Posición aproximada</small>' : '';
      const popup = `
        <div class="popup-entrega">
          <strong>#${orden} — ${entrega.nombreDestinatario}</strong><br>
          ${entrega.direccionEntrega}, ${entrega.ciudad}<br>
          <span style="color:${color};font-weight:600">${label}</span>
          ${aproxTag}
          ${entrega.receptorNombre ? `<br><small>Firmado: ${entrega.receptorNombre}</small>` : ''}
        </div>`;

      L.marker([lat, lng], { icon })
        .bindPopup(popup)
        .addTo(this.markersLayer);
    }

    // Trazar la línea de ruta
    L.polyline(polylinePoints, {
      color: '#1976d2',
      weight: 3,
      opacity: 0.75,
      dashArray: '6,4'
    }).addTo(this.routeLayer);

    // Ajustar el mapa para mostrar todos los puntos
    const bounds = L.latLngBounds(polylinePoints);
    this.map.fitBounds(bounds, { padding: [32, 32] });
  }

  /** Re-fetches route details to update delivery states reactively. */
  private pollRutaEstado(): void {
    this.repartoService.obtenerRutaDetalle(this.rutaId).subscribe({
      next: (ruta) => {
        this.ruta.set(ruta);
        this.renderEntregas(ruta.entregas);
      }
    });
  }

  private pollDriverLocation(): void {
    const ruta = this.ruta();
    if (!ruta) return;

    this.repartoService.obtenerUbicacionesActivas(ruta.oficinaOrigenJsonId, 5).subscribe({
      next: (ubicaciones) => {
        const driver = ubicaciones.find(u => u.rutaActivaId === ruta.id);
        this.ubicacionDriver.set(driver ?? null);
        this.renderDriverMarker(driver);
      }
    });
  }

  private renderDriverMarker(driver: UbicacionActiva | undefined): void {
    if (!this.map) return;
    this.driverLayer.clearLayers();
    if (!driver) return;

    const icon = L.divIcon({
      className: '',
      html: `<div class="map-driver-marker"><span class="material-symbols-outlined">local_shipping</span></div>`,
      iconSize: [36, 36],
      iconAnchor: [18, 18],
      popupAnchor: [0, -20]
    });

    const hace = Math.floor(driver.segundosDesdeActualizacion / 60);
    const haceTxt = hace === 0 ? 'ahora mismo' : `hace ${hace} min`;

    L.marker([driver.latitud, driver.longitud], { icon })
      .bindPopup(`<strong>${driver.nombreRepartidor}</strong><br>Actualizado ${haceTxt}`)
      .addTo(this.driverLayer);
  }

  toggleEntrega(id: number): void {
    this.entregaExpandida.set(this.entregaExpandida() === id ? null : id);
  }

  centrarEnEntrega(entrega: EntregaPaquete): void {
    const coords = this.getDisplayCoords(entrega);
    if (!this.map || !coords) return;
    this.map.setView([coords.lat, coords.lng], 16);
  }

  volver(): void {
    this.router.navigate(['/gestion-rutas']);
  }

  getEstadoClass(estado: string): string {
    const map: Record<string, string> = {
      Planificada: 'estado-planificada',
      EnCurso: 'estado-en-curso',
      Completada: 'estado-completada',
      CompletadaParcial: 'estado-completada-parcial',
      Cancelada: 'estado-cancelada',
    };
    return map[estado] ?? '';
  }

  getEstadoLabel(estado: string): string {
    const map: Record<string, string> = {
      EnCurso: 'En curso',
      CompletadaParcial: 'Completada parcial',
    };
    return map[estado] ?? estado;
  }

  getEntregaClass(estado: string): string {
    const map: Record<string, string> = {
      Entregado: 'e-entregado',
      EntregadoPuntoAlternativo: 'e-entregado',
      Pendiente: 'e-pendiente',
      EnCamino: 'e-camino',
      Ausente: 'e-fallido',
      DireccionIncorrecta: 'e-fallido',
      Rechazado: 'e-fallido',
      DevueltoAOficina: 'e-devuelto',
    };
    return map[estado] ?? '';
  }

  getEntregaLabel(estado: string): string {
    return ESTADO_LABELS[estado] ?? estado;
  }

  tieneUbicacion(e: EntregaPaquete): boolean {
    return this.getDisplayCoords(e) != null;
  }

  ngOnDestroy(): void {
    if (this.pollHandle) clearInterval(this.pollHandle);
    if (this.rutaEstadoPollHandle) clearInterval(this.rutaEstadoPollHandle);
    this.map?.remove();
  }
}
