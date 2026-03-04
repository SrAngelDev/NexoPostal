import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfirmacionService } from '../../services/confirmacion.service';

@Component({
  selector: 'app-confirmacion-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (confirmacion.visible()) {
      <!-- Overlay -->
      <div 
        class="fixed inset-0 bg-black/40 backdrop-blur-sm z-[9998] flex items-center justify-center p-4 animate-fade-in"
        (click)="confirmacion.resolver(false)"
      >
        <!-- Modal -->
        <div 
          class="bg-white rounded-2xl shadow-2xl max-w-sm w-full overflow-hidden animate-scale-in"
          (click)="$event.stopPropagation()"
        >
          <!-- Icono + Título -->
          <div class="p-6 pb-3 text-center">
            <div class="mx-auto w-14 h-14 rounded-full flex items-center justify-center mb-4"
                 [class]="confirmacion.config().tipo === 'peligro' ? 'bg-red-100' : 'bg-indigo-100'">
              <span class="material-symbols-outlined text-3xl"
                    [class]="confirmacion.config().tipo === 'peligro' ? 'text-red-600' : 'text-[#1A237E]'">
                {{ confirmacion.config().tipo === 'peligro' ? 'warning' : 'help' }}
              </span>
            </div>
            <h3 class="text-lg font-bold text-gray-900">{{ confirmacion.config().titulo }}</h3>
            <p class="text-sm text-gray-600 mt-2 leading-relaxed">{{ confirmacion.config().mensaje }}</p>
          </div>

          <!-- Botones -->
          <div class="flex gap-3 p-5 pt-3">
            <button
              (click)="confirmacion.resolver(false)"
              class="flex-1 px-4 py-2.5 text-sm font-semibold text-gray-700 bg-gray-100 rounded-xl hover:bg-gray-200 transition-colors cursor-pointer"
            >
              {{ confirmacion.config().textoCancelar }}
            </button>
            <button
              (click)="confirmacion.resolver(true)"
              class="flex-1 px-4 py-2.5 text-sm font-semibold text-white rounded-xl transition-colors cursor-pointer"
              [class]="confirmacion.config().tipo === 'peligro' 
                ? 'bg-red-600 hover:bg-red-700' 
                : 'bg-[#1A237E] hover:bg-[#0D1B5E]'"
            >
              {{ confirmacion.config().textoConfirmar }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    @keyframes fade-in {
      from { opacity: 0; }
      to { opacity: 1; }
    }
    @keyframes scale-in {
      from { opacity: 0; transform: scale(0.9); }
      to { opacity: 1; transform: scale(1); }
    }
    .animate-fade-in {
      animation: fade-in 0.2s ease-out;
    }
    .animate-scale-in {
      animation: scale-in 0.25s ease-out;
    }
  `]
})
export class ConfirmacionModalComponent {
  confirmacion = inject(ConfirmacionService);
}
