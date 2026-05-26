import { Component, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BrowserMultiFormatReader, IScannerControls } from '@zxing/browser';
import { BarcodeFormat, DecodeHintType } from '@zxing/library';

@Component({
  selector: 'app-barcode-scanner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="scanner-wrap">
      <!-- Camera viewfinder -->
      <div class="viewfinder"
           [class.viewfinder-active]="escaneando"
           [class.viewfinder-idle]="!escaneando"
           [style.height.px]="alturaScanner">

        <video #videoEl [id]="scannerId" class="scanner-area" muted playsinline autoplay></video>

        @if (escaneando) {
          <!-- Marco de targeting visible cuando hay cámara -->
          <div class="targeting-frame" aria-hidden="true">
            <span class="corner corner-tl"></span>
            <span class="corner corner-tr"></span>
            <span class="corner corner-bl"></span>
            <span class="corner corner-br"></span>
            <div class="laser-line"></div>
          </div>
        } @else {
          <div class="scanner-overlay">
            <span class="material-symbols-outlined overlay-icon">qr_code_scanner</span>
            <p class="overlay-text">Cámara detenida</p>
            <span class="overlay-hint">Pulsa "Iniciar cámara" para escanear</span>
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
      width: 100%;
      border-radius: 1rem;
      overflow: hidden;
      background: #0b1220;
      border: 2px solid #1f2937;
      box-shadow: 0 8px 24px -12px rgba(0, 0, 0, 0.4);
      transition: border-color 0.2s ease;
    }

    .viewfinder-active {
      border-color: #10b981;
      box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.15), 0 8px 24px -12px rgba(0, 0, 0, 0.4);
    }

    .viewfinder-idle {
      border-color: #374151;
    }

    .scanner-area {
      position: absolute;
      inset: 0;
      width: 100%;
      height: 100%;
      object-fit: cover;
      display: block;
      background: #0b1226;
    }

    /* ───── Marco de targeting ───── */
    .targeting-frame {
      position: absolute;
      top: 50%;
      left: 50%;
      width: min(70%, 240px);
      height: min(45%, 150px);
      transform: translate(-50%, -50%);
      pointer-events: none;
      box-shadow: 0 0 0 9999px rgba(11, 18, 32, 0.45);
      border-radius: 12px;
    }

    .corner {
      position: absolute;
      width: 28px;
      height: 28px;
      border-color: #10b981;
      border-style: solid;
      border-width: 0;
    }
    .corner-tl { top: -2px; left: -2px; border-top-width: 4px; border-left-width: 4px; border-top-left-radius: 12px; }
    .corner-tr { top: -2px; right: -2px; border-top-width: 4px; border-right-width: 4px; border-top-right-radius: 12px; }
    .corner-bl { bottom: -2px; left: -2px; border-bottom-width: 4px; border-left-width: 4px; border-bottom-left-radius: 12px; }
    .corner-br { bottom: -2px; right: -2px; border-bottom-width: 4px; border-right-width: 4px; border-bottom-right-radius: 12px; }

    .laser-line {
      position: absolute;
      left: 8%;
      right: 8%;
      top: 50%;
      height: 2px;
      background: linear-gradient(90deg, transparent, #10b981, transparent);
      box-shadow: 0 0 8px rgba(16, 185, 129, 0.7);
      animation: laser 2s ease-in-out infinite;
    }

    @keyframes laser {
      0%, 100% { transform: translateY(-30px); opacity: 0.4; }
      50% { transform: translateY(30px); opacity: 1; }
    }

    /* ───── Overlay idle ───── */
    .scanner-overlay {
      position: absolute;
      inset: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #0f172a, #1e293b);
      gap: 0.5rem;
      padding: 1rem;
      text-align: center;
    }

    .overlay-icon {
      font-size: 3.5rem;
      color: #475569;
    }

    .overlay-text {
      color: #cbd5e1;
      font-size: 1rem;
      font-weight: 600;
      margin: 0;
    }

    .overlay-hint {
      color: #64748b;
      font-size: 0.8125rem;
    }

    /* ───── Controles ───── */
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
      background: linear-gradient(135deg, #10b981, #059669);
      color: white;
      box-shadow: 0 4px 12px -4px rgba(5, 150, 105, 0.5);
    }

    .btn-start:hover {
      transform: translateY(-1px);
      box-shadow: 0 6px 16px -4px rgba(5, 150, 105, 0.6);
    }

    .btn-stop {
      background: linear-gradient(135deg, #ef4444, #dc2626);
      color: white;
      box-shadow: 0 4px 12px -4px rgba(220, 38, 38, 0.5);
    }

    .btn-stop:hover {
      transform: translateY(-1px);
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
      padding: 0.625rem 0.875rem;
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
export class BarcodeScannerComponent implements OnInit, OnDestroy {
  @Input() alturaScanner = 260;
  @Input() continuoMode = true;
  @Output() codigoDetectado = new EventEmitter<string>();

  @ViewChild('videoEl') videoEl?: ElementRef<HTMLVideoElement>;

  scannerId = 'driver-scanner-' + Math.random().toString(36).substring(7);
  escaneando = false;
  error: string | null = null;

  private reader: BrowserMultiFormatReader | null = null;
  private controls: IScannerControls | null = null;

  // Doble confirmación: solo emitimos el código cuando dos lecturas consecutivas
  // coinciden. Evita los falsos positivos típicos de barras (ej. "NX*-@DA#!WU").
  private lecturaPrevia = '';
  private ultimoEmitido = '';
  private ultimoTimestamp = 0;
  private readonly DEBOUNCE_MS = 1500;

  ngOnInit(): void {}

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
      this.error = 'No se pudo acceder a la cámara. Revisa los permisos.';
      console.error('Error al iniciar cámara:', err);
    }
  }

  detenerEscaneo(): void {
    try {
      this.controls?.stop();
    } catch {
      // Ignore
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
