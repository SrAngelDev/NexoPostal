import { Component, signal, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificacionModalComponent } from './components/notificacion-modal/notificacion-modal.component';
import { ConfirmacionModalComponent } from './components/confirmacion-modal/confirmacion-modal.component';
import { ThemeService } from './services/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NotificacionModalComponent, ConfirmacionModalComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Front-NexoPostal');
  // Inicializa el tema (light/dark) al arranque de la app.
  private readonly _theme = inject(ThemeService);
}
