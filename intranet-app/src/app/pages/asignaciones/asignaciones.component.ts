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
import { SignalrService } from '../../services/signalr.service';

@Component({
  selector: 'app-asignaciones',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './asignaciones.component.html',
  styleUrl: './asignaciones.component.css'
})
export class AsignacionesComponent implements OnInit {
  userName = '';
  userRole = '';

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

  // Acciones
  accionLoading = signal<number | null>(null);
  private ultimoEventoProcesado = '';

  private readonly eventosConRefresco = new Set([
    'PaqueteRecibidoEnCta',
    'TareaAsignada',
    'TareaIniciada',
    'TareaCompletada',
    'TareaCancelada',
    'MovimientoRecibido'
  ]);

  tiposTarea = [
    { valor: 'Recepcion', etiqueta: 'Recepción' },
    { valor: 'Clasificacion', etiqueta: 'Clasificación' },
    { valor: 'CargaTransporte', etiqueta: 'Carga en transporte' },
    { valor: 'DescargaTransporte', etiqueta: 'Descarga de transporte' },
    { valor: 'Expedicion', etiqueta: 'Expedición' }
  ];

  constructor(
    private authService: AuthService,
    private intranetApi: IntranetApiService,
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

  onCtaChange(ctaId: number): void {
    const info = this.misCtasInfo();
    if (!info) return;
    const cta = info.ctas.find(c => c.ctaId === ctaId);
    if (cta) this.seleccionarCta(cta);
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

  // ─── Acciones sobre tareas ───
  iniciarTarea(id: number): void {
    this.accionLoading.set(id);
    this.intranetApi.iniciarTarea(id).subscribe({
      next: () => {
        this.accionLoading.set(null);
        const cta = this.ctaSeleccionado();
        if (cta) this.cargarAsignaciones(cta.ctaId);
      },
      error: () => this.accionLoading.set(null)
    });
  }

  completarTarea(id: number): void {
    this.accionLoading.set(id);
    this.intranetApi.completarTarea(id).subscribe({
      next: () => {
        this.accionLoading.set(null);
        const cta = this.ctaSeleccionado();
        if (cta) this.cargarAsignaciones(cta.ctaId);
      },
      error: () => this.accionLoading.set(null)
    });
  }

  cancelarTarea(id: number): void {
    this.accionLoading.set(id);
    this.intranetApi.cancelarTarea(id).subscribe({
      next: () => {
        this.accionLoading.set(null);
        const cta = this.ctaSeleccionado();
        if (cta) this.cargarAsignaciones(cta.ctaId);
      },
      error: () => this.accionLoading.set(null)
    });
  }

  // ─── Navegación ───
  volverDashboard(): void {
    this.router.navigate(['/']);
  }

  irGestionCta(): void {
    this.router.navigate(['/gestion-cta']);
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
