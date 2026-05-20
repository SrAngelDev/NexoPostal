import { Component, signal, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PagosService, VerificarPagoResponse } from '../../services/pagos.service';
import { NavbarPublicoComponent } from '../../components/navbar-publico/navbar-publico.component';

@Component({
  selector: 'app-pago-exitoso',
  standalone: true,
  imports: [CommonModule, NavbarPublicoComponent],
  templateUrl: './pago-exitoso.component.html',
  styleUrls: ['./pago-exitoso.component.css']
})
export class PagoExitosoComponent implements OnInit {
  verificando = signal(true);
  resultado = signal<VerificarPagoResponse | null>(null);
  error = signal('');

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private pagosService: PagosService
  ) {}

  ngOnInit(): void {
    const sessionId = this.route.snapshot.queryParamMap.get('session_id');
    
    if (!sessionId) {
      this.error.set('No se encontró la sesión de pago.');
      this.verificando.set(false);
      return;
    }

    this.pagosService.verificarPago(sessionId).subscribe({
      next: (res) => {
        this.resultado.set(res);
        this.verificando.set(false);
      },
      error: () => {
        this.error.set('No se pudo verificar el estado del pago. Inténtalo de nuevo más tarde.');
        this.verificando.set(false);
      }
    });
  }

  irAlPanel(): void {
    this.router.navigate(['/panel']);
  }

  irAlInicio(): void {
    this.router.navigate(['/']);
  }
}
