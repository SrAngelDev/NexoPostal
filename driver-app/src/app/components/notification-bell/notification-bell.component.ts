import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SignalrService } from '../../services/signalr.service';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.css'
})
export class NotificationBellComponent {
  protected signalr = inject(SignalrService);
  abierto = signal(false);

  toggle(): void {
    const nuevo = !this.abierto();
    this.abierto.set(nuevo);
    if (nuevo) this.signalr.marcarTodasLeidas();
  }

  cerrar(): void { this.abierto.set(false); }

  limpiar(): void {
    this.signalr.limpiar();
    this.abierto.set(false);
  }
}
