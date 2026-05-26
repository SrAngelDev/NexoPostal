import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import * as L from 'leaflet';
import { RepartoService, UbicacionActiva } from '../../services/reparto.service';
import { AuthService } from '../../services/auth.service';
import { DriverNavbarComponent } from '../../components/driver-navbar/driver-navbar.component';

@Component({
  selector: 'app-mapa-tiempo-real',
  standalone: true,
  imports: [CommonModule, DriverNavbarComponent],
  templateUrl: './mapa-tiempo-real.component.html',
  styleUrl: './mapa-tiempo-real.component.css'
})
export class MapaTiempoRealComponent implements OnInit, OnDestroy {
  private map: L.Map | null = null;
  private markersLayer: L.LayerGroup = L.layerGroup();
  private pollHandle: ReturnType<typeof setInterval> | null = null;

  ubicaciones = signal<UbicacionActiva[]>([]);
  cargando = signal(false);
  error = signal<string | null>(null);
  ultimaActualizacion = signal<Date | null>(null);

  navSubtitle = computed(() => {
    const t = this.ultimaActualizacion();
    const base = `${this.ubicaciones().length} repartidor(es) activo(s)`;
    if (!t) return base;
    return `${base} · ${t.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}`;
  });

  constructor(
    private repartoService: RepartoService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Esperar al render para que el div #mapa exista en el DOM
    setTimeout(() => this.initMap(), 0);
  }

  private initMap(): void {
    this.map = L.map('mapa', {
      center: [40.4168, -3.7038], // Madrid como centro inicial
      zoom: 6
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '© OpenStreetMap'
    }).addTo(this.map);

    this.markersLayer.addTo(this.map);

    this.refrescar();
    this.pollHandle = setInterval(() => this.refrescar(), 15000);
  }

  refrescar(): void {
    this.cargando.set(true);
    this.repartoService.obtenerUbicacionesActivas(undefined, 10).subscribe({
      next: (data) => {
        this.ubicaciones.set(data);
        this.ultimaActualizacion.set(new Date());
        this.cargando.set(false);
        this.error.set(null);
        this.renderMarkers(data);
      },
      error: (err) => {
        this.error.set('No se pudieron cargar las ubicaciones.');
        this.cargando.set(false);
        console.error('Error obteniendo ubicaciones:', err);
      }
    });
  }

  private renderMarkers(ubicaciones: UbicacionActiva[]): void {
    if (!this.map) return;
    this.markersLayer.clearLayers();

    if (ubicaciones.length === 0) return;

    const icon = L.icon({
      iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
      iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
      shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      popupAnchor: [1, -34],
      shadowSize: [41, 41]
    });

    const latLngs: L.LatLngExpression[] = [];

    for (const u of ubicaciones) {
      const marker = L.marker([u.latitud, u.longitud], { icon });
      const segundos = u.segundosDesdeActualizacion;
      const minutos = Math.floor(segundos / 60);
      const tiempo = minutos > 0 ? `hace ${minutos} min` : `hace ${segundos}s`;
      const rutaInfo = u.rutaCodigo
        ? `<div><strong>Ruta:</strong> ${u.rutaCodigo} (${u.rutaEstado})</div>`
        : '<div><em>Sin ruta activa</em></div>';

      marker.bindPopup(`
        <div style="min-width: 180px;">
          <div style="font-weight: 600; color: #1565c0; margin-bottom: 4px;">
            ${this.escape(u.nombreRepartidor)}
          </div>
          <div><strong>Código:</strong> ${this.escape(u.codigoEmpleado)}</div>
          <div><strong>Oficina:</strong> ${this.escape(u.oficinaNombre)}</div>
          ${rutaInfo}
          <div style="margin-top: 6px; color: #666; font-size: 0.85em;">
            Actualizado ${tiempo}
          </div>
        </div>
      `);
      this.markersLayer.addLayer(marker);
      latLngs.push([u.latitud, u.longitud]);
    }

    if (latLngs.length === 1) {
      this.map.setView(latLngs[0], 14);
    } else if (latLngs.length > 1) {
      this.map.fitBounds(L.latLngBounds(latLngs), { padding: [40, 40], maxZoom: 14 });
    }
  }

  private escape(s: string | null | undefined): string {
    if (!s) return '';
    return s.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]!));
  }

  volver(): void {
    this.router.navigate(['/dashboard-jefe']);
  }

  ngOnDestroy(): void {
    if (this.pollHandle) clearInterval(this.pollHandle);
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }
}
