import { Component, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NotificacionService } from '../../services/notificacion.service';
import { TarifasService } from '../../services/tarifas.service';
import { NavbarPublicoComponent } from '../../components/navbar-publico/navbar-publico.component';
import { FooterPublicoComponent } from '../../components/footer-publico/footer-publico.component';

interface TarifaCalculada {
  peso: number;
  pesoFacturable: number;
  largo: number;
  ancho: number;
  alto: number;
  cpOrigen: string;
  cpDestino: string;
  zona: string;
  tarifaEstandar: number;
  tarifaPremium: number;
  tiempoEstandar: string;
  tiempoPremium: string;
  aplicaRecargo: boolean;
  recargoPorcentaje: number;
}

@Component({
  selector: 'app-calculadora-tarifas',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarPublicoComponent, FooterPublicoComponent],
  templateUrl: './calculadora-tarifas.component.html',
  styleUrl: './calculadora-tarifas.component.css'
})
export class CalculadoraTarifasComponent {
  // Formulario
  cpOrigen = signal('');
  cpDestino = signal('');
  peso = signal<number | null>(null);
  largo = signal<number | null>(null);
  ancho = signal<number | null>(null);
  alto = signal<number | null>(null);
  
  // Resultados
  tarifaCalculada = signal<TarifaCalculada | null>(null);
  isCalculating = signal(false);

  constructor(
    private router: Router,
    private notificacion: NotificacionService,
    private tarifasService: TarifasService
  ) {}

  ngOnInit(): void {
    window.scrollTo(0, 0);
  }

  calcularTarifa(): void {
    // Validación básica de campos
    if (!this.cpOrigen() || !this.cpDestino() || !this.peso() || 
        !this.largo() || !this.ancho() || !this.alto()) {
      this.notificacion.aviso('Campos incompletos', 'Completa todos los campos para calcular la tarifa.');
      return;
    }

    const peso = this.peso() || 0;
    const largo = this.largo() || 0;
    const ancho = this.ancho() || 0;
    const alto = this.alto() || 0;

    // Validación de dimensiones mínimas (10x15cm para etiqueta)
    const dimensionesOrdenadas = [largo, ancho, alto].sort((a, b) => b - a);
    if (dimensionesOrdenadas[0] < 15 || dimensionesOrdenadas[1] < 10) {
      this.notificacion.aviso('Dimensiones insuficientes', 'Las dimensiones mínimas son 10x15 cm para poder colocar la etiqueta.');
      return;
    }

    if (peso > 30) {
      this.notificacion.aviso('Peso excedido', 'El peso máximo permitido es de 30 kg.');
      return;
    }

    if (largo > 170) {
      this.notificacion.aviso('Largo excedido', 'El lado mayor máximo permitido es de 170 cm.');
      return;
    }

    const sumaDimensiones = largo + ancho + alto;
    if (sumaDimensiones > 210) {
      this.notificacion.aviso(
        'Dimensiones extra',
        `La suma de dimensiones supera 210 cm. Se aplicará un recargo del 35%. (Actual: ${sumaDimensiones} cm)`
      );
    }

    this.isCalculating.set(true);

    this.tarifasService.consultarTarifas({
      peso,
      largo,
      ancho,
      alto,
      codigoPostalOrigen: this.cpOrigen(),
      codigoPostalDestino: this.cpDestino()
    }).subscribe({
      next: (response) => {
        const estandar = response.tarifas.find(t => t.nombre.toLowerCase() === 'estandar');
        const premium = response.tarifas.find(t => t.nombre.toLowerCase() === 'premium');

        if (!estandar || !premium) {
          this.notificacion.error('Tarifas no disponibles', 'No se pudieron cargar las tarifas.');
          this.isCalculating.set(false);
          return;
        }

        this.tarifaCalculada.set({
          peso,
          pesoFacturable: response.pesoFacturable,
          largo,
          ancho,
          alto,
          cpOrigen: this.cpOrigen(),
          cpDestino: this.cpDestino(),
          zona: response.zona,
          tarifaEstandar: estandar.precioTotal,
          tarifaPremium: premium.precioTotal,
          tiempoEstandar: estandar.tiempoEntregaEstimado,
          tiempoPremium: premium.tiempoEntregaEstimado,
          aplicaRecargo: response.aplicaRecargo,
          recargoPorcentaje: response.recargoPorcentaje
        });

        this.isCalculating.set(false);
      },
      error: (err) => {
        this.isCalculating.set(false);
        this.notificacion.errorHttp(err, 'No se pudieron calcular las tarifas');
      }
    });
  }

  limpiar(): void {
    this.cpOrigen.set('');
    this.cpDestino.set('');
    this.peso.set(null);
    this.largo.set(null);
    this.ancho.set(null);
    this.alto.set(null);
    this.tarifaCalculada.set(null);
  }

  volverInicio(): void {
    this.router.navigate(['/']);
  }

  contratarEnvio(): void {
    this.router.navigate(['/nuevo-envio']);
  }
}
