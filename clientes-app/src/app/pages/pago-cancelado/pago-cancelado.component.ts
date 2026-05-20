import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PagosService } from '../../services/pagos.service';
import { NavbarPublicoComponent } from '../../components/navbar-publico/navbar-publico.component';

@Component({
  selector: 'app-pago-cancelado',
  standalone: true,
  imports: [CommonModule, NavbarPublicoComponent],
  templateUrl: './pago-cancelado.component.html',
  styleUrl: './pago-cancelado.component.css'
})
export class PagoCanceladoComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private pagosService = inject(PagosService);

  numeroSeguimiento = signal<string | null>(null);
  reintentando = signal(false);
  error = signal<string | null>(null);

  ngOnInit() {
    const envio = this.route.snapshot.queryParamMap.get('envio');
    if (envio) {
      this.numeroSeguimiento.set(envio);
    }
  }

  reintentarPago() {
    const numero = this.numeroSeguimiento();
    if (!numero) return;

    this.reintentando.set(true);
    this.error.set(null);

    this.pagosService.reintentarPago(numero, { urlBase: window.location.origin }).subscribe({
      next: (res) => {
        window.location.href = res.sessionUrl;
      },
      error: (err) => {
        this.reintentando.set(false);
        this.error.set(err.error?.message || 'No se pudo crear una nueva sesión de pago. Inténtalo de nuevo.');
      }
    });
  }

  irAlInicio() {
    this.router.navigate(['/']);
  }
}
