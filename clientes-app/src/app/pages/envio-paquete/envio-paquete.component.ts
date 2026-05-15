import { Component, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NotificacionService } from '../../services/notificacion.service';
import { ConfirmacionService } from '../../services/confirmacion.service';
import { OficinasService, Oficina } from '../../services/oficinas.service';
import { PagosService, CrearSesionPagoRequest } from '../../services/pagos.service';
import { AuthService } from '../../services/auth.service';
import { PerfilService, DireccionFavoritaDto } from '../../services/perfil.service';
import { TarifasService } from '../../services/tarifas.service';

interface RateOption {
  tipoTarifa: 'Estandar' | 'Premium';
  name: string;
  description: string;
  price: number;
  deliveryTime: string;
}

type TipoEntrega = 'oficina' | 'direccion';

interface DatosPersona {
  nombre: string;
  apellidos: string;
  telefono: string;
  email: string;
  dni: string;
  tipoEntrega: TipoEntrega;
  // Campos dirección
  direccion: string;
  codigoPostal: string;
  ciudad: string;
  provincia: string;
  // Oficina seleccionada
  oficina: Oficina | null;
}

@Component({
  selector: 'app-envio-paquete',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './envio-paquete.component.html',
  styleUrls: ['./envio-paquete.component.css']
})
export class EnvioPaqueteComponent {
  currentStep = signal(1);
  totalSteps = 3;
  
  // Datos del formulario
  cpOrigin = signal('');
  cpDest = signal('');
  weight = signal<number | null>(null);
  length = signal<number | null>(null);
  width = signal<number | null>(null);
  height = signal<number | null>(null);
  
  // Tarifas calculadas
  ratesOptions = signal<RateOption[]>([]);
  selectedRate = signal<RateOption | null>(null);
  isCalculating = signal(false);
  procesandoPago = signal(false);
  
  // Datos de remitente
  remitente = signal<DatosPersona>({
    nombre: '', apellidos: '', telefono: '', email: '', dni: '',
    tipoEntrega: 'direccion',
    direccion: '', codigoPostal: '', ciudad: '', provincia: '',
    oficina: null
  });

  // Datos de destinatario
  destinatario = signal<DatosPersona>({
    nombre: '', apellidos: '', telefono: '', email: '', dni: '',
    tipoEntrega: 'direccion',
    direccion: '', codigoPostal: '', ciudad: '', provincia: '',
    oficina: null
  });

  // Agenda de direcciones favoritas
  direccionesGuardadas = signal<DireccionFavoritaDto[]>([]);
  showSelectorRemitente = signal(false);
  showSelectorDestinatario = signal(false);

  // Búsqueda de oficinas
  senderOficinaBusqueda = signal('');
  recipientOficinaBusqueda = signal('');
  senderOficinas = signal<Oficina[]>([]);
  recipientOficinas = signal<Oficina[]>([]);
  senderShowOficinas = signal(false);
  recipientShowOficinas = signal(false);

  // Canarias: CP que empiezan por 35 (Las Palmas) o 38 (Sta. Cruz de Tenerife)
  requiereDni = computed(() => {
    const cpOrigen = this.cpOrigin();
    const cpDestino = this.cpDest();
    return this.esCanarias(cpOrigen) || this.esCanarias(cpDestino);
  });

  constructor(
    private router: Router,
    private notificacion: NotificacionService,
    private confirmacionService: ConfirmacionService,
    private oficinasService: OficinasService,
    private pagosService: PagosService,
    private authService: AuthService,
    private perfilService: PerfilService,
    private tarifasService: TarifasService
  ) {}

  ngOnInit(): void {
    window.scrollTo(0, 0);
    this.cargarDireccionesGuardadas();
  }

  cargarDireccionesGuardadas(): void {
    if (this.authService.isAuthenticated()) {
      this.perfilService.obtenerDirecciones().subscribe({
        next: (dirs) => this.direccionesGuardadas.set(dirs),
        error: () => {} // Silencioso: el usuario puede no tener perfil aún
      });
    }
  }

  aplicarDireccion(quien: 'remitente' | 'destinatario', dir: DireccionFavoritaDto): void {
    const datos: Partial<DatosPersona> = {
      nombre: dir.nombreDestinatario.split(' ')[0] || dir.nombreDestinatario,
      apellidos: dir.nombreDestinatario.split(' ').slice(1).join(' '),
      telefono: dir.telefono || '',
      tipoEntrega: 'direccion' as TipoEntrega,
      direccion: dir.direccion,
      codigoPostal: dir.codigoPostal,
      ciudad: dir.ciudad,
      provincia: dir.provincia,
      oficina: null
    };
    if (quien === 'remitente') {
      this.remitente.update(r => ({ ...r, ...datos }));
      this.showSelectorRemitente.set(false);
    } else {
      this.destinatario.update(d => ({ ...d, ...datos }));
      this.showSelectorDestinatario.set(false);
    }
    this.notificacion.exito('Dirección aplicada', `Se han rellenado los datos desde "${dir.alias}".`);
  }

  goToStep(step: number): void {
    if (step === 2 && this.currentStep() === 1) {
      if (this.validateStep1()) {
        this.calculateRates();
        this.currentStep.set(step);
      }
    } else if (step === 3 && this.currentStep() === 2) {
      if (this.selectedRate()) {
        this.currentStep.set(step);
      } else {
        this.notificacion.aviso('Tarifa requerida', 'Selecciona una tarifa antes de continuar.');
      }
    } else if (step < this.currentStep()) {
      this.currentStep.set(step);
    }
  }

  validateStep1(): boolean {
    if (!this.cpOrigin() || !this.cpDest() || !this.weight() || 
        !this.length() || !this.width() || !this.height()) {
      this.notificacion.aviso('Campos incompletos', 'Completa todos los campos requeridos.');
      return false;
    }

    const weight = this.weight() || 0;
    const length = this.length() || 0;
    const width = this.width() || 0;
    const height = this.height() || 0;

    // Validación de dimensiones mínimas (10x15cm para etiqueta)
    const dimensionesOrdenadas = [length, width, height].sort((a, b) => b - a);
    if (dimensionesOrdenadas[0] < 15 || dimensionesOrdenadas[1] < 10) {
      this.notificacion.aviso('Dimensiones insuficientes', 'Las dimensiones mínimas son 10x15 cm para poder colocar la etiqueta.');
      return false;
    }

    if (weight > 30) {
      this.notificacion.aviso('Peso excedido', 'El peso máximo permitido es de 30 kg.');
      return false;
    }

    if (length > 170) {
      this.notificacion.aviso('Largo excedido', 'El lado mayor máximo permitido es de 170 cm.');
      return false;
    }

    const sumaDimensiones = length + width + height;
    if (sumaDimensiones > 210) {
      this.notificacion.aviso(
        'Dimensiones extra',
        `La suma de dimensiones supera 210 cm. Se aplicará un recargo del 35%. (Actual: ${sumaDimensiones} cm)`
      );
    }

    return true;
  }

  calculateRates(): void {
    this.isCalculating.set(true);
    
    const weight = this.weight() || 1;
    const length = this.length() || 0;
    const width = this.width() || 0;
    const height = this.height() || 0;

    this.tarifasService.consultarTarifas({
      peso: weight,
      largo: length,
      ancho: width,
      alto: height,
      codigoPostalOrigen: this.cpOrigin(),
      codigoPostalDestino: this.cpDest()
    }).subscribe({
      next: (response) => {
        this.ratesOptions.set(
          response.tarifas.map(tarifa => ({
            tipoTarifa: tarifa.nombre.toLowerCase() === 'premium' ? 'Premium' : 'Estandar',
            name: `Envío ${tarifa.nombre}`,
            description: tarifa.descripcion,
            deliveryTime: tarifa.tiempoEntregaEstimado,
            price: tarifa.precioTotal
          }))
        );
        this.isCalculating.set(false);
      },
      error: (err) => {
        this.isCalculating.set(false);
        this.notificacion.errorHttp(err, 'No se pudieron calcular las tarifas');
      }
    });
  }

  selectRate(rate: RateOption): void {
    this.selectedRate.set(rate);
  }

  // --- Métodos para remitente/destinatario ---

  updateRemitente(campo: keyof DatosPersona, valor: any): void {
    this.remitente.update(r => ({ ...r, [campo]: valor }));
  }

  updateDestinatario(campo: keyof DatosPersona, valor: any): void {
    this.destinatario.update(d => ({ ...d, [campo]: valor }));
  }

  setTipoEntrega(quien: 'remitente' | 'destinatario', tipo: TipoEntrega): void {
    if (quien === 'remitente') {
      this.remitente.update(r => ({ ...r, tipoEntrega: tipo, oficina: null, direccion: '', codigoPostal: '', ciudad: '', provincia: '' }));
      this.senderOficinaBusqueda.set('');
      this.senderOficinas.set([]);
    } else {
      this.destinatario.update(d => ({ ...d, tipoEntrega: tipo, oficina: null, direccion: '', codigoPostal: '', ciudad: '', provincia: '' }));
      this.recipientOficinaBusqueda.set('');
      this.recipientOficinas.set([]);
    }
  }

  buscarOficinas(quien: 'remitente' | 'destinatario'): void {
    const query = quien === 'remitente' ? this.senderOficinaBusqueda() : this.recipientOficinaBusqueda();
    if (!query || query.length < 2) {
      if (quien === 'remitente') this.senderOficinas.set([]);
      else this.recipientOficinas.set([]);
      return;
    }

    this.oficinasService.buscarPorDireccion(query).subscribe(oficinas => {
      const resultado = oficinas.slice(0, 10);
      if (quien === 'remitente') {
        this.senderOficinas.set(resultado);
        this.senderShowOficinas.set(true);
      } else {
        this.recipientOficinas.set(resultado);
        this.recipientShowOficinas.set(true);
      }
    });
  }

  seleccionarOficina(quien: 'remitente' | 'destinatario', oficina: Oficina): void {
    if (quien === 'remitente') {
      this.remitente.update(r => ({ ...r, oficina }));
      this.senderOficinaBusqueda.set(oficina.nombre);
      this.senderShowOficinas.set(false);
    } else {
      this.destinatario.update(d => ({ ...d, oficina }));
      this.recipientOficinaBusqueda.set(oficina.nombre);
      this.recipientShowOficinas.set(false);
    }
  }

  esCanarias(cp: string): boolean {
    if (!cp || cp.length < 2) return false;
    const prefijo = cp.substring(0, 2);
    return prefijo === '35' || prefijo === '38';
  }

  private validarPersona(persona: DatosPersona, label: string): boolean {
    if (!persona.nombre.trim()) {
      this.notificacion.aviso('Campos incompletos', `Introduce el nombre del ${label}.`);
      return false;
    }
    if (!persona.apellidos.trim()) {
      this.notificacion.aviso('Campos incompletos', `Introduce los apellidos del ${label}.`);
      return false;
    }
    if (!persona.telefono.trim()) {
      this.notificacion.aviso('Campos incompletos', `Introduce el teléfono del ${label}.`);
      return false;
    }

    if (persona.tipoEntrega === 'oficina') {
      if (!persona.oficina) {
        this.notificacion.aviso('Oficina requerida', `Selecciona una oficina para el ${label}.`);
        return false;
      }
    } else {
      if (!persona.direccion.trim()) {
        this.notificacion.aviso('Campos incompletos', `Introduce la dirección del ${label}.`);
        return false;
      }
      if (!persona.codigoPostal.trim() || persona.codigoPostal.length !== 5) {
        this.notificacion.aviso('Campos incompletos', `Introduce un código postal válido para el ${label}.`);
        return false;
      }
      if (!persona.ciudad.trim()) {
        this.notificacion.aviso('Campos incompletos', `Introduce la ciudad del ${label}.`);
        return false;
      }
      if (!persona.provincia.trim()) {
        this.notificacion.aviso('Campos incompletos', `Introduce la provincia del ${label}.`);
        return false;
      }
    }

    if (this.requiereDni() && !persona.dni.trim()) {
      this.notificacion.aviso('DNI requerido', `El DNI/NIF del ${label} es obligatorio para envíos a/desde Canarias.`);
      return false;
    }

    return true;
  }

  onSubmit(): void {
    if (!this.authService.isAuthenticated()) {
      this.notificacion.aviso('Iniciar sesión', 'Debes iniciar sesión para realizar un envío.');
      return;
    }

    if (!this.validarPersona(this.remitente(), 'remitente')) return;
    if (!this.validarPersona(this.destinatario(), 'destinatario')) return;

    if (!this.remitente().email.trim()) {
      this.notificacion.aviso('Campos incompletos', 'Introduce el email del remitente para recibir la etiqueta.');
      return;
    }

    const rate = this.selectedRate();
    if (!rate) return;

    const rem = this.remitente();
    const dest = this.destinatario();

    // Construir dirección de origen
    let direccionOrigen = '';
    if (rem.tipoEntrega === 'oficina' && rem.oficina) {
      direccionOrigen = `Oficina: ${rem.oficina.nombre} — ${rem.oficina.direccion}, ${rem.oficina.codigoPostal} ${rem.oficina.ciudad}`;
    } else {
      direccionOrigen = `${rem.direccion}, ${rem.codigoPostal} ${rem.ciudad}, ${rem.provincia}`;
    }

    // Construir dirección de destino
    let direccionDestino = '';
    if (dest.tipoEntrega === 'oficina' && dest.oficina) {
      direccionDestino = `Oficina: ${dest.oficina.nombre} — ${dest.oficina.direccion}, ${dest.oficina.codigoPostal} ${dest.oficina.ciudad}`;
    } else {
      direccionDestino = `${dest.direccion}, ${dest.codigoPostal} ${dest.ciudad}, ${dest.provincia}`;
    }

    const request: CrearSesionPagoRequest = {
      peso: this.weight() || 0,
      dimensiones: `${this.length()}x${this.width()}x${this.height()} cm`,
      codigoPostalOrigen: this.cpOrigin(),
      codigoPostalDestino: this.cpDest(),
      tipoTarifa: rate.tipoTarifa,
      coste: rate.price,
      tiempoEntregaEstimado: rate.deliveryTime,
      nombreRemitente: rem.nombre,
      apellidosRemitente: rem.apellidos,
      telefonoRemitente: rem.telefono,
      emailRemitente: rem.email,
      dniRemitente: rem.dni || undefined,
      direccionOrigen,
      nombreDestinatario: dest.nombre,
      apellidosDestinatario: dest.apellidos,
      telefonoDestinatario: dest.telefono,
      emailDestinatario: dest.email || undefined,
      dniDestinatario: dest.dni || undefined,
      direccionDestino,
      urlBase: window.location.origin
    };

    this.procesandoPago.set(true);

    this.pagosService.crearSesionPago(request).subscribe({
      next: (res) => {
        // Redirigir a Stripe Checkout
        window.location.href = res.sessionUrl;
      },
      error: (err) => {
        this.procesandoPago.set(false);
        this.notificacion.errorHttp(err, 'Error al iniciar el pago');
      }
    });
  }

  async cancelShipment(): Promise<void> {
    const ok = await this.confirmacionService.confirmar({
      titulo: 'Cancelar envío',
      mensaje: '¿Seguro que quieres cancelar el envío? Se perderán los datos introducidos.',
      textoConfirmar: 'Sí, cancelar',
      tipo: 'peligro'
    });
    if (!ok) return;
    this.router.navigate(['/']);
  }
}
