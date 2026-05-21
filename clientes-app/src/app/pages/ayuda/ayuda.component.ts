import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NavbarPublicoComponent } from '../../components/navbar-publico/navbar-publico.component';
import { FooterPublicoComponent } from '../../components/footer-publico/footer-publico.component';

@Component({
  selector: 'app-ayuda',
  standalone: true,
  imports: [CommonModule, NavbarPublicoComponent, FooterPublicoComponent],
  templateUrl: './ayuda.component.html'
})
export class AyudaComponent {
  faqs = [
    {
      pregunta: '¿Cómo puedo hacer un seguimiento de mi envío?',
      respuesta: 'Desde la página principal, introduce tu número de seguimiento (formato NX + 11 dígitos + ES) en el localizador. También puedes consultar el estado desde el panel de usuario en la sección "Mis Envíos".',
      abierta: false
    },
    {
      pregunta: '¿Cuáles son los plazos de entrega?',
      respuesta: 'Envío estándar Península: 3-5 días laborables. Envío urgente: 24-48 horas. Islas Baleares: 4-6 días. Islas Canarias, Ceuta y Melilla: 5-7 días laborables.',
      abierta: false
    },
    {
      pregunta: '¿Qué peso y dimensiones máximas puedo enviar?',
      respuesta: 'El peso máximo por bulto es de 30 kg. Las dimensiones máximas son 150 cm de largo y 300 cm de suma de largo + perímetro. Para envíos especiales, contacta con nuestro equipo.',
      abierta: false
    },
    {
      pregunta: '¿Cómo se calcula el precio del envío?',
      respuesta: 'El precio se calcula en función del peso del paquete, la zona de destino (Península, Baleares o Canarias) y el tipo de tarifa seleccionado (estándar o urgente). Puedes usar la calculadora de tarifas para obtener un presupuesto al instante.',
      abierta: false
    },
    {
      pregunta: '¿Qué métodos de pago aceptan?',
      respuesta: 'Aceptamos pago con tarjeta de crédito y débito (Visa, Mastercard, American Express) a través de la plataforma segura Stripe. Todos los pagos están cifrados y protegidos.',
      abierta: false
    },
    {
      pregunta: '¿Puedo cancelar un envío?',
      respuesta: 'Un envío puede cancelarse antes de que sea recogido por el transportista. Una vez admitido en la red logística, no es posible la cancelación. Contacta con atención al cliente lo antes posible.',
      abierta: false
    },
    {
      pregunta: '¿Qué ocurre si no estoy en casa cuando llega el repartidor?',
      respuesta: 'Se realizarán hasta 2 intentos de entrega. Si no es posible, el paquete quedará disponible para recogida en la oficina NexoPostal más cercana al destino durante 15 días.',
      abierta: false
    },
    {
      pregunta: '¿Los envíos incluyen seguro?',
      respuesta: 'Sí, todos los envíos incluyen un seguro básico que cubre hasta 50€ en caso de pérdida o daño. Los clientes empresa pueden contratar coberturas ampliadas de hasta 1.000€.',
      abierta: false
    }
  ];

  constructor(private router: Router) {}

  toggleFaq(index: number): void {
    this.faqs[index].abierta = !this.faqs[index].abierta;
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
