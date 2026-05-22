import { Component, EventEmitter, Input, AfterViewInit, OnDestroy, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Html5Qrcode, Html5QrcodeSupportedFormats } from 'html5-qrcode';

@Component({
  selector: 'app-barcode-scanner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './barcode-scanner.component.html',
  styleUrls: ['./barcode-scanner.component.css']
})
export class BarcodeScannerComponent implements AfterViewInit, OnDestroy {
  @Input() alturaScanner = 280;
  @Input() continuoMode = true;
  @Output() codigoDetectado = new EventEmitter<string>();

  scannerId = 'barcode-scanner-' + Math.random().toString(36).substring(7);
  escaneando = false;
  error: string | null = null;

  private html5Qrcode: Html5Qrcode | null = null;
  private ultimoCodigo = '';
  private ultimoTimestamp = 0;
  private readonly DEBOUNCE_MS = 2000; // evitar escaneos duplicados

  ngAfterViewInit(): void {
    this.html5Qrcode = new Html5Qrcode(this.scannerId, {
      formatsToSupport: [
        Html5QrcodeSupportedFormats.QR_CODE,
        Html5QrcodeSupportedFormats.CODE_128,
        Html5QrcodeSupportedFormats.CODE_39,
        Html5QrcodeSupportedFormats.EAN_13,
        Html5QrcodeSupportedFormats.DATA_MATRIX
      ],
      verbose: false
    });
  }

  ngOnDestroy(): void {
    this.detenerEscaneo();
  }

  async iniciarEscaneo(): Promise<void> {
    if (!this.html5Qrcode || this.escaneando) return;

    this.error = null;

    try {
      await this.html5Qrcode.start(
        { facingMode: 'environment' }, // cámara trasera preferida
        {
          fps: 10,
          qrbox: { width: 250, height: 150 },
          aspectRatio: 1.5
        },
        (decodedText) => this.onScanSuccess(decodedText),
        () => {} // ignora errores de frames sin código
      );
      this.escaneando = true;
    } catch (err: any) {
      this.error = 'No se pudo acceder a la cámara. Asegúrate de dar permisos.';
      console.error('Error al iniciar cámara:', err);
    }
  }

  async detenerEscaneo(): Promise<void> {
    if (!this.html5Qrcode || !this.escaneando) return;

    try {
      await this.html5Qrcode.stop();
    } catch {
      // Ignorar error al detener
    }
    this.escaneando = false;
  }

  onManualSubmit(value: string): void {
    const codigo = value.trim().toUpperCase();
    if (codigo.length > 0) {
      this.codigoDetectado.emit(codigo);
    }
  }

  private onScanSuccess(decodedText: string): void {
    const now = Date.now();
    const codigo = decodedText.trim().toUpperCase();

    // Debounce: ignorar escaneos duplicados muy seguidos
    if (codigo === this.ultimoCodigo && (now - this.ultimoTimestamp) < this.DEBOUNCE_MS) {
      return;
    }

    this.ultimoCodigo = codigo;
    this.ultimoTimestamp = now;

    // Emitir código detectado
    this.codigoDetectado.emit(codigo);

    // Si no es modo continuo, detener cámara
    if (!this.continuoMode) {
      this.detenerEscaneo();
    }
  }
}
