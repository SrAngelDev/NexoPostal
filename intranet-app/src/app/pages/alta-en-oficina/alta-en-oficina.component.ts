import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import {
  AdmisionService,
  AltaEnvioOficinaRequest,
  AltaEnvioOficinaResponse,
  OficinaJsonItem
} from '../../services/admision.service';

@Component({
  selector: 'app-alta-en-oficina',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './alta-en-oficina.component.html',
  styleUrl: './alta-en-oficina.component.css'
})
export class AltaEnOficinaComponent {
  userName = '';

  // Paquete
  peso: number | null = null;
  largo: number | null = null;
  ancho: number | null = null;
  alto: number | null = null;

  // Remitente
  nombreRem = '';
  apellidosRem = '';
  origen = '';
  cpOrigen = '';
  telRem = '';
  emailRem = '';
  dniRem = '';

  // Destinatario
  nombreDest = '';
  apellidosDest = '';
  destino = '';
  cpDestino = '';
  telDest = '';
  emailDest = '';

  // Entrega
  tipoEntrega: 'Domicilio' | 'Oficina' = 'Domicilio';
  oficinasDestino = signal<OficinaJsonItem[]>([]);
  oficinaDestinoSeleccionada = signal<OficinaJsonItem | null>(null);
  buscandoOficinas = signal(false);

  metodoCobro = 'Efectivo';
  observaciones = '';

  // UI
  enviando = signal(false);
  error = signal<string>('');
  resultado = signal<AltaEnvioOficinaResponse | null>(null);

  constructor(
    private authService: AuthService,
    private admision: AdmisionService,
    private router: Router
  ) {
    const user = this.authService.getCurrentUser();
    this.userName = user?.user ?? '';
  }

  onTipoEntregaChange(): void {
    if (this.tipoEntrega === 'Domicilio') {
      this.oficinaDestinoSeleccionada.set(null);
      this.oficinasDestino.set([]);
    } else if (this.cpDestino) {
      this.buscarOficinasDestino();
    }
  }

  buscarOficinasDestino(): void {
    if (!this.cpDestino || this.cpDestino.length < 4) {
      this.oficinasDestino.set([]);
      return;
    }
    this.buscandoOficinas.set(true);
    this.admision.buscarOficinas(this.cpDestino).subscribe({
      next: (lista) => {
        this.oficinasDestino.set(lista);
        this.buscandoOficinas.set(false);
      },
      error: () => {
        this.oficinasDestino.set([]);
        this.buscandoOficinas.set(false);
      }
    });
  }

  seleccionarOficina(of: OficinaJsonItem): void {
    this.oficinaDestinoSeleccionada.set(of);
  }

  private validar(): string | null {
    if (!this.peso || this.peso <= 0 || this.peso > 30) return 'Peso entre 0,1 y 30 kg.';
    if (!this.largo || !this.ancho || !this.alto) return 'Indica las tres dimensiones.';
    if (!this.nombreRem.trim()) return 'Nombre del remitente requerido.';
    if (!this.origen.trim() || !this.cpOrigen.trim()) return 'Dirección y CP origen requeridos.';
    if (!this.telRem.trim()) return 'Teléfono del remitente requerido.';
    if (!this.nombreDest.trim()) return 'Nombre del destinatario requerido.';
    if (!this.destino.trim() || !this.cpDestino.trim()) return 'Dirección y CP destino requeridos.';
    if (!this.telDest.trim()) return 'Teléfono del destinatario requerido.';
    if (this.tipoEntrega === 'Oficina' && !this.oficinaDestinoSeleccionada()) {
      return 'Selecciona la oficina destino donde se recogerá el paquete.';
    }
    return null;
  }

  enviar(): void {
    this.error.set('');
    const err = this.validar();
    if (err) {
      this.error.set(err);
      return;
    }

    const dto: AltaEnvioOficinaRequest = {
      peso: this.peso!,
      dimensiones: `${this.largo}x${this.ancho}x${this.alto} cm`,
      nombreRemitente: this.nombreRem.trim(),
      apellidosRemitente: this.apellidosRem.trim() || undefined,
      origen: this.origen.trim(),
      codigoPostalOrigen: this.cpOrigen.trim(),
      telefonoRemitente: this.telRem.trim(),
      emailRemitente: this.emailRem.trim() || undefined,
      dniRemitente: this.dniRem.trim() || undefined,
      nombreDestinatario: this.nombreDest.trim(),
      apellidosDestinatario: this.apellidosDest.trim() || undefined,
      destino: this.destino.trim(),
      codigoPostalDestino: this.cpDestino.trim(),
      telefonoDestinatario: this.telDest.trim(),
      emailDestinatario: this.emailDest.trim() || undefined,
      tipoEntrega: this.tipoEntrega,
      oficinaDestinoId: this.tipoEntrega === 'Oficina' ? this.oficinaDestinoSeleccionada()?.id : null,
      metodoCobro: this.metodoCobro,
      observaciones: this.observaciones.trim() || undefined
    };

    this.enviando.set(true);
    this.admision.altaPresencialOficina(dto).subscribe({
      next: (res) => {
        this.resultado.set(res);
        this.enviando.set(false);
      },
      error: (err) => {
        this.enviando.set(false);
        this.error.set(err.error?.mensaje || err.error?.title || 'Error al dar de alta el envío.');
      }
    });
  }

  nuevoEnvio(): void {
    this.resultado.set(null);
    this.peso = this.largo = this.ancho = this.alto = null;
    this.nombreRem = this.apellidosRem = this.origen = this.cpOrigen = '';
    this.telRem = this.emailRem = this.dniRem = '';
    this.nombreDest = this.apellidosDest = this.destino = this.cpDestino = '';
    this.telDest = this.emailDest = '';
    this.tipoEntrega = 'Domicilio';
    this.oficinasDestino.set([]);
    this.oficinaDestinoSeleccionada.set(null);
    this.observaciones = '';
  }

  volver(): void {
    this.router.navigate(['/']);
  }
}
