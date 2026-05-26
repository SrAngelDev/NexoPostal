import { Component, Input, Output, EventEmitter, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { SignalrService } from '../../services/signalr.service';

@Component({
  selector: 'app-driver-navbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './driver-navbar.component.html',
  styleUrl: './driver-navbar.component.css'
})
export class DriverNavbarComponent implements OnInit {
  @Input() icon = 'local_shipping';
  @Input() title = '';
  /** Subtítulo opcional (p.ej. "3 repartidores activos"). */
  @Input() subtitle = '';
  /** Muestra el botón atrás. false en dashboard y dashboard-jefe. */
  @Input() showBack = true;
  /** Si hay listener externo, emite el evento; si no, navega a /dashboard. */
  @Output() backClick = new EventEmitter<void>();

  private auth = inject(AuthService);
  public signalr = inject(SignalrService);
  private router = inject(Router);

  showNotif = signal(false);

  ngOnInit(): void {
    this.signalr.iniciar();
  }

  get userName(): string { return this.auth.getCurrentUser()?.user ?? ''; }
  get userRole(): string { return this.auth.getCurrentUser()?.rol ?? ''; }
  get roleLabel(): string {
    switch (this.userRole) {
      case 'Repartidor':  return 'Repartidor';
      case 'JefeReparto': return 'Jefe de Reparto';
      default:            return this.userRole;
    }
  }
  get initials(): string { return this.userName.slice(0, 2).toUpperCase(); }

  get estadoLabel(): string {
    switch (this.signalr.estadoConexion()) {
      case 'conectado':   return 'En línea';
      case 'conectando':  return 'Conectando…';
      default:            return 'Desconectado';
    }
  }

  onBack(): void {
    if (this.backClick.observed) {
      this.backClick.emit();
    } else {
      this.router.navigate(['/dashboard']);
    }
  }

  logout(): void {
    this.signalr.detener();
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  toggleNotif(): void {
    const opening = !this.showNotif();
    this.showNotif.set(opening);
    if (opening) this.signalr.marcarTodasLeidas();
  }
}
