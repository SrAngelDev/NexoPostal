import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';

interface RutaResumen {
  id: number;
  codigo: string;
  fecha: string;
  estado: string;
  repartidorNombre: string;
  totalEntregas: number;
  entregasCompletadas: number;
}

@Component({
  selector: 'app-gestion-rutas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-rutas.component.html',
  styleUrl: './gestion-rutas.component.css'
})
export class GestionRutasComponent implements OnInit {
  rutas = signal<RutaResumen[]>([]);
  cargando = signal(false);
  error = signal<string | null>(null);

  private readonly API = '/api/reparto';

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.cargarRutas();
  }

  cargarRutas(): void {
    this.cargando.set(true);
    this.error.set(null);

    const hoy = new Date().toISOString().split('T')[0];
    this.http.get<RutaResumen[]>(`${this.API}/rutas?fecha=${hoy}`).subscribe({
      next: (rutas) => {
        this.rutas.set(rutas);
        this.cargando.set(false);
      },
      error: (err) => {
        this.error.set('No se pudieron cargar las rutas. Inténtalo de nuevo.');
        this.cargando.set(false);
        console.error('Error cargando rutas:', err);
      }
    });
  }

  verDetalle(id: number): void {
    this.router.navigate(['/ruta'], { queryParams: { id } });
  }

  volver(): void {
    this.router.navigate(['/']);
  }

  getEstadoClass(estado: string): string {
    switch (estado?.toLowerCase()) {
      case 'planificada': return 'estado-planificada';
      case 'encurso': return 'estado-en-curso';
      case 'completada': return 'estado-completada';
      default: return '';
    }
  }
}
