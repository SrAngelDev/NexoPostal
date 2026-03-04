import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { SignalrService } from '../../services/signalr.service';
import { IntranetApiService, MisCtasInfo, CtaAsignacion } from '../../services/intranet-api.service';
import { ScanService, ScanRequest, ScanResult, ModoEscaneo } from '../../services/scan.service';
import { BarcodeScannerComponent } from '../../components/barcode-scanner/barcode-scanner.component';

interface ScanHistoryItem {
  codigo: string;
  resultado: ScanResult;
  fecha: Date;
}

@Component({
  selector: 'app-escaneo',
  standalone: true,
  imports: [CommonModule, FormsModule, BarcodeScannerComponent],
  templateUrl: './escaneo.component.html',
  styleUrl: './escaneo.component.css'
})
export class EscaneoComponent implements OnInit {
  userName = '';
  userRole = '';

  // Estado
  loading = signal(true);
  error = signal('');
  procesando = signal(false);

  // CTA context
  misCtasInfo = signal<MisCtasInfo | null>(null);
  ctaSeleccionado = signal<CtaAsignacion | null>(null);

  // Modos de escaneo
  modos = signal<ModoEscaneo[]>([]);
  modoActivo = signal<string>('');

  // Último resultado
  ultimoResultado = signal<ScanResult | null>(null);

  // Historial de escaneos (sesión)
  historial = signal<ScanHistoryItem[]>([]);

  // Contexto adicional (oficina)
  oficinaJsonId = '';
  oficinaNombre = '';

  // Batch mode
  modoBatch = signal(false);
  codigosBatch = signal<string[]>([]);

  constructor(
    private authService: AuthService,
    private router: Router,
    public signalr: SignalrService,
    private intranetApi: IntranetApiService,
    private scanService: ScanService
  ) {
    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
    this.userRole = user?.rol ?? '';
  }

  ngOnInit(): void {
    this.signalr.conectar();
    this.cargarDatos();
  }

  cargarDatos(): void {
    this.loading.set(true);
    this.error.set('');

    // Cargar CTAs del operario y modos en paralelo
    this.intranetApi.obtenerMisCtas().subscribe({
      next: (info) => {
        this.misCtasInfo.set(info);
        if (info.ctas.length > 0) {
          this.ctaSeleccionado.set(info.ctas[0]);
        }
        this.cargarModos();
      },
      error: () => {
        this.error.set('No se pudieron cargar los datos del operario.');
        this.loading.set(false);
      }
    });
  }

  cargarModos(): void {
    this.scanService.obtenerModos().subscribe({
      next: (modos) => {
        this.modos.set(modos);
        if (modos.length > 0) {
          this.modoActivo.set(modos[0].valor);
        }
        this.loading.set(false);
      },
      error: () => {
        // Cargar modos estáticos como fallback
        this.modos.set([
          { valor: 'RecepcionCta', etiqueta: 'Recepción en CTA', icono: 'inventory_2', requiere: 'cta' },
          { valor: 'Clasificacion', etiqueta: 'Clasificación', icono: 'category', requiere: 'cta' },
          { valor: 'DespachoTroncal', etiqueta: 'Despacho troncal', icono: 'local_shipping', requiere: 'cta' },
          { valor: 'RecepcionTroncal', etiqueta: 'Recepción troncal', icono: 'move_to_inbox', requiere: 'cta' },
          { valor: 'RecepcionOficina', etiqueta: 'Recepción oficina', icono: 'store', requiere: 'oficina' },
          { valor: 'EntregaOficinaDestino', etiqueta: 'Entrega a oficina destino', icono: 'markunread_mailbox', requiere: 'oficina' },
          { valor: 'SalidaAReparto', etiqueta: 'Salida a reparto', icono: 'directions_bike', requiere: 'oficina' }
        ]);
        this.modoActivo.set('RecepcionCta');
        this.loading.set(false);
      }
    });
  }

  onCtaChange(ctaId: number): void {
    const info = this.misCtasInfo();
    if (info) {
      const cta = info.ctas.find(c => c.ctaId === +ctaId);
      if (cta) this.ctaSeleccionado.set(cta);
    }
  }

  seleccionarModo(modo: string): void {
    this.modoActivo.set(modo);
    this.ultimoResultado.set(null);
  }

  getModoInfo(): ModoEscaneo | undefined {
    return this.modos().find(m => m.valor === this.modoActivo());
  }

  requiereOficina(): boolean {
    const modo = this.getModoInfo();
    return modo?.requiere === 'oficina';
  }

  requiereCta(): boolean {
    const modo = this.getModoInfo();
    return modo?.requiere === 'cta';
  }

  // ─── Procesar escaneo ───

  onCodigoDetectado(codigo: string): void {
    if (this.procesando()) return;

    if (this.modoBatch()) {
      this.agregarABatch(codigo);
      return;
    }

    this.procesarCodigo(codigo);
  }

  procesarCodigo(codigo: string): void {
    if (!this.modoActivo()) return;

    this.procesando.set(true);
    this.ultimoResultado.set(null);

    const cta = this.ctaSeleccionado();
    const request: ScanRequest = {
      codigoEscaneado: codigo,
      modoOperacion: this.modoActivo(),
      ctaId: cta?.ctaId,
      ctaCodigo: cta?.ctaCodigo,
      operarioNombre: this.userName
    };

    // Si el modo requiere oficina
    if (this.requiereOficina()) {
      if (this.oficinaJsonId) request.oficinaJsonId = +this.oficinaJsonId;
      if (this.oficinaNombre) request.oficinaNombre = this.oficinaNombre;
    }

    this.scanService.procesar(request).subscribe({
      next: (resultado) => {
        this.ultimoResultado.set(resultado);
        this.historial.update(h => [{
          codigo,
          resultado,
          fecha: new Date()
        }, ...h].slice(0, 50)); // máximo 50 entradas
        this.procesando.set(false);
      },
      error: (err) => {
        const errorResult: ScanResult = {
          exito: false,
          numeroExpedicion: codigo,
          modoOperacion: this.modoActivo(),
          modoDescripcion: '',
          estadoNuevo: '',
          mensaje: err.error?.mensaje || err.error?.title || 'Error al procesar el escaneo',
          fechaProcesado: new Date().toISOString(),
          movimientoTroncalCreado: false,
          notificacionEnviada: false
        };
        this.ultimoResultado.set(errorResult);
        this.historial.update(h => [{
          codigo,
          resultado: errorResult,
          fecha: new Date()
        }, ...h].slice(0, 50));
        this.procesando.set(false);
      }
    });
  }

  // ─── Batch mode ───

  toggleBatch(): void {
    this.modoBatch.update(v => !v);
    if (!this.modoBatch()) {
      this.codigosBatch.set([]);
    }
  }

  agregarABatch(codigo: string): void {
    if (!this.codigosBatch().includes(codigo)) {
      this.codigosBatch.update(list => [...list, codigo]);
    }
  }

  eliminarDeBatch(codigo: string): void {
    this.codigosBatch.update(list => list.filter(c => c !== codigo));
  }

  procesarBatch(): void {
    const codigos = this.codigosBatch();
    if (codigos.length === 0) return;

    this.procesando.set(true);
    const cta = this.ctaSeleccionado();

    this.scanService.procesarLote({
      codigosEscaneados: codigos,
      modoOperacion: this.modoActivo(),
      ctaId: cta?.ctaId,
      ctaCodigo: cta?.ctaCodigo,
      operarioNombre: this.userName,
      oficinaJsonId: this.requiereOficina() && this.oficinaJsonId ? +this.oficinaJsonId : undefined,
      oficinaNombre: this.requiereOficina() ? this.oficinaNombre : undefined
    }).subscribe({
      next: (batchResult) => {
        for (const r of batchResult.resultados) {
          this.historial.update(h => [{
            codigo: r.numeroExpedicion,
            resultado: r,
            fecha: new Date()
          }, ...h].slice(0, 50));
        }
        this.codigosBatch.set([]);
        this.modoBatch.set(false);
        this.procesando.set(false);
      },
      error: () => {
        this.procesando.set(false);
      }
    });
  }

  // ─── Helpers ───

  formatHora(fecha: Date | string): string {
    const d = typeof fecha === 'string' ? new Date(fecha) : fecha;
    return d.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }

  limpiarHistorial(): void {
    this.historial.set([]);
  }

  contarExitosos(): number {
    return this.historial().filter(h => h.resultado.exito).length;
  }

  contarFallidos(): number {
    return this.historial().filter(h => !h.resultado.exito).length;
  }

  volverDashboard(): void {
    this.router.navigate(['/']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
