import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { EnviosService, TrazabilidadResponse, EventoTrazabilidad } from '../../services/envios.service';
import {
  TrackingService,
  TrackingEstadoEvento,
  TrackingEntregaEvento,
  TrackingIncidenciaEvento,
  EstadoConexion
} from '../../services/tracking.service';

/**
 * Pasos del flujo logístico para la barra de progreso.
 * Cada paso tiene un estado asociado de la enum del backend.
 */
interface PasoLogistico {
  etiqueta: string;
  icono: string;
  estados: string[];
  activo: boolean;
  completado: boolean;
}

@Component({
  selector: 'app-tracking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tracking.component.html'
})
export class TrackingComponent implements OnInit, OnDestroy {
  /** Número de seguimiento introducido por el usuario */
  numeroSeguimiento = '';

  /** True cuando se ha iniciado una búsqueda */
  buscando = false;

  /** Datos de trazabilidad del servidor (HTTP) */
  trazabilidad: TrazabilidadResponse | null = null;

  /** Eventos recibidos en tiempo real (SignalR) */
  eventosRealtime: TrackingEstadoEvento[] = [];

  /** Estado de la conexión SignalR */
  estadoConexion: EstadoConexion = 'desconectado';

  /** Mensaje de error */
  error: string | null = null;

  /** Pasos de la barra de progreso */
  pasos: PasoLogistico[] = [
    { etiqueta: 'Recogido', icono: '📦', estados: ['Admitido', 'Recogido', 'AdmitidoOficina'], activo: false, completado: false },
    { etiqueta: 'En oficina origen', icono: '🏤', estados: ['EnOficinaOrigen', 'SalidaOficinaOrigen'], activo: false, completado: false },
    { etiqueta: 'En clasificación', icono: '🏭', estados: ['RecibidoCtaOrigen', 'EnClasificacion', 'Clasificado'], activo: false, completado: false },
    { etiqueta: 'En tránsito', icono: '🚚', estados: ['EnTransitoTerrestre', 'EnTransitoAereo', 'EnTransitoMaritimo', 'ExpedidoCtaOrigen'], activo: false, completado: false },
    { etiqueta: 'En CTA destino', icono: '🏭', estados: ['RecibidoCtaDestino', 'ClasificadoCtaDestino', 'ExpedidoCtaDestino'], activo: false, completado: false },
    { etiqueta: 'En oficina destino', icono: '🏤', estados: ['RecibidoOficinaDestino', 'DisponibleRecogida'], activo: false, completado: false },
    { etiqueta: 'En reparto', icono: '🛵', estados: ['EnReparto', 'IntentadoEntrega', 'EnRepartoSegundoIntento'], activo: false, completado: false },
    { etiqueta: 'Entregado', icono: '✅', estados: ['Entregado', 'EntregadoEnOficina', 'RecogidoPorDestinatario'], activo: false, completado: false }
  ];

  private subs: Subscription[] = [];

  constructor(
    private enviosService: EnviosService,
    private trackingService: TrackingService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Suscribirse a eventos SignalR
    this.subs.push(
      this.trackingService.estadoConexion$.subscribe(e => this.estadoConexion = e),
      this.trackingService.estadoActualizado$.subscribe(e => this.onEstadoActualizado(e)),
      this.trackingService.entregaCompletada$.subscribe(e => this.onEntregaCompletada(e)),
      this.trackingService.incidenciaDetectada$.subscribe(e => this.onIncidencia(e))
    );

    // Si viene un número de seguimiento en la URL (queryParam o ruta)
    this.route.queryParamMap.subscribe(params => {
      const ns = params.get('ns');
      if (ns) {
        this.numeroSeguimiento = ns;
        this.buscar();
      }
    });
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
    this.trackingService.desuscribir();
    this.trackingService.desconectar();
  }

  /**
   * Busca la trazabilidad por HTTP y se suscribe al tracking en tiempo real.
   */
  buscar(): void {
    if (!this.numeroSeguimiento.trim()) return;

    this.buscando = true;
    this.error = null;
    this.trazabilidad = null;
    this.eventosRealtime = [];
    this.resetPasos();

    // 1. Obtener historial existente por HTTP
    this.enviosService.obtenerTrazabilidad(this.numeroSeguimiento).subscribe({
      next: (resp) => {
        this.trazabilidad = resp;
        this.actualizarBarraProgreso(resp.estadoActual);
        this.buscando = false;

        // 2. Suscribirse a actualizaciones en tiempo real
        this.trackingService.conectar();
        this.trackingService.suscribir(this.numeroSeguimiento);
      },
      error: (err) => {
        this.buscando = false;
        if (err.status === 404) {
          this.error = 'No se encontró ningún envío con ese número de seguimiento.';
        } else {
          this.error = 'Error al consultar el seguimiento. Inténtelo de nuevo.';
        }
      }
    });
  }

  // ─── Handlers de eventos en tiempo real ───

  private onEstadoActualizado(evento: TrackingEstadoEvento): void {
    this.eventosRealtime.unshift(evento); // Más reciente primero
    this.actualizarBarraProgreso(evento.estado);

    // Actualizar el estado actual de la trazabilidad
    if (this.trazabilidad) {
      this.trazabilidad.estadoActual = evento.estado;
    }
  }

  private onEntregaCompletada(evento: TrackingEntregaEvento): void {
    // Marcar todos los pasos como completados
    this.pasos.forEach(p => { p.completado = true; p.activo = false; });
    this.pasos[this.pasos.length - 1].activo = true;

    if (this.trazabilidad) {
      this.trazabilidad.estadoActual = 'Entregado';
      this.trazabilidad.fechaEntrega = evento.fechaEntrega;
    }
  }

  private onIncidencia(evento: TrackingIncidenciaEvento): void {
    this.eventosRealtime.unshift({
      numeroSeguimiento: evento.numeroSeguimiento,
      estado: 'Incidencia',
      estadoAnterior: '',
      ubicacion: '',
      descripcion: `⚠️ ${evento.tipo}: ${evento.descripcion}`,
      fecha: evento.fecha,
      visibleParaCliente: true
    });
  }

  // ─── Barra de progreso ───

  private actualizarBarraProgreso(estadoActual: string): void {
    let encontrado = false;

    for (let i = this.pasos.length - 1; i >= 0; i--) {
      const paso = this.pasos[i];
      if (!encontrado && paso.estados.includes(estadoActual)) {
        paso.activo = true;
        paso.completado = false;
        encontrado = true;
      } else if (encontrado) {
        paso.activo = false;
        paso.completado = true;
      } else {
        paso.activo = false;
        paso.completado = false;
      }
    }
  }

  private resetPasos(): void {
    this.pasos.forEach(p => { p.activo = false; p.completado = false; });
  }

  /** Calcula el porcentaje de progreso para la barra visual */
  get progresoPercent(): number {
    const completados = this.pasos.filter(p => p.completado).length;
    const activo = this.pasos.findIndex(p => p.activo);
    const total = this.pasos.length;
    if (activo === -1 && completados === 0) return 0;
    return Math.round(((completados + (activo >= 0 ? 0.5 : 0)) / total) * 100);
  }

  /** Traduce el estado técnico a un texto legible */
  traducirEstado(estado: string): string {
    const traducciones: Record<string, string> = {
      'Admitido': 'Paquete admitido',
      'Recogido': 'Recogido por repartidor',
      'AdmitidoOficina': 'Admitido en oficina',
      'EnOficinaOrigen': 'En oficina de origen',
      'SalidaOficinaOrigen': 'Ha salido de la oficina de origen',
      'RecibidoCtaOrigen': 'Recibido en centro de clasificación',
      'EnClasificacion': 'En proceso de clasificación',
      'Clasificado': 'Clasificado para envío',
      'EnTransitoTerrestre': 'En tránsito por carretera',
      'EnTransitoAereo': 'En tránsito aéreo',
      'EnTransitoMaritimo': 'En tránsito marítimo',
      'ExpedidoCtaOrigen': 'Expedido del centro de origen',
      'RecibidoCtaDestino': 'Recibido en centro de destino',
      'ClasificadoCtaDestino': 'Clasificado en destino',
      'ExpedidoCtaDestino': 'Expedido del centro de destino',
      'RecibidoOficinaDestino': 'Recibido en oficina de destino',
      'DisponibleRecogida': 'Disponible para recogida',
      'EnReparto': 'En reparto',
      'IntentadoEntrega': 'Intento de entrega',
      'Entregado': 'Entregado',
      'EntregadoEnOficina': 'Entregado en oficina',
      'RecogidoPorDestinatario': 'Recogido por destinatario',
      'Incidencia': 'Incidencia'
    };
    return traducciones[estado] || estado;
  }
}
