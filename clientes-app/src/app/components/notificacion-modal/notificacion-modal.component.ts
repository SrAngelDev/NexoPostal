import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificacionService, Notificacion } from '../../services/notificacion.service';

@Component({
  selector: 'app-notificacion-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <!-- Contenedor de notificaciones — posición fija arriba derecha -->
    <div class="fixed top-4 right-4 z-[9999] flex flex-col gap-3 pointer-events-none max-w-md w-full">
      @for (n of notificacionService.notificaciones(); track n.id) {
        <div 
          class="pointer-events-auto rounded-xl shadow-2xl border-l-4 backdrop-blur-sm animate-slide-in"
          [class]="getClases(n)"
          role="alert"
        >
          <!-- Header -->
          <div class="flex items-start gap-3 p-4 pb-2">
            <span class="material-symbols-outlined text-2xl mt-0.5 shrink-0" [class]="getIconColor(n)">
              {{ getIcono(n) }}
            </span>
            <div class="flex-1 min-w-0">
              <h4 class="font-semibold text-sm leading-tight" [class]="getTituloColor(n)">
                {{ n.titulo }}
              </h4>
              @if (n.mensaje) {
                <p class="text-sm mt-1 leading-snug" [class]="getMensajeColor(n)">
                  {{ n.mensaje }}
                </p>
              }
            </div>
            <button 
              (click)="notificacionService.cerrar(n.id)"
              class="shrink-0 p-0.5 rounded-lg hover:bg-black/10 transition-colors cursor-pointer"
              [class]="getBotonColor(n)"
              aria-label="Cerrar notificación"
            >
              <span class="material-symbols-outlined text-lg">close</span>
            </button>
          </div>

          <!-- Detalles expandibles (para errores de validación) -->
          @if (n.detalles) {
            <div class="px-4 pb-3 pl-[3.25rem]">
              <pre class="text-xs whitespace-pre-wrap font-sans leading-relaxed rounded-lg p-2.5 mt-1"
                   [class]="getDetallesClases(n)">{{ n.detalles }}</pre>
            </div>
          }

          <!-- Barra de progreso para auto-cerrar -->
          @if (n.autoCerrar) {
            <div class="h-1 rounded-b-xl overflow-hidden" [class]="getBarraFondo(n)">
              <div 
                class="h-full rounded-b-xl transition-none"
                [class]="getBarraColor(n)"
                [style.animation]="'shrink ' + n.duracion + 'ms linear forwards'"
              ></div>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    @keyframes slide-in {
      from {
        opacity: 0;
        transform: translateX(100%) scale(0.95);
      }
      to {
        opacity: 1;
        transform: translateX(0) scale(1);
      }
    }

    @keyframes shrink {
      from { width: 100%; }
      to { width: 0%; }
    }

    .animate-slide-in {
      animation: slide-in 0.3s ease-out;
    }
  `]
})
export class NotificacionModalComponent {
  notificacionService = inject(NotificacionService);

  getClases(n: Notificacion): string {
    const base = 'animate-slide-in';
    switch (n.tipo) {
      case 'exito':  return `${base} bg-green-50 border-green-500`;
      case 'error':  return `${base} bg-red-50 border-red-500`;
      case 'aviso':  return `${base} bg-amber-50 border-amber-500`;
      case 'info':   return `${base} bg-blue-50 border-blue-500`;
    }
  }

  getIcono(n: Notificacion): string {
    switch (n.tipo) {
      case 'exito':  return 'check_circle';
      case 'error':  return 'error';
      case 'aviso':  return 'warning';
      case 'info':   return 'info';
    }
  }

  getIconColor(n: Notificacion): string {
    switch (n.tipo) {
      case 'exito':  return 'text-green-600';
      case 'error':  return 'text-red-600';
      case 'aviso':  return 'text-amber-600';
      case 'info':   return 'text-blue-600';
    }
  }

  getTituloColor(n: Notificacion): string {
    switch (n.tipo) {
      case 'exito':  return 'text-green-800';
      case 'error':  return 'text-red-800';
      case 'aviso':  return 'text-amber-800';
      case 'info':   return 'text-blue-800';
    }
  }

  getMensajeColor(n: Notificacion): string {
    switch (n.tipo) {
      case 'exito':  return 'text-green-700';
      case 'error':  return 'text-red-700';
      case 'aviso':  return 'text-amber-700';
      case 'info':   return 'text-blue-700';
    }
  }

  getBotonColor(n: Notificacion): string {
    switch (n.tipo) {
      case 'exito':  return 'text-green-500 hover:text-green-700';
      case 'error':  return 'text-red-500 hover:text-red-700';
      case 'aviso':  return 'text-amber-500 hover:text-amber-700';
      case 'info':   return 'text-blue-500 hover:text-blue-700';
    }
  }

  getDetallesClases(n: Notificacion): string {
    switch (n.tipo) {
      case 'exito':  return 'bg-green-100/60 text-green-800';
      case 'error':  return 'bg-red-100/60 text-red-800';
      case 'aviso':  return 'bg-amber-100/60 text-amber-800';
      case 'info':   return 'bg-blue-100/60 text-blue-800';
    }
  }

  getBarraFondo(n: Notificacion): string {
    switch (n.tipo) {
      case 'exito':  return 'bg-green-200';
      case 'error':  return 'bg-red-200';
      case 'aviso':  return 'bg-amber-200';
      case 'info':   return 'bg-blue-200';
    }
  }

  getBarraColor(n: Notificacion): string {
    switch (n.tipo) {
      case 'exito':  return 'bg-green-500';
      case 'error':  return 'bg-red-500';
      case 'aviso':  return 'bg-amber-500';
      case 'info':   return 'bg-blue-500';
    }
  }
}
