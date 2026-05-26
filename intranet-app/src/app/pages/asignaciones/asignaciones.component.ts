import { Component, OnInit, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import {
  IntranetApiService,
  MisCtasInfo,
  CtaAsignacion,
  MiOficinaInfo,
  AsignacionResumen,
  OperarioResumen,
  CrearAsignacionRequest
} from '../../services/intranet-api.service';
import { ScanService, ScanResult } from '../../services/scan.service';
import { SignalrService } from '../../services/signalr.service';
import { BarcodeScannerComponent } from '../../components/barcode-scanner/barcode-scanner.component';
import { IntranetNavbarComponent } from '../../components/intranet-navbar/intranet-navbar.component';

@Component({
  selector: 'app-asignaciones',
  standalone: true,
  imports: [CommonModule, FormsModule, BarcodeScannerComponent, IntranetNavbarComponent],
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

  /** Solo Admin y Supervisor pueden cancelar/reasignar tareas (gestión administrativa). */
  get puedeGestionarTareas(): boolean {
    return this.userRole === 'Admin' || this.userRole === 'Supervisor';
  }

  misCtasInfo = signal<MisCtasInfo | null>(null);
  ctaSeleccionado = signal<CtaAsignacion | null>(null);
  miOficinaInfo = signal<MiOficinaInfo | null>(null);
  asignaciones = signal<AsignacionResumen[]>([]);
  operarios = signal<OperarioResumen[]>([]);
  loading = signal(true);
  error = signal('');

  /** True si el usuario autenticado es OperarioOficina (sin CTA asignado). */
  get esOperarioOficina(): boolean {
    return this.userRole === 'OperarioOficina';
  }

  // Filtro
  filtroEstado = '';

  /** IDs de asignaciones completadas que el usuario ha "limpiado" (oculto). */
  completadasOcultas = signal<Set<number>>(new Set<number>());

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

  // Modal reasignar
  showReasignarModal = signal(false);
  tareaAReasignar = signal<AsignacionResumen | null>(null);
  nuevoOperarioReasignarId = 0;
  reasignando = signal(false);
  reasignarError = signal('');

  // Cancelación en curso (por id)
  cancelandoId = signal<number | null>(null);

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
      if (!ultima) return;
      if (!this.eventosConRefresco.has(ultima.tipo)) return;

      const claveEvento = `${ultima.tipo}|${ultima.numeroExpedicion ?? ''}|${ultima.fechaHora}`;
      if (claveEvento === this.ultimoEventoProcesado) return;

      if (this.esOperarioOficina) {
        this.ultimoEventoProcesado = claveEvento;
        this.cargarTareasOficina();
        return;
      }

      const cta = this.ctaSeleccionado();
      if (!cta) return;
      if (ultima.ctaId !== cta.ctaId) return;

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

    if (this.esOperarioOficina) {
      this.cargarDatosOficina();
      return;
    }

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

  /** Carga inicial para OperarioOficina: info de oficina + sus tareas. */
  private cargarDatosOficina(): void {
    this.intranetApi.obtenerMiOficina().subscribe({
      next: (info) => {
        this.miOficinaInfo.set(info);
        this.cargarTareasOficina();
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.status === 404
          ? 'No estás asignado a ninguna oficina.'
          : 'Error al cargar tu oficina.');
      }
    });
  }

  /** Refresca pendientes + en progreso + completadas recientes para OperarioOficina. */
  private cargarTareasOficina(): void {
    this.loading.set(true);
    let recibidos = 0;
    let pendientes: AsignacionResumen[] = [];
    let enProgreso: AsignacionResumen[] = [];
    let completadas: AsignacionResumen[] = [];

    const fusionar = () => {
      recibidos++;
      if (recibidos < 3) return;
      // Filtrar completadas "limpiadas" localmente.
      const ocultas = this.completadasOcultas();
      const completadasVisibles = completadas.filter(a => !ocultas.has(a.id));
      this.asignaciones.set([...pendientes, ...enProgreso, ...completadasVisibles]);
      this.loading.set(false);
    };

    this.intranetApi.obtenerMisPendientes().subscribe({
      next: (xs) => { pendientes = xs; fusionar(); },
      error: () => { pendientes = []; fusionar(); }
    });
    this.intranetApi.obtenerMisEnProgreso().subscribe({
      next: (xs) => { enProgreso = xs; fusionar(); },
      error: () => { enProgreso = []; fusionar(); }
    });
    this.intranetApi.obtenerMisCompletadas().subscribe({
      next: (xs) => { completadas = xs; fusionar(); },
      error: () => { completadas = []; fusionar(); }
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
    const ocultas = this.completadasOcultas();
    const todas = this.asignaciones().filter(a => !ocultas.has(a.id));
    if (!this.filtroEstado) return todas;
    return todas.filter(a => a.estadoTarea === this.filtroEstado);
  }

  /** Número de tareas completadas visibles (para mostrar el botón Limpiar). */
  get tieneCompletadasVisibles(): boolean {
    const ocultas = this.completadasOcultas();
    return this.asignaciones().some(a => a.estadoTarea === 'Completada' && !ocultas.has(a.id));
  }

  /** Oculta localmente las tareas completadas (no las borra del backend). */
  limpiarCompletadas(): void {
    const completadas = this.asignaciones().filter(a => a.estadoTarea === 'Completada');
    const nuevas = new Set(this.completadasOcultas());
    for (const a of completadas) nuevas.add(a.id);
    this.completadasOcultas.set(nuevas);
  }

  /** Refresca la vista actual según el rol. */
  refrescar(): void {
    if (this.esOperarioOficina) {
      this.cargarTareasOficina();
      return;
    }
    const cta = this.ctaSeleccionado();
    if (cta) this.cargarAsignaciones(cta.ctaId);
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
    const oficina = this.miOficinaInfo();
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
      oficinaJsonId: oficina?.oficinaJsonId,
      oficinaNombre: oficina?.oficinaNombre,
      operarioNombre: this.userName,
      esUrgente: tarea.esUrgente
    }).subscribe({
      next: (res: ScanResult) => {
        this.buscando.set(false);
        if (res.exito) {
          this.scanOk.set(`✔ ${res.mensaje || tarea.numeroExpedicion + ' procesado'}`);
          this.codigoBusqueda = '';
          if (cta) this.cargarAsignaciones(cta.ctaId);
          else if (this.esOperarioOficina) this.cargarTareasOficina();
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

  // ─── Gestión administrativa: Cancelar / Reasignar ───

  /** True si la tarea puede gestionarse (no terminal). */
  puedeGestionar(tarea: AsignacionResumen): boolean {
    return this.puedeGestionarTareas
      && tarea.estadoTarea !== 'Completada'
      && tarea.estadoTarea !== 'Cancelada';
  }

  cancelarTarea(tarea: AsignacionResumen): void {
    if (!this.puedeGestionar(tarea)) return;
    const ok = confirm(`¿Cancelar la tarea ${this.getTipoTareaLabel(tarea.tipoTarea)} del paquete ${tarea.numeroExpedicion}?`);
    if (!ok) return;

    this.cancelandoId.set(tarea.id);
    this.intranetApi.cancelarTarea(tarea.id).subscribe({
      next: () => {
        this.cancelandoId.set(null);
        this.refrescar();
      },
      error: (err) => {
        this.cancelandoId.set(null);
        alert(err.error?.message || 'No se pudo cancelar la tarea');
      }
    });
  }

  abrirReasignar(tarea: AsignacionResumen): void {
    if (!this.puedeGestionar(tarea)) return;
    this.tareaAReasignar.set(tarea);
    this.nuevoOperarioReasignarId = 0;
    this.reasignarError.set('');
    // Asegurar que tenemos la lista de operarios del CTA cargada
    const cta = this.ctaSeleccionado();
    if (cta && this.operarios().length === 0) {
      this.cargarOperarios(cta.ctaId);
    }
    this.showReasignarModal.set(true);
  }

  cerrarReasignar(): void {
    this.showReasignarModal.set(false);
    this.tareaAReasignar.set(null);
    this.reasignarError.set('');
  }

  /** Lista de operarios CTA candidatos (excluye Supervisor y al operario actualmente asignado). */
  get operariosReasignarCandidatos(): OperarioResumen[] {
    const tarea = this.tareaAReasignar();
    return this.operarios().filter(o =>
      o.rol === 'OperarioCTA' &&
      (!tarea || o.id !== tarea.operarioAsignadoId)
    );
  }

  confirmarReasignar(): void {
    const tarea = this.tareaAReasignar();
    if (!tarea) return;
    const nuevoId = +this.nuevoOperarioReasignarId;
    if (!nuevoId) {
      this.reasignarError.set('Selecciona un operario destino');
      return;
    }
    this.reasignando.set(true);
    this.reasignarError.set('');

    this.intranetApi.reasignarTarea(tarea.id, nuevoId).subscribe({
      next: () => {
        this.reasignando.set(false);
        this.showReasignarModal.set(false);
        this.tareaAReasignar.set(null);
        this.refrescar();
      },
      error: (err) => {
        this.reasignando.set(false);
        this.reasignarError.set(err.error?.message || 'No se pudo reasignar la tarea');
      }
    });
  }
}
