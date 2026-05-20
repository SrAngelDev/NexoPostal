import { Component, ElementRef, EventEmitter, Input, AfterViewInit, OnDestroy, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Html5Qrcode, Html5QrcodeSupportedFormats } from 'html5-qrcode';

@Component({
  selector: 'app-barcode-scanner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="scanner-container">
      <!-- Viewfinder de cámara -->
      <div class="relative rounded-xl overflow-hidden bg-gray-900 border-2"
           [class.border-blue-500]="escaneando"
           [class.border-gray-600]="!escaneando">

        <div #scannerElement [id]="scannerId"
             class="w-full"
             [style.min-height.px]="alturaScanner">
        </div>

        <!-- Overlay cuando no está escaneando -->
        @if (!escaneando) {
          <div class="absolute inset-0 flex flex-col items-center justify-center bg-gray-900/80 gap-4">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-16 h-16 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                    d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z"/>
            </svg>
            <p class="text-gray-400 text-sm">Cámara detenida</p>
          </div>
        }
      </div>

      <!-- Controles -->
      <div class="flex items-center gap-3 mt-3">
        @if (!escaneando) {
          <button (click)="iniciarEscaneo()"
                  class="flex-1 flex items-center justify-center gap-2 bg-blue-600 hover:bg-blue-700 text-white font-medium py-2.5 px-4 rounded-lg transition-colors">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"/>
            </svg>
            Iniciar cámara
          </button>
        } @else {
          <button (click)="detenerEscaneo()"
                  class="flex-1 flex items-center justify-center gap-2 bg-red-600 hover:bg-red-700 text-white font-medium py-2.5 px-4 rounded-lg transition-colors">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 10a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z"/>
            </svg>
            Detener
          </button>
        }
      </div>

      <!-- Input manual como alternativa -->
      <div class="mt-3 flex gap-2">
        <input #manualInput
               type="text"
               placeholder="O escribe el código manualmente (NXI-...)"
               class="flex-1 bg-gray-800 border border-gray-600 text-white rounded-lg px-3 py-2 text-sm placeholder-gray-500 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none"
               (keydown.enter)="onManualSubmit(manualInput.value); manualInput.value = ''"/>
        <button (click)="onManualSubmit(manualInput.value); manualInput.value = ''"
                class="bg-gray-700 hover:bg-gray-600 text-white px-3 py-2 rounded-lg transition-colors">
          <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
          </svg>
        </button>
      </div>

      <!-- Error message -->
      @if (error) {
        <div class="mt-2 bg-red-900/30 border border-red-800 text-red-400 px-3 py-2 rounded-lg text-sm">
          {{ error }}
        </div>
      }
    </div>
  `
})
export class BarcodeScannerComponent implements AfterViewInit, OnDestroy {
  @Input() alturaScanner = 280;
  @Input() continuoMode = true;
  @Output() codigoDetectado = new EventEmitter<string>();

  @ViewChild('scannerElement') scannerElement!: ElementRef;

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
