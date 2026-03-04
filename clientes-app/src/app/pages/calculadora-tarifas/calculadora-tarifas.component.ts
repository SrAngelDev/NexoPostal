import { Component, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NotificacionService } from '../../services/notificacion.service';

interface TarifaCalculada {
  peso: number;
  largo: number;
  ancho: number;
  alto: number;
  cpOrigen: string;
  cpDestino: string;
  zona: string;
  tarifaEstandar: number;
  tarifaUrgente: number;
}

@Component({
  selector: 'app-calculadora-tarifas',
  standalone: true,
  imports: [CommonModule, FormsModule],
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

  constructor(private router: Router, private notificacion: NotificacionService) {}

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

    if (largo > 120) {
      this.notificacion.aviso('Largo excedido', 'El largo máximo permitido es de 120 cm.');
      return;
    }

    const sumaDimensiones = largo + ancho + alto;
    if (sumaDimensiones > 210) {
      this.notificacion.aviso('Dimensiones excedidas', `La suma del largo, ancho y alto no puede superar los 210 cm. Actualmente: ${sumaDimensiones} cm.`);
      return;
    }

    this.isCalculating.set(true);

    // Simulación de cálculo
    setTimeout(() => {
      // Cálculo del peso volumétrico: (largo × ancho × alto) / 5000
      const pesoVolumetrico = (largo * ancho * alto) / 5000;
      const pesoFacturable = Math.max(peso, pesoVolumetrico);
      
      // Determinar zona según código postal destino
      const cpDest = parseInt(this.cpDestino());
      let zona = 'Península';
      
      if (cpDest >= 35000 && cpDest <= 35999) {
        zona = 'Canarias';
      } else if (cpDest >= 7000 && cpDest <= 7999) {
        zona = 'Baleares';
      } else if (cpDest >= 51000 && cpDest <= 52999) {
        zona = 'Ceuta/Melilla';
      }

      // Tarifa unificada basada solo en peso volumétrico
      // Precio base + (peso facturable × tarifa por kg)
      const basePrice = 5 + (pesoFacturable * 1.5);
      const tarifaEstandar = basePrice;
      const tarifaUrgente = tarifaEstandar * 1.8;

      this.tarifaCalculada.set({
        peso: peso,
        largo: largo,
        ancho: ancho,
        alto: alto,
        cpOrigen: this.cpOrigen(),
        cpDestino: this.cpDestino(),
        zona,
        tarifaEstandar: parseFloat(tarifaEstandar.toFixed(2)),
        tarifaUrgente: parseFloat(tarifaUrgente.toFixed(2))
      });

      this.isCalculating.set(false);
    }, 800);
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
