import { Component, ElementRef, EventEmitter, Input, AfterViewInit, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Html5Qrcode, Html5QrcodeSupportedFormats } from 'html5-qrcode';

@Component({
  selector: 'app-barcode-scanner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="scanner-wrap">
      <!-- Camera viewfinder -->
      <div class="viewfinder"
           [class.viewfinder-active]="escaneando"
           [class.viewfinder-idle]="!escaneando">

        <div #scannerElement [id]="scannerId"
             class="scanner-area"
             [style.min-height.px]="alturaScanner">
        </div>

        @if (!escaneando) {
          <div class="scanner-overlay">
            <span class="material-symbols-outlined overlay-icon">qr_code_scanner</span>
            <p class="overlay-text">Cámara detenida</p>
          </div>
        }
      </div>

      <!-- Controles -->
      <div class="scanner-controls">
        @if (!escaneando) {
          <button (click)="iniciarEscaneo()" class="btn-start">
            <span class="material-symbols-outlined">videocam</span>
            Iniciar cámara
          </button>
        } @else {
          <button (click)="detenerEscaneo()" class="btn-stop">
            <span class="material-symbols-outlined">stop_circle</span>
            Detener
          </button>
        }
      </div>

      <!-- Input manual -->
      <div class="manual-input">
        <input #manualInput
               type="text"
               placeholder="Escribe el código (NXI-...)"
               class="manual-field"
               (keydown.enter)="onManualSubmit(manualInput.value); manualInput.value = ''"/>
        <button (click)="onManualSubmit(manualInput.value); manualInput.value = ''"
                class="btn-manual">
          <span class="material-symbols-outlined">search</span>
        </button>
      </div>

      @if (error) {
        <div class="scanner-error">
          <span class="material-symbols-outlined">warning</span>
          {{ error }}
        </div>
      }
    </div>
  `,
  styles: [`
    .scanner-wrap {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .viewfinder {
      position: relative;
      border-radius: 0.75rem;
      overflow: hidden;
      background: #111827;
      border: 2px solid;
    }

    .viewfinder-active {
      border-color: #059669;
    }

    .viewfinder-idle {
      border-color: #374151;
    }

    .scanner-area {
      width: 100%;
    }

    .scanner-overlay {
      position: absolute;
      inset: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background: rgba(17, 24, 39, 0.85);
      gap: 0.75rem;
    }

    .overlay-icon {
      font-size: 3rem;
      color: #6b7280;
    }

    .overlay-text {
      color: #9ca3af;
      font-size: 0.875rem;
      margin: 0;
    }

    .scanner-controls {
      display: flex;
      gap: 0.5rem;
    }

    .btn-start, .btn-stop {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      border: none;
      border-radius: 0.75rem;
      font-size: 0.9375rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
    }

    .btn-start {
      background: #059669;
      color: white;
    }

    .btn-start:hover {
      background: #047857;
    }

    .btn-stop {
      background: #dc2626;
      color: white;
    }

    .btn-stop:hover {
      background: #b91c1c;
    }

    .btn-start .material-symbols-outlined,
    .btn-stop .material-symbols-outlined {
      font-size: 1.25rem;
    }

    .manual-input {
      display: flex;
      gap: 0.5rem;
    }

    .manual-field {
      flex: 1;
      background: #f3f4f6;
      border: 1px solid #d1d5db;
      border-radius: 0.625rem;
      padding: 0.625rem 0.875rem;
      font-size: 0.875rem;
      color: #1f2937;
      transition: all 0.2s;
    }

    .manual-field:focus {
      outline: none;
      border-color: #059669;
      box-shadow: 0 0 0 2px rgba(5, 150, 105, 0.2);
    }

    .manual-field::placeholder {
      color: #9ca3af;
    }

    .btn-manual {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 0.625rem;
      background: #e5e7eb;
      border: 1px solid #d1d5db;
      border-radius: 0.625rem;
      color: #4b5563;
      cursor: pointer;
      transition: all 0.2s;
    }

    .btn-manual:hover {
      background: #d1d5db;
      color: #059669;
    }

    .btn-manual .material-symbols-outlined {
      font-size: 1.25rem;
    }

    .scanner-error {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.625rem 0.875rem;
      background: #fef2f2;
      border: 1px solid #fecaca;
      border-radius: 0.625rem;
      color: #dc2626;
      font-size: 0.8125rem;
    }

    .scanner-error .material-symbols-outlined {
      font-size: 1.125rem;
    }
  `]
})
export class BarcodeScannerComponent implements OnInit, AfterViewInit, OnDestroy {
  @Input() alturaScanner = 260;
  @Input() continuoMode = true;
  @Output() codigoDetectado = new EventEmitter<string>();

  @ViewChild('scannerElement') scannerElement!: ElementRef;

  scannerId = 'driver-scanner-' + Math.random().toString(36).substring(7);
  escaneando = false;
  error: string | null = null;

  private html5Qrcode: Html5Qrcode | null = null;
  private ultimoCodigo = '';
  private ultimoTimestamp = 0;
  private readonly DEBOUNCE_MS = 2000;

  ngOnInit(): void {}

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
        { facingMode: 'environment' },
        {
          fps: 10,
          qrbox: { width: 250, height: 150 },
          aspectRatio: 1.5
        },
        (decodedText) => this.onScanSuccess(decodedText),
        () => {}
      );
      this.escaneando = true;
    } catch (err: any) {
      this.error = 'No se pudo acceder a la cámara. Revisa los permisos.';
      console.error('Error al iniciar cámara:', err);
    }
  }

  async detenerEscaneo(): Promise<void> {
    if (!this.html5Qrcode || !this.escaneando) return;

    try {
      await this.html5Qrcode.stop();
    } catch {
      // Ignore
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
    const ahora = Date.now();
    if (decodedText === this.ultimoCodigo && ahora - this.ultimoTimestamp < this.DEBOUNCE_MS) {
      return;
    }
    this.ultimoCodigo = decodedText;
    this.ultimoTimestamp = ahora;
    this.codigoDetectado.emit(decodedText.toUpperCase());

    if (!this.continuoMode) {
      this.detenerEscaneo();
    }
  }
}
