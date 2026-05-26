import { Component, ElementRef, EventEmitter, Input, OnDestroy, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BrowserMultiFormatReader, IScannerControls } from '@zxing/browser';
import { BarcodeFormat, DecodeHintType } from '@zxing/library';

@Component({
  selector: 'app-barcode-scanner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './barcode-scanner.component.html',
  styleUrls: ['./barcode-scanner.component.css']
})
export class BarcodeScannerComponent implements OnDestroy {
  @Input() alturaScanner = 280;
  @Input() continuoMode = true;
  @Output() codigoDetectado = new EventEmitter<string>();

  @ViewChild('videoEl') videoEl?: ElementRef<HTMLVideoElement>;

  scannerId = 'barcode-scanner-' + Math.random().toString(36).substring(7);
  escaneando = false;
  error: string | null = null;

  private reader: BrowserMultiFormatReader | null = null;
  private controls: IScannerControls | null = null;

  // Doble confirmación: solo emitimos el código cuando dos lecturas consecutivas
  // coinciden. Evita las lecturas corruptas típicas de barras (ej. "NX*-@DA#!WU").
  private lecturaPrevia = '';
  private ultimoEmitido = '';
  private ultimoTimestamp = 0;
  private readonly DEBOUNCE_MS = 1500;

  ngOnDestroy(): void {
    this.detenerEscaneo();
  }

  async iniciarEscaneo(): Promise<void> {
    if (this.escaneando) return;
    if (!this.videoEl) {
      this.error = 'Video no disponible';
      return;
    }

    this.error = null;
    this.lecturaPrevia = '';

    try {
      if (!this.reader) {
        const hints = new Map<DecodeHintType, unknown>();
        hints.set(DecodeHintType.POSSIBLE_FORMATS, [
          BarcodeFormat.QR_CODE,
          BarcodeFormat.CODE_128,
          BarcodeFormat.CODE_39,
          BarcodeFormat.EAN_13,
          BarcodeFormat.DATA_MATRIX
        ]);
        hints.set(DecodeHintType.TRY_HARDER, true);
        this.reader = new BrowserMultiFormatReader(hints, { delayBetweenScanAttempts: 80 });
      }

      this.controls = await this.reader.decodeFromConstraints(
        {
          video: {
            facingMode: { ideal: 'environment' },
            width: { ideal: 1280 },
            height: { ideal: 720 }
          }
        },
        this.videoEl.nativeElement,
        (result) => {
          if (result) this.onLectura(result.getText());
        }
      );
      this.escaneando = true;
    } catch (err: any) {
      this.error = 'No se pudo acceder a la cámara. Asegúrate de dar permisos.';
      console.error('Error al iniciar cámara:', err);
    }
  }

  detenerEscaneo(): void {
    try {
      this.controls?.stop();
    } catch {
      // Ignorar
    }
    this.controls = null;
    this.escaneando = false;
  }

  onManualSubmit(value: string): void {
    const codigo = value.trim().toUpperCase();
    if (codigo.length > 0) {
      this.codigoDetectado.emit(codigo);
    }
  }

  private onLectura(decodedText: string): void {
    const codigo = decodedText.trim().toUpperCase();

    // 1) Doble confirmación: dos lecturas iguales seguidas.
    if (codigo !== this.lecturaPrevia) {
      this.lecturaPrevia = codigo;
      return;
    }

    // 2) Debounce: no repetir el mismo código en una ventana corta.
    const now = Date.now();
    if (codigo === this.ultimoEmitido && (now - this.ultimoTimestamp) < this.DEBOUNCE_MS) {
      return;
    }

    this.ultimoEmitido = codigo;
    this.ultimoTimestamp = now;
    this.lecturaPrevia = '';

    this.codigoDetectado.emit(codigo);

    if (!this.continuoMode) {
      this.detenerEscaneo();
    }
  }
}
