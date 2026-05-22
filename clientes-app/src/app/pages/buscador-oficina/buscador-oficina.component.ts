import { Component, signal, AfterViewInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OficinasService, Oficina, SugerenciaLocal } from '../../services/oficinas.service';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of, map } from 'rxjs';
import * as L from 'leaflet';
import { NavbarPublicoComponent } from '../../components/navbar-publico/navbar-publico.component';
import { FooterPublicoComponent } from '../../components/footer-publico/footer-publico.component';

@Component({
  selector: 'app-buscador-oficina',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarPublicoComponent, FooterPublicoComponent],
  templateUrl: './buscador-oficina.component.html',
  styleUrl: './buscador-oficina.component.css'
})
export class BuscadorOficinaComponent implements AfterViewInit, OnDestroy {
  // Búsqueda
  searchQuery = signal('');
  searchType = signal<'direccion' | 'codigoPostal' | 'ubicacionActual'>('codigoPostal');
  isSearching = signal(false);
  
  // Sugerencias (ambas usan datos locales del JSON)
  sugerenciasDireccion = signal<SugerenciaLocal[]>([]);
  sugerenciasLocales = signal<string[]>([]);
  mostrarSugerencias = signal(false);
  private searchSubject = new Subject<string>();
  
  // Resultados
  oficinas = signal<Oficina[]>([]);
  oficinaSeleccionada = signal<Oficina | null>(null);
  mostrarResultados = signal(false);
  
  // Mensajes de advertencia
  mensajeAdvertencia = signal<string | null>(null);
  tipoMensaje = signal<'info' | 'warning' | 'error'>('info');
  
  // Mapa
  private map: L.Map | null = null;
  private markers: L.Marker[] = [];
  coordenadasBusqueda: { lat: number; lng: number } | null = null;

  constructor(
    private router: Router,
    private oficinasService: OficinasService
  ) {
    // Configurar autocompletado: todo desde el JSON local
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        if (this.searchType() === 'codigoPostal' && query.length >= 2) {
          return this.oficinasService.obtenerSugerenciasCodigoPostal(query).pipe(
            map(codigos => ({ tipo: 'cp' as const, datos: codigos }))
          );
        } 
        if (this.searchType() === 'direccion' && query.length >= 2) {
          return this.oficinasService.obtenerSugerenciasDireccion(query).pipe(
            map(sugerencias => ({ tipo: 'direccion' as const, datos: sugerencias }))
          );
        }
        return of({ tipo: 'ninguno' as const, datos: [] as any[] });
      })
    ).subscribe(resultado => {
      if (resultado.tipo === 'cp') {
        this.sugerenciasLocales.set(resultado.datos as string[]);
        this.sugerenciasDireccion.set([]);
        this.mostrarSugerencias.set(resultado.datos.length > 0);
      } else if (resultado.tipo === 'direccion') {
        this.sugerenciasDireccion.set(resultado.datos as SugerenciaLocal[]);
        this.sugerenciasLocales.set([]);
        this.mostrarSugerencias.set(resultado.datos.length > 0);
      } else {
        this.sugerenciasDireccion.set([]);
        this.sugerenciasLocales.set([]);
        this.mostrarSugerencias.set(false);
      }
    });
  }

  ngOnInit(): void {
    window.scrollTo(0, 0);
  }

  ngAfterViewInit(): void {
    // El mapa se inicializará después de realizar una búsqueda
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }

  cambiarTipoBusqueda(tipo: 'direccion' | 'codigoPostal' | 'ubicacionActual'): void {
    this.searchType.set(tipo);
    // Limpiar formulario automáticamente
    this.searchQuery.set('');
    this.sugerenciasDireccion.set([]);
    this.sugerenciasLocales.set([]);
    this.mostrarSugerencias.set(false);
    this.oficinas.set([]);
    this.oficinaSeleccionada.set(null);
    this.mostrarResultados.set(false);
    this.coordenadasBusqueda = null;
    this.mensajeAdvertencia.set(null);
    
    // Si selecciona ubicación actual, buscar automáticamente
    if (tipo === 'ubicacionActual') {
      this.buscarPorUbicacionActual();
    }
  }

  onSearchInput(value: string): void {
    this.searchQuery.set(value);
    this.coordenadasBusqueda = null;
    
    if ((this.searchType() === 'direccion' || this.searchType() === 'codigoPostal') && value.length >= 2) {
      this.searchSubject.next(value);
    } else {
      this.sugerenciasDireccion.set([]);
      this.sugerenciasLocales.set([]);
      this.mostrarSugerencias.set(false);
    }
  }

  seleccionarSugerenciaLocal(sugerencia: string): void {
    this.searchQuery.set(sugerencia);
    this.sugerenciasLocales.set([]);
    this.mostrarSugerencias.set(false);
    
    // Para código postal las coordenadas se obtienen del JSON al buscar
    setTimeout(() => this.buscarOficinas(), 100);
  }

  seleccionarSugerencia(sugerencia: SugerenciaLocal): void {
    this.searchQuery.set(sugerencia.texto);
    this.sugerenciasDireccion.set([]);
    this.mostrarSugerencias.set(false);
    
    // Usar las coordenadas de la oficina que coincidió
    this.coordenadasBusqueda = {
      lat: sugerencia.lat,
      lng: sugerencia.lng
    };
    
    // Buscar automáticamente después de seleccionar
    setTimeout(() => this.buscarOficinas(), 100);
  }

  buscarOficinas(): void {
    const query = this.searchQuery().trim();
    
    if (!query) {
      this.mostrarMensaje('Por favor, introduce una dirección o código postal.', 'warning');
      return;
    }

    // Validar código postal si es el tipo de búsqueda
    if (this.searchType() === 'codigoPostal') {
      const cpLimpio = query.replace(/\D/g, '');
      if (cpLimpio.length !== 5) {
        this.mostrarMensaje('Por favor, introduce un código postal válido (5 dígitos).', 'warning');
        return;
      }
    }

    this.isSearching.set(true);
    this.oficinaSeleccionada.set(null);
    this.mensajeAdvertencia.set(null);

    // Si ya tenemos coordenadas (vienen de seleccionar una sugerencia), buscar por cercanía
    if (this.coordenadasBusqueda) {
      this.buscarPorCercaniaGlobal();
    } else {
      // Búsqueda textual directa contra el JSON
      this.buscarOficinasLocales(query);
    }
  }

  private buscarOficinasLocales(query: string): void {
    // CASO 1: Ya tenemos coordenadas precisas (por sugerencia o geolocalización)
    if (this.coordenadasBusqueda) {
      this.buscarPorCercaniaGlobal();
      return;
    }

    // CASO 2: Búsqueda textual (CP o dirección escrita a mano sin seleccionar sugerencia)
    const busqueda = this.searchType() === 'codigoPostal'
      ? this.oficinasService.buscarPorCodigoPostal(query)
      : this.oficinasService.buscarPorDireccion(query);

    busqueda.subscribe({
      next: (resultados) => {
        if (resultados.length > 0) {
          // Usamos la primera oficina como punto central para ordenar por cercanía
          this.coordenadasBusqueda = {
            lat: resultados[0].coordenadas.lat,
            lng: resultados[0].coordenadas.lng
          };
          this.buscarPorCercaniaGlobal();
        } else {
          this.isSearching.set(false);
          this.mostrarMensaje('No se encontraron oficinas para esa búsqueda. Prueba con otra dirección o código postal.', 'warning');
        }
      },
      error: (err) => {
        this.isSearching.set(false);
        console.error('Error buscando oficinas:', err);
        this.mostrarMensaje('Error al buscar oficinas.', 'error');
      }
    });
  }

  private mostrarMensaje(mensaje: string, tipo: 'info' | 'warning' | 'error'): void {
    this.mensajeAdvertencia.set(mensaje);
    this.tipoMensaje.set(tipo);
    // Ocultar el mensaje automáticamente después de 8 segundos
    setTimeout(() => {
      this.mensajeAdvertencia.set(null);
    }, 8000);
  }

  cerrarMensaje(): void {
    this.mensajeAdvertencia.set(null);
  }

  buscarPorUbicacionActual(): void {
    if (!navigator.geolocation) {
      this.mostrarMensaje('Tu navegador no soporta geolocalización.', 'error');
      return;
    }

    this.isSearching.set(true);
    this.oficinaSeleccionada.set(null);
    this.mensajeAdvertencia.set(null);
    this.mostrarMensaje('Obteniendo tu ubicación...', 'info');

    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.coordenadasBusqueda = {
          lat: position.coords.latitude,
          lng: position.coords.longitude
        };
        
        console.log('Ubicación actual obtenida:', this.coordenadasBusqueda);
        this.searchQuery.set('Mi ubicación actual');
        this.mostrarMensaje('Ubicación obtenida. Buscando oficinas cercanas...', 'info');
        
        // Buscar oficinas más cercanas
        this.buscarPorCercaniaGlobal();
      },
      (error) => {
        this.isSearching.set(false);
        console.error('Error obteniendo ubicación:', error);
        
        let mensaje = 'No se pudo obtener tu ubicación.';
        if (error.code === error.PERMISSION_DENIED) {
          mensaje = 'Has denegado el permiso de ubicación. Por favor, actívalo en la configuración de tu navegador.';
        } else if (error.code === error.POSITION_UNAVAILABLE) {
          mensaje = 'La información de ubicación no está disponible.';
        } else if (error.code === error.TIMEOUT) {
          mensaje = 'La solicitud de ubicación ha tardado demasiado tiempo.';
        }
        
        this.mostrarMensaje(mensaje, 'error');
      },
      {
        enableHighAccuracy: true,
        timeout: 10000,
        maximumAge: 0
      }
    );
  }

  // Busca en TODO el listado de oficinas y ordena por distancia a this.coordenadasBusqueda
  private buscarPorCercaniaGlobal(): void {
    if (!this.coordenadasBusqueda) return;

    this.oficinasService.cargarOficinas().subscribe({
      next: (todas) => {
        // Al pasar todas a procesarResultados con coordenadasBusqueda set, 
        // él se encarga de calcular distancia, ordenar y cortar.
        this.procesarResultados(todas);
      },
      error: (err) => {
        console.error('Error cargando todas las oficinas para cercanía:', err);
        this.isSearching.set(false);
      }
    });
  }

  private procesarResultados(resultados: Oficina[]): void {
    console.log('Procesando', resultados.length, 'oficinas');
    
    // Calcular distancias desde las coordenadas de búsqueda
    if (this.coordenadasBusqueda) {
      resultados = resultados.map(oficina => ({
        ...oficina,
        distancia: this.oficinasService.calcularDistancia(
          this.coordenadasBusqueda!.lat,
          this.coordenadasBusqueda!.lng,
          oficina.coordenadas.lat,
          oficina.coordenadas.lng
        )
      }));

      // Ordenar por distancia (más cercanas primero)
      resultados.sort((a, b) => (a.distancia || 0) - (b.distancia || 0));
      
      // Limitar a las 20 oficinas más cercanas
      resultados = resultados.slice(0, 20);
    }

    this.oficinas.set(resultados);
    this.mostrarResultados.set(true);
    this.isSearching.set(false);

    if (resultados.length === 0) {
      this.mostrarMensaje('No se encontraron oficinas cercanas. Intenta con otra ubicación.', 'warning');
    } else {
      console.log('Mostrando', resultados.length, 'oficinas más cercanas');
      // Inicializar mapa después de mostrar resultados
      setTimeout(() => this.inicializarMapa(), 100);
    }
  }

  seleccionarOficina(oficina: Oficina): void {
    this.oficinaSeleccionada.set(oficina);
    // Scroll suave hacia los detalles
    setTimeout(() => {
      document.getElementById('detalles-oficina')?.scrollIntoView({ 
        behavior: 'smooth', 
        block: 'start' 
      });
    }, 100);
  }

  inicializarMapa(): void {
    // Limpiar mapa existente
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
    this.markers = [];

    // Crear nuevo mapa
    const mapElement = document.getElementById('mapa-oficinas');
    if (!mapElement) return;

    const center = this.coordenadasBusqueda || { lat: 40.4168, lng: -3.7038 };
    this.map = L.map('mapa-oficinas').setView([center.lat, center.lng], 13);

    // Agregar capa de OpenStreetMap
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      maxZoom: 19
    }).addTo(this.map);

    // Icono personalizado para la ubicación buscada
    const iconoBusqueda = L.icon({
      iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-red.png',
      shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      popupAnchor: [1, -34],
      shadowSize: [41, 41]
    });

    // Marcador de la ubicación buscada
    const marcadorBusqueda = L.marker([center.lat, center.lng], { icon: iconoBusqueda })
      .addTo(this.map)
      .bindPopup('<b>Tu búsqueda</b><br>' + this.searchQuery())
      .openPopup();

    // Icono personalizado para oficinas
    const iconoOficina = L.icon({
      iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-blue.png',
      shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      popupAnchor: [1, -34],
      shadowSize: [41, 41]
    });

    // Agregar marcadores para cada oficina
    const bounds = L.latLngBounds([[center.lat, center.lng]]);
    this.oficinas().forEach(oficina => {
      const marker = L.marker(
        [oficina.coordenadas.lat, oficina.coordenadas.lng],
        { icon: iconoOficina }
      )
        .addTo(this.map!)
        .bindPopup(`
          <div class="p-2">
            <b class="text-base">${oficina.nombre}</b><br>
            <span class="text-sm text-gray-600">${oficina.direccion}</span><br>
            <span class="text-sm text-gray-600">${oficina.ciudad}, ${oficina.codigoPostal}</span><br>
            <span class="text-sm font-semibold text-blue-600">📞 ${oficina.telefono}</span>
            ${oficina.distancia ? `<br><span class="text-sm text-gray-500">📍 ${oficina.distancia.toFixed(1)} km</span>` : ''}
          </div>
        `);

      marker.on('click', () => {
        this.seleccionarOficina(oficina);
      });

      this.markers.push(marker);
      bounds.extend([oficina.coordenadas.lat, oficina.coordenadas.lng]);
    });

    // Ajustar vista para mostrar todos los marcadores
    this.map.fitBounds(bounds, { padding: [50, 50] });
  }

  limpiarBusqueda(): void {
    this.searchQuery.set('');
    this.oficinas.set([]);
    this.oficinaSeleccionada.set(null);
    this.mostrarResultados.set(false);
    this.coordenadasBusqueda = null;
    this.mensajeAdvertencia.set(null);
    
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
    this.markers = [];
  }

  volverInicio(): void {
    this.router.navigate(['/']);
  }

  verEnMapa(oficina: Oficina): void {
    // Abrir en Google Maps
    const url = `https://www.google.com/maps/search/?api=1&query=${oficina.coordenadas.lat},${oficina.coordenadas.lng}`;
    window.open(url, '_blank');
  }

  llamarOficina(telefono: string): void {
    window.location.href = `tel:${telefono}`;
  }
}
