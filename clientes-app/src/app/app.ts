import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificacionModalComponent } from './components/notificacion-modal/notificacion-modal.component';
import { ConfirmacionModalComponent } from './components/confirmacion-modal/confirmacion-modal.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NotificacionModalComponent, ConfirmacionModalComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Front-NexoPostal');
}
