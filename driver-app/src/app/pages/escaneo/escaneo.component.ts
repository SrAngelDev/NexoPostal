import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import {
  EnviosInternoService,
  EnvioInternoDetallado,
  ESTADOS_REPARTIDOR,
  ActualizarEstadoInternoRequest
} from '../../services/envios-interno.service';
import { BarcodeScannerComponent } from '../../components/barcode-scanner/barcode-scanner.component';

interface HistorialEntrega {
  expedicion: string;
  accion: string;
  exito: boolean;
  mensaje: string;
  hora: Date;
}

@Component({
  selector: 'app-escaneo',
  standalone: true,
  imports: [CommonModule, FormsModule, BarcodeScannerComponent],
  templateUrl: './escaneo.component.html',
  styleUrl: './escaneo.component.css'
})
export class EscaneoComponent {
  userName = '';
  userRole = '';

  // Estado
  buscando = signal(false);
  actualizando = signal(false);
  error = signal('');
  mensaje = signal('');

  // Paquete escaneado
  paquete = signal<EnvioInternoDetallado | null>(null);

  // Acción seleccionada
  accionSeleccionada = '';
  observaciones = '';

  // Historial de sesión
  historial = signal<HistorialEntrega[]>([]);

  // Estados disponibles para el repartidor
  estadosRepartidor = ESTADOS_REPARTIDOR;

  constructor(
    private authService: AuthService,
    private router: Router,
    private enviosService: EnviosInternoService
  ) {
    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
    this.userRole = user?.rol ?? '';
  }

  // ─── Escaneo ───

  onCodigoDetectado(codigo: string): void {
    if (this.buscando()) return;

    this.buscando.set(true);
    this.error.set('');
    this.mensaje.set('');
    this.paquete.set(null);
    this.accionSeleccionada = '';
    this.observaciones = '';

    // Validar formato
    if (!this.enviosService.validarNumeroExpedicion(codigo)) {
      this.error.set(`Código inválido: "${codigo}". Se espera formato NXI-XXXXXXXX.`);
      this.buscando.set(false);
      return;
    }

    this.enviosService.obtenerPorExpedicion(codigo).subscribe({
      next: (envio) => {
        this.paquete.set(envio);
        this.buscando.set(false);
        // Pre-seleccionar acción según estado actual
        this.preseleccionarAccion(envio.estadoInterno);
      },
      error: (err) => {
        const msg = err.status === 404
          ? `No se encontró el envío ${codigo}`
          : 'Error al buscar el paquete. Comprueba la conexión.';
        this.error.set(msg);
        this.buscando.set(false);
      }
    });
  }

  preseleccionarAccion(estadoActual: string): void {
    // Lógica de auto-sugerencia por estado
    const sugerencias: Record<string, string> = {
      'EnReparto': 'EntregadoEnDomicilio',
      'DepositivoEnOficina': 'EntregadoEnOficina',
      'PrimerIntentoFallido': 'EntregadoEnDomicilio',
      'SegundoIntentoFallido': 'DepositivoEnOficina',
      'RecogidoEnOrigen': 'EnReparto',
      'RecibidoEnCentroDestino': 'EnReparto',
      'ClasificadoParaExpedicion': 'EnReparto'
    };
    this.accionSeleccionada = sugerencias[estadoActual] ?? '';
  }

  // ─── Acciones rápidas ───

  esEntregaFinal(valor: string): boolean {
    return ['EntregadoEnDomicilio', 'EntregadoEnOficina', 'EntregadoAAutorizado'].includes(valor);
  }

  esIncidencia(valor: string): boolean {
    return valor.startsWith('Incidencia');
  }

  // ─── Actualizar estado ───

  confirmarAccion(): void {
    const paq = this.paquete();
    if (!paq || !this.accionSeleccionada) return;

    this.actualizando.set(true);
    this.error.set('');
    this.mensaje.set('');

    const request: ActualizarEstadoInternoRequest = {
      nuevoEstadoInterno: this.accionSeleccionada,
      observaciones: this.observaciones || undefined
    };

    this.enviosService.actualizarEstado(paq.numeroExpedicion, request).subscribe({
      next: (envioActualizado) => {
        const etiqueta = this.enviosService.formatearEstadoInterno(this.accionSeleccionada);
        this.mensaje.set(`✓ ${paq.numeroExpedicion} → ${etiqueta}`);

        // Añadir al historial
        this.historial.update(h => [{
          expedicion: paq.numeroExpedicion,
          accion: etiqueta,
          exito: true,
          mensaje: `Estado actualizado a: ${etiqueta}`,
          hora: new Date()
        }, ...h].slice(0, 30));

        // Actualizar paquete
        this.paquete.set(envioActualizado);
        this.accionSeleccionada = '';
        this.observaciones = '';
        this.actualizando.set(false);
      },
      error: (err) => {
        const msg = err.error?.message || err.error?.title || 'No se pudo actualizar el estado.';
        this.error.set(msg);

        this.historial.update(h => [{
          expedicion: paq.numeroExpedicion,
          accion: this.accionSeleccionada,
          exito: false,
          mensaje: msg,
          hora: new Date()
        }, ...h].slice(0, 30));

        this.actualizando.set(false);
      }
    });
  }

  limpiarPaquete(): void {
    this.paquete.set(null);
    this.accionSeleccionada = '';
    this.observaciones = '';
    this.error.set('');
    this.mensaje.set('');
  }

  // ─── Helpers ───

  getEstadoClase(estado: string): string {
    if (estado.startsWith('Entregado')) return 'estado-entregado';
    if (estado.startsWith('Incidencia')) return 'estado-incidencia';
    if (estado === 'EnReparto') return 'estado-reparto';
    return 'estado-default';
  }

  formatHora(fecha: Date): string {
    return fecha.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }

  contarEntregas(): number {
    return this.historial().filter(h => h.exito && h.accion.includes('Entregado')).length;
  }

  volverDashboard(): void {
    this.router.navigate(['/']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
