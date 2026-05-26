import { Component, Input, Output, EventEmitter, OnInit, signal, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { SignalrService } from '../../services/signalr.service';

@Component({
  selector: 'app-intranet-navbar',
  standalone: true,
  imports: [],
  templateUrl: './intranet-navbar.component.html',
  styleUrl: './intranet-navbar.component.css'
})
export class IntranetNavbarComponent implements OnInit {
  @Input() icon = 'home';
  @Input() title = '';
  /** Muestra el botón atrás (navega a /dashboard). false en dashboard/admin-panel. */
  @Input() showBack = true;
  /** Emite si el padre quiere manejar la navegación atrás. Si no se escucha, va a /dashboard. */
  @Output() backClick = new EventEmitter<void>();

  private auth = inject(AuthService);
  public signalr = inject(SignalrService);
  private router = inject(Router);

  showNotif = signal(false);

  ngOnInit(): void {
    this.signalr.conectar();
  }

  get userName(): string { return this.auth.getCurrentUser()?.user ?? ''; }
  get userRole(): string { return this.auth.getCurrentUser()?.rol ?? ''; }
  get initials(): string { return this.userName.slice(0, 2).toUpperCase(); }

  onBack(): void {
    if (this.backClick.observed) {
      this.backClick.emit();
    } else {
      this.router.navigate(['/dashboard']);
    }
  }

  logout(): void {
    this.signalr.desconectar();
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  toggleNotif(): void {
    const opening = !this.showNotif();
    this.showNotif.set(opening);
    if (opening) this.signalr.marcarComoLeidas();
  }
}
