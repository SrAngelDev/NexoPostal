import { Component, OnInit, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import {
  IntranetApiService,
  MisCtasInfo,
  CtaAsignacion,
  AsignacionResumen,
  OperarioResumen,
  CrearAsignacionRequest
} from '../../services/intranet-api.service';
import { ScanService, ScanResult } from '../../services/scan.service';
import { SignalrService } from '../../services/signalr.service';
import { BarcodeScannerComponent } from '../../components/barcode-scanner/barcode-scanner.component';

@Component({
  selector: 'app-asignaciones',
  standalone: true,
  imports: [CommonModule, FormsModule, BarcodeScannerComponent],
  templateUrl: './asignaciones.component.html',
  styleUrl: './asignaciones.component.css'
})
export class AsignacionesComponent implements OnInit {
  userName = '';
  userRole = '';

  /** Solo Admin y Supervisor pueden crear asignaciones manuales. */
  get puedeCrearAsignacion(): boolean {
    return this.userRole === 'Admin' || this.userRole === 'Supervisor';
  }

  misCtasInfo = signal<MisCtasInfo | null>(null);
  ctaSeleccionado = signal<CtaAsignacion | null>(null);
  asignaciones = signal<AsignacionResumen[]>([]);
  operarios = signal<OperarioResumen[]>([]);
  loading = signal(true);
  error = signal('');

  // Filtro
  filtroEstado = '';

  // Modal crear asignación
  showCrearModal = signal(false);
  nuevaAsignacion = {
    numeroExpedicion: '',
    operarioAsignadoId: 0,
    tipoTarea: '',
    esUrgente: false,
    observaciones: ''
  };
  creando = signal(false);
  crearError = signal('');

  // Buscador / escáner integrado
  codigoBusqueda = '';
  buscando = signal(false);
  scanError = signal('');
  scanOk = signal('');
  mostrarScanner = signal(false);

  // Modal "paquete fuera de tus tareas"
  fueraTareasVisible = signal(false);
  fueraTareasCodigo = signal('');
  fueraTareasMotivo = '';
  fueraTareasError = signal('');
  fueraTareasEnviando = signal(false);

  // Acciones
  private ultimoEventoProcesado = '';

  private readonly eventosConRefresco = new Set([
    'PaqueteRecibidoEnCta',
    'NuevoPaqueteEnOficina',
    'PaqueteDisponibleParaReparto',
    'TareaAsignada',
    'TareaCompletada',
    'TareaCancelada',
    'MovimientoRecibido'
  ]);

  tiposTarea = [
    { valor: 'Recepcion', etiqueta: 'Recepción en CTA' },
    { valor: 'Clasificacion', etiqueta: 'Clasificación' },
    { valor: 'CargaTransporte', etiqueta: 'Carga en transporte' },
    { valor: 'DescargaTransporte', etiqueta: 'Descarga de transporte' },
    { valor: 'Expedicion', etiqueta: 'Expedición' },
    { valor: 'RecepcionOficina', etiqueta: 'Recepción en oficina' },
    { valor: 'SalidaOficinaACta', etiqueta: 'Salida oficina → CTA' },
    { valor: 'DespachoTroncal', etiqueta: 'Despacho troncal' },
    { valor: 'RecepcionTroncal', etiqueta: 'Recepción troncal' },
    { valor: 'DisponibleParaReparto', etiqueta: 'Disponible para reparto' }
  ];

  constructor(
    private authService: AuthService,
    private intranetApi: IntranetApiService,
    private scanService: ScanService,
    public signalr: SignalrService,
    private router: Router
  ) {
    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
    this.userRole = user?.rol ?? '';

    effect(() => {
      const ultima = this.signalr.ultimaNotificacion();
      const cta = this.ctaSeleccionado();

      if (!ultima || !cta) return;
      if (ultima.ctaId !== cta.ctaId) return;
      if (!this.eventosConRefresco.has(ultima.tipo)) return;

      const claveEvento = `${ultima.tipo}|${ultima.numeroExpedicion ?? ''}|${ultima.fechaHora}`;
      if (claveEvento === this.ultimoEventoProcesado) return;

      this.ultimoEventoProcesado = claveEvento;
      this.cargarAsignaciones(cta.ctaId);
    });
  }

  ngOnInit(): void {
    this.signalr.conectar();
    this.cargarDatos();
  }

  cargarDatos(): void {
    this.loading.set(true);
    this.error.set('');

    this.intranetApi.obtenerMisCtas().subscribe({
      next: (info) => {
        this.misCtasInfo.set(info);
        if (info.ctas.length > 0) {
          this.seleccionarCta(info.ctas[0]);
        } else {
          this.loading.set(false);
          this.error.set('No estás asignado a ningún CTA.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.status === 404
          ? 'No estás asignado a ningún CTA.'
          : 'Error al cargar datos.');
      }
    });
  }

  seleccionarCta(cta: CtaAsignacion): void {
    this.ctaSeleccionado.set(cta);
    this.loading.set(true);
    this.cargarAsignaciones(cta.ctaId);
    this.cargarOperarios(cta.ctaId);
  }

  onCtaChange(ctaId: number | string): void {
    const info = this.misCtasInfo();
    if (!info) return;
    const id = typeof ctaId === 'string' ? Number(ctaId) : ctaId;
    if (Number.isNaN(id)) return;
    const cta = info.ctas.find(c => c.ctaId === id);
    if (cta && cta.ctaId !== this.ctaSeleccionado()?.ctaId) {
      this.seleccionarCta(cta);
    }
  }

  cargarAsignaciones(ctaId: number): void {
    this.intranetApi.obtenerAsignacionesCta(ctaId).subscribe({
      next: (asig) => {
        this.asignaciones.set(asig);
        this.loading.set(false);
      },
      error: () => {
        this.asignaciones.set([]);
        this.loading.set(false);
      }
    });
  }

  cargarOperarios(ctaId: number): void {
    this.intranetApi.obtenerOperariosCta(ctaId).subscribe({
      next: (ops) => this.operarios.set(ops.filter(o => o.activo)),
      error: () => this.operarios.set([])
    });
  }

  get asignacionesFiltradas(): AsignacionResumen[] {
    const todas = this.asignaciones();
    if (!this.filtroEstado) return todas;
    return todas.filter(a => a.estadoTarea === this.filtroEstado);
  }

  // ─── Modal Crear ───
  abrirCrearModal(): void {
    if (!this.puedeCrearAsignacion) return;
    this.nuevaAsignacion = {
      numeroExpedicion: '',
      operarioAsignadoId: 0,
      tipoTarea: '',
      esUrgente: false,
      observaciones: ''
    };
    this.crearError.set('');
    this.showCrearModal.set(true);
  }

  cerrarCrearModal(): void {
    this.showCrearModal.set(false);
  }

  crearAsignacion(): void {
    if (!this.nuevaAsignacion.numeroExpedicion || !this.nuevaAsignacion.operarioAsignadoId || !this.nuevaAsignacion.tipoTarea) {
      this.crearError.set('Completa todos los campos obligatorios');
      return;
    }

    this.creando.set(true);
    this.crearError.set('');

    const dto: CrearAsignacionRequest = {
      numeroExpedicion: this.nuevaAsignacion.numeroExpedicion.trim(),
      operarioAsignadoId: +this.nuevaAsignacion.operarioAsignadoId,
      tipoTarea: this.nuevaAsignacion.tipoTarea,
      esUrgente: this.nuevaAsignacion.esUrgente,
      observaciones: this.nuevaAsignacion.observaciones || undefined
    };

    this.intranetApi.crearAsignacion(dto).subscribe({
      next: () => {
        this.creando.set(false);
        this.showCrearModal.set(false);
        // Recargar asignaciones
        const cta = this.ctaSeleccionado();
        if (cta) this.cargarAsignaciones(cta.ctaId);
      },
      error: (err) => {
        this.creando.set(false);
        this.crearError.set(err.error?.message || 'Error al crear la asignación');
      }
    });
  }

  // ─── Navegación ───
  volverDashboard(): void {
    this.router.navigate(['/']);
  }

  irGestionCta(): void {
    this.router.navigate(['/gestion-cta']);
  }

  // ─── Buscador / escáner integrado ───

  abrirScanner(): void {
    this.scanError.set('');
    this.scanOk.set('');
    this.mostrarScanner.set(true);
  }

  cerrarScanner(): void {
    this.mostrarScanner.set(false);
  }

  onCodigoEscaneado(codigo: string): void {
    this.codigoBusqueda = codigo;
    this.mostrarScanner.set(false);
    this.buscarYConfirmar();
  }

  /**
   * Busca el código escaneado/tecleado en las tareas del operario.
   * Si existe → confirma directamente el paso (escaneo con modoSugerido).
   * Si no → abre modal bloqueante para reportar incidencia "PaqueteFueraDeTareas".
   */
  buscarYConfirmar(): void {
    const codigo = this.codigoBusqueda.trim();
    if (!codigo) {
      this.scanError.set('Introduce un código');
      return;
    }
    this.scanError.set('');
    this.scanOk.set('');
    this.buscando.set(true);

    this.intranetApi.buscarTareaPorCodigo(codigo).subscribe({
      next: (tarea) => {
        this.confirmarTarea(tarea);
      },
      error: (err) => {
        this.buscando.set(false);
        if (err.status === 404) {
          this.abrirFueraTareas(codigo);
        } else {
          this.scanError.set(err.error?.message || 'Error al buscar el código');
        }
      }
    });
  }

  /** Lanza el escaneo en backend con el modoSugerido de la tarea. */
  private confirmarTarea(tarea: AsignacionResumen): void {
    const cta = this.ctaSeleccionado();
    if (!tarea.modoSugerido) {
      this.buscando.set(false);
      this.scanError.set(`La tarea "${tarea.tipoTarea}" no tiene un modo de escaneo asociado`);
      return;
    }

    this.scanService.procesar({
      codigoEscaneado: tarea.numeroExpedicion,
      modoOperacion: tarea.modoSugerido,
      ctaId: cta?.ctaId,
      ctaCodigo: cta?.ctaCodigo,
      operarioNombre: this.userName,
      esUrgente: tarea.esUrgente
    }).subscribe({
      next: (res: ScanResult) => {
        this.buscando.set(false);
        if (res.exito) {
          this.scanOk.set(`✔ ${res.mensaje || tarea.numeroExpedicion + ' procesado'}`);
          this.codigoBusqueda = '';
          if (cta) this.cargarAsignaciones(cta.ctaId);
        } else {
          this.scanError.set(res.mensaje || 'El escaneo no se pudo completar');
        }
      },
      error: (err) => {
        this.buscando.set(false);
        this.scanError.set(err.error?.message || 'Error al procesar el escaneo');
      }
    });
  }

  // ─── Modal "fuera de tus tareas" ───

  private abrirFueraTareas(codigo: string): void {
    this.fueraTareasCodigo.set(codigo);
    this.fueraTareasMotivo = '';
    this.fueraTareasError.set('');
    this.fueraTareasVisible.set(true);
  }

  cerrarFueraTareas(): void {
    this.fueraTareasVisible.set(false);
  }

  reportarFueraTareas(): void {
    const motivo = (this.fueraTareasMotivo || '').trim();
    if (!motivo) {
      this.fueraTareasError.set('Indica el motivo');
      return;
    }
    this.fueraTareasEnviando.set(true);
    this.fueraTareasError.set('');

    this.intranetApi.reportarPaqueteFueraDeTareas({
      numeroExpedicion: this.fueraTareasCodigo(),
      motivo
    }).subscribe({
      next: () => {
        this.fueraTareasEnviando.set(false);
        this.fueraTareasVisible.set(false);
        this.scanOk.set('Incidencia reportada al supervisor');
        this.codigoBusqueda = '';
      },
      error: (err) => {
        this.fueraTareasEnviando.set(false);
        this.fueraTareasError.set(err.error?.message || 'No se pudo reportar la incidencia');
      }
    });
  }

  logout(): void {
    this.signalr.desconectar();
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  // ─── Helpers ───
  formatearFecha(fecha: string): string {
    if (!fecha) return '—';
    return new Date(fecha).toLocaleDateString('es-ES', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getEstadoClase(estado: string): string {
    const clases: Record<string, string> = {
      'Pendiente': 'estado-pendiente',
      'EnProgreso': 'estado-progreso',
      'Completada': 'estado-completada',
      'Cancelada': 'estado-cancelada'
    };
    return clases[estado] || 'estado-desconocido';
  }

  getEstadoLabel(estado: string): string {
    const labels: Record<string, string> = {
      'Pendiente': 'Pendiente',
      'EnProgreso': 'En progreso',
      'Completada': 'Completada',
      'Cancelada': 'Cancelada'
    };
    return labels[estado] || estado;
  }

  getTipoTareaLabel(tipo: string): string {
    const found = this.tiposTarea.find(t => t.valor === tipo);
    return found ? found.etiqueta : tipo;
  }
}
