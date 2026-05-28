import {
  Component, Input, Output, EventEmitter, OnDestroy, signal, OnChanges, SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of, map } from 'rxjs';
import { OficinasService, Oficina, SugerenciaLocal } from '../../services/oficinas.service';

/** Buscador compacto de oficinas sin mapa, embebible en formularios.
 *  Soporta búsqueda por Código Postal, Dirección y (opcionalmente) Ubicación actual.
 */
@Component({
  selector: 'app-buscador-oficina-inline',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './buscador-oficina-inline.component.html'
})
export class BuscadorOficinaInlineComponent implements OnChanges, OnDestroy {
  /** Si false, oculta la pestaña "Mi Ubicación". */
  @Input() permitirUbicacion = true;

  /** Oficina pre-seleccionada (para rellenar el estado inicial). */
  @Input() oficinaInicial: Oficina | null = null;

  /** Emite cada vez que el usuario selecciona o limpia una oficina. */
  @Output() oficinaCambiada = new EventEmitter<Oficina | null>();

  // ─── estado interno ────────────────────────────────────────────────────────
  searchType  = signal<'codigoPostal' | 'direccion' | 'ubicacionActual'>('codigoPostal');
  searchQuery = signal('');
  isSearching = signal(false);

  sugerenciasLocales   = signal<string[]>([]);
  sugerenciasDireccion = signal<SugerenciaLocal[]>([]);
  mostrarSugerencias   = signal(false);

  resultados          = signal<Oficina[]>([]);
  mostrarResultados   = signal(false);
  oficina             = signal<Oficina | null>(null);

  mensajeError = signal<string | null>(null);

  private coordenadas: { lat: number; lng: number } | null = null;
  private searchSubject = new Subject<string>();
  private sub = this.searchSubject.pipe(
    debounceTime(300),
    distinctUntilChanged(),
    switchMap(query => {
      if (this.searchType() === 'codigoPostal' && query.length >= 2)
        return this.oficinasService.obtenerSugerenciasCodigoPostal(query)
               .pipe(map(d => ({ tipo: 'cp' as const, datos: d })));
      if (this.searchType() === 'direccion' && query.length >= 2)
        return this.oficinasService.obtenerSugerenciasDireccion(query)
               .pipe(map(d => ({ tipo: 'dir' as const, datos: d })));
      return of({ tipo: 'none' as const, datos: [] as any[] });
    })
  ).subscribe(r => {
    if (r.tipo === 'cp') {
      this.sugerenciasLocales.set(r.datos);
      this.sugerenciasDireccion.set([]);
    } else if (r.tipo === 'dir') {
      this.sugerenciasDireccion.set(r.datos);
      this.sugerenciasLocales.set([]);
    } else {
      this.sugerenciasLocales.set([]);
      this.sugerenciasDireccion.set([]);
    }
    this.mostrarSugerencias.set(r.datos.length > 0);
  });

  constructor(private oficinasService: OficinasService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['oficinaInicial']) {
      this.oficina.set(this.oficinaInicial);
      if (this.oficinaInicial) {
        this.mostrarResultados.set(false);
        this.resultados.set([]);
      }
    }
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  // ─── tabs ──────────────────────────────────────────────────────────────────

  cambiarTipo(tipo: 'codigoPostal' | 'direccion' | 'ubicacionActual'): void {
    this.searchType.set(tipo);
    this.searchQuery.set('');
    this.coordenadas = null;
    this.sugerenciasLocales.set([]);
    this.sugerenciasDireccion.set([]);
    this.mostrarSugerencias.set(false);
    this.resultados.set([]);
    this.mostrarResultados.set(false);
    this.mensajeError.set(null);

    if (tipo === 'ubicacionActual') this.buscarPorUbicacion();
  }

  // ─── input ─────────────────────────────────────────────────────────────────

  onInput(value: string): void {
    if (this.searchType() === 'codigoPostal') value = value.replace(/\D/g, '').slice(0, 5);
    this.searchQuery.set(value);
    this.coordenadas = null;
    if (value.length >= 2) this.searchSubject.next(value);
    else {
      this.sugerenciasLocales.set([]);
      this.sugerenciasDireccion.set([]);
      this.mostrarSugerencias.set(false);
    }
  }

  // ─── sugerencias ───────────────────────────────────────────────────────────

  elegirSugerenciaCp(cp: string): void {
    this.searchQuery.set(cp);
    this.sugerenciasLocales.set([]);
    this.mostrarSugerencias.set(false);
    setTimeout(() => this.buscar(), 50);
  }

  elegirSugerenciaDir(s: SugerenciaLocal): void {
    this.searchQuery.set(s.texto);
    this.sugerenciasDireccion.set([]);
    this.mostrarSugerencias.set(false);
    this.coordenadas = { lat: s.lat, lng: s.lng };
    setTimeout(() => this.buscar(), 50);
  }

  // ─── búsqueda ──────────────────────────────────────────────────────────────

  buscar(): void {
    const q = this.searchQuery().trim();
    this.mensajeError.set(null);

    if (!q) {
      this.mensajeError.set('Introduce un valor para buscar.');
      return;
    }
    if (this.searchType() === 'codigoPostal' && q.replace(/\D/g, '').length !== 5) {
      this.mensajeError.set('El código postal debe tener 5 dígitos.');
      return;
    }

    this.isSearching.set(true);
    this.mostrarResultados.set(false);

    if (this.coordenadas) {
      this.buscarPorCercania();
    } else {
      const obs = this.searchType() === 'codigoPostal'
        ? this.oficinasService.buscarPorCodigoPostal(q)
        : this.oficinasService.buscarPorDireccion(q);

      obs.subscribe({
        next: res => {
          if (res.length === 0) {
            this.mensajeError.set('No se encontraron oficinas para esa búsqueda.');
            this.isSearching.set(false);
            return;
          }
          this.coordenadas = { lat: res[0].coordenadas.lat, lng: res[0].coordenadas.lng };
          this.buscarPorCercania();
        },
        error: () => {
          this.mensajeError.set('Error al buscar oficinas. Inténtalo de nuevo.');
          this.isSearching.set(false);
        }
      });
    }
  }

  private buscarPorCercania(): void {
    this.oficinasService.cargarOficinas().subscribe({
      next: todas => {
        const coord = this.coordenadas!;
        const conDistancia = todas.map(o => ({
          ...o,
          distancia: this.oficinasService.calcularDistancia(
            coord.lat, coord.lng, o.coordenadas.lat, o.coordenadas.lng
          )
        }));
        conDistancia.sort((a, b) => (a.distancia ?? 0) - (b.distancia ?? 0));
        this.resultados.set(conDistancia.slice(0, 10));
        this.mostrarResultados.set(true);
        this.isSearching.set(false);
      },
      error: () => {
        this.mensajeError.set('Error al cargar la lista de oficinas.');
        this.isSearching.set(false);
      }
    });
  }

  private buscarPorUbicacion(): void {
    if (!navigator.geolocation) {
      this.mensajeError.set('Tu navegador no soporta geolocalización.');
      return;
    }
    this.isSearching.set(true);
    navigator.geolocation.getCurrentPosition(
      pos => {
        this.coordenadas = { lat: pos.coords.latitude, lng: pos.coords.longitude };
        this.searchQuery.set('Mi ubicación actual');
        this.buscarPorCercania();
      },
      err => {
        this.isSearching.set(false);
        const msgs: Record<number, string> = {
          1: 'Has denegado el permiso de ubicación. Actívalo en tu navegador.',
          2: 'La información de ubicación no está disponible.',
          3: 'La solicitud de ubicación tardó demasiado. Inténtalo de nuevo.'
        };
        this.mensajeError.set(msgs[err.code] ?? 'No se pudo obtener tu ubicación.');
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
    );
  }

  // ─── selección ─────────────────────────────────────────────────────────────

  elegirOficina(o: Oficina): void {
    this.oficina.set(o);
    this.mostrarResultados.set(false);
    this.resultados.set([]);
    this.oficinaCambiada.emit(o);
  }

  cambiarOficina(): void {
    this.oficina.set(null);
    this.searchQuery.set('');
    this.coordenadas = null;
    this.mensajeError.set(null);
    this.oficinaCambiada.emit(null);
  }

  limpiar(): void {
    this.cambiarOficina();
    this.sugerenciasLocales.set([]);
    this.sugerenciasDireccion.set([]);
    this.mostrarSugerencias.set(false);
    this.resultados.set([]);
    this.mostrarResultados.set(false);
  }
}
