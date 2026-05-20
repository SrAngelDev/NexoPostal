import { Component, signal, computed, OnInit, HostListener, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { PerfilService, PerfilDto, ActualizarPerfilDto, DireccionFavoritaDto, CrearDireccionFavoritaDto } from '../../services/perfil.service';
import { UsuarioService, UsuarioInfoDto, ActualizarUsuarioDto, CambiarPasswordDto } from '../../services/usuario.service';
import { EnviosService, EnvioResponse } from '../../services/envios.service';
import { NotificacionService } from '../../services/notificacion.service';
import { ConfirmacionService } from '../../services/confirmacion.service';
import { NavbarPublicoComponent } from '../../components/navbar-publico/navbar-publico.component';

@Component({
  selector: 'app-panel-usuario',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarPublicoComponent],
  templateUrl: './panel-usuario.component.html',
  styleUrl: './panel-usuario.component.css'
})
export class PanelUsuarioComponent implements OnInit {
  // Estado del usuario
  currentUser = signal<any>(null);
  showUserMenu = signal(false);

  // Tabs
  activeTab = signal<'perfil' | 'direcciones' | 'envios'>('perfil');

  // Perfil
  perfil = signal<PerfilDto | null>(null);
  editandoPerfil = signal(false);
  perfilForm = signal<ActualizarPerfilDto>({ dni: '', telefono: '', direccionPredeterminada: '' });
  guardandoPerfil = signal(false);
  perfilError = signal('');
  perfilExito = signal('');

  // Datos de usuario (Identity)
  usuarioInfo = signal<UsuarioInfoDto | null>(null);
  usuarioForm = signal<ActualizarUsuarioDto>({ nombreCompleto: '', email: '', phoneNumber: '' });
  guardandoUsuario = signal(false);
  usuarioError = signal('');
  usuarioExito = signal('');

  // Cambio de contraseña
  mostrarCambioPassword = signal(false);
  passwordForm = signal<CambiarPasswordDto & { confirmar: string }>({ passwordActual: '', nuevaPassword: '', confirmar: '' });
  guardandoPassword = signal(false);
  passwordError = signal('');
  passwordExito = signal('');

  // Direcciones
  direcciones = signal<DireccionFavoritaDto[]>([]);
  mostrarFormDireccion = signal(false);
  editandoDireccionId = signal<number | null>(null);
  nuevaDireccion = signal<CrearDireccionFavoritaDto>({
    alias: '', nombreDestinatario: '', direccion: '', codigoPostal: '', ciudad: '', provincia: '', telefono: ''
  });
  guardandoDireccion = signal(false);
  direccionError = signal('');

  // Envíos
  envios = signal<EnvioResponse[]>([]);
  cargandoEnvios = signal(false);

  // Loading general
  cargando = signal(true);

  // Computed: dirección seleccionada en formulario de edición
  direccionSeleccionada = computed(() => {
    const idStr = this.perfilForm().direccionPredeterminada;
    if (!idStr) return null;
    const id = parseInt(idStr, 10);
    return isNaN(id) ? null : this.direcciones().find(d => d.id === id) || null;
  });

  // Computed: dirección predeterminada del perfil guardado
  direccionPredeterminadaInfo = computed(() => {
    const perfil = this.perfil();
    if (!perfil?.direccionPredeterminada) return null;
    const id = parseInt(perfil.direccionPredeterminada, 10);
    return isNaN(id) ? null : this.direcciones().find(d => d.id === id) || null;
  });

  constructor(
    private router: Router,
    private authService: AuthService,
    private perfilService: PerfilService,
    private usuarioService: UsuarioService,
    private enviosService: EnviosService,
    private notificacion: NotificacionService,
    private confirmacionService: ConfirmacionService,
    private elementRef: ElementRef
  ) {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser.set(user);
      if (!user) {
        this.router.navigate(['/']);
      }
    });
  }

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
      return;
    }
    this.cargarPerfil();
    this.cargarDirecciones();
    this.cargarEnvios();
    this.cargarUsuario();
  }

  // ===== NAVEGACIÓN =====

  navigateHome(): void {
    this.router.navigate(['/']);
  }

  toggleUserMenu(): void {
    this.showUserMenu.set(!this.showUserMenu());
  }

  closeUserMenu(): void {
    this.showUserMenu.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.showUserMenu() && !this.elementRef.nativeElement.querySelector('.user-menu-container')?.contains(event.target)) {
      this.closeUserMenu();
    }
  }

  cambiarTab(tab: 'perfil' | 'direcciones' | 'envios'): void {
    this.activeTab.set(tab);
    this.perfilError.set('');
    this.perfilExito.set('');
    this.usuarioError.set('');
    this.usuarioExito.set('');
    this.passwordError.set('');
    this.passwordExito.set('');
    this.direccionError.set('');
  }

  // ===== USUARIO (Identity) =====

  cargarUsuario(): void {
    this.usuarioService.obtenerUsuario().subscribe({
      next: (u) => this.usuarioInfo.set(u),
      error: () => this.usuarioInfo.set(null)
    });
  }

  // ===== PERFIL =====

  cargarPerfil(): void {
    this.perfilService.obtenerPerfil().subscribe({
      next: (perfil) => {
        this.perfil.set(perfil);
        this.perfilForm.set({
          dni: perfil.dni || '',
          telefono: perfil.telefono || '',
          direccionPredeterminada: perfil.direccionPredeterminada || ''
        });
        this.cargando.set(false);
      },
      error: (err) => {
        // Si 404, el perfil aún no existe — permitimos crearlo
        if (err.status === 404) {
          this.perfil.set(null);
          this.editandoPerfil.set(true);
        }
        this.cargando.set(false);
      }
    });
  }

  iniciarEdicionPerfil(): void {
    const p = this.perfil();
    const u = this.usuarioInfo();
    this.perfilForm.set({
      dni: p?.dni || '',
      telefono: p?.telefono || '',
      direccionPredeterminada: p?.direccionPredeterminada || ''
    });
    this.usuarioForm.set({
      nombreCompleto: u?.nombreCompleto || '',
      email: u?.email || '',
      phoneNumber: u?.phoneNumber || ''
    });
    this.editandoPerfil.set(true);
    this.perfilError.set('');
    this.perfilExito.set('');
    this.usuarioError.set('');
    this.usuarioExito.set('');
    this.mostrarCambioPassword.set(false);
  }

  cancelarEdicionPerfil(): void {
    this.editandoPerfil.set(false);
    this.perfilError.set('');
    this.usuarioError.set('');
    this.mostrarCambioPassword.set(false);
  }

  guardarPerfil(): void {
    this.guardandoPerfil.set(true);
    this.perfilError.set('');
    this.perfilExito.set('');

    let completados = 0;
    const totalOps = 2;
    let hayError = false;

    const checkFinish = () => {
      completados++;
      if (completados >= totalOps) {
        this.guardandoPerfil.set(false);
        if (!hayError) {
          this.editandoPerfil.set(false);
          this.notificacion.exito('Perfil actualizado', 'Los datos se han guardado correctamente.');
        }
      }
    };

    this.usuarioService.actualizarUsuario(this.usuarioForm()).subscribe({
      next: (u) => {
        this.usuarioInfo.set(u);
        // Actualizar nombre en localStorage
        const current = this.currentUser();
        if (current) {
          const updated = { ...current, user: u.nombreCompleto };
          this.currentUser.set(updated);
          localStorage.setItem('nexopostal_user', JSON.stringify(updated));
        }
        checkFinish();
      },
      error: (err) => {
        hayError = true;
        this.notificacion.errorHttp(err, 'Error al actualizar datos de cuenta');
        checkFinish();
      }
    });

    const form = this.perfilForm();
    const perfilData: ActualizarPerfilDto = {
      dni: form.dni || undefined,
      telefono: form.telefono || undefined,
      direccionPredeterminada: form.direccionPredeterminada || undefined
    };
    this.perfilService.actualizarPerfil(perfilData).subscribe({
      next: (perfil) => {
        this.perfil.set(perfil);
        checkFinish();
      },
      error: (err) => {
        hayError = true;
        this.notificacion.errorHttp(err, 'Error al guardar el perfil');
        checkFinish();
      }
    });
  }

  updatePerfilField(field: keyof ActualizarPerfilDto, value: string): void {
    this.perfilForm.set({ ...this.perfilForm(), [field]: value });
  }

  updateUsuarioField(field: keyof ActualizarUsuarioDto, value: string): void {
    this.usuarioForm.set({ ...this.usuarioForm(), [field]: value });
  }

  toggleCambioPassword(): void {
    this.mostrarCambioPassword.set(!this.mostrarCambioPassword());
    this.passwordForm.set({ passwordActual: '', nuevaPassword: '', confirmar: '' });
    this.passwordError.set('');
    this.passwordExito.set('');
  }

  updatePasswordField(field: 'passwordActual' | 'nuevaPassword' | 'confirmar', value: string): void {
    this.passwordForm.set({ ...this.passwordForm(), [field]: value });
  }

  guardarPassword(): void {
    const form = this.passwordForm();
    if (!form.passwordActual || !form.nuevaPassword) {
      this.notificacion.aviso('Campos incompletos', 'Completa todos los campos de contraseña.');
      return;
    }
    if (form.nuevaPassword.length < 6) {
      this.notificacion.aviso('Contraseña muy corta', 'La nueva contraseña debe tener al menos 6 caracteres.');
      return;
    }
    if (form.nuevaPassword !== form.confirmar) {
      this.notificacion.aviso('No coinciden', 'Las contraseñas no coinciden.');
      return;
    }
    this.guardandoPassword.set(true);
    this.passwordError.set('');
    this.passwordExito.set('');

    this.usuarioService.cambiarPassword({
      passwordActual: form.passwordActual,
      nuevaPassword: form.nuevaPassword
    }).subscribe({
      next: () => {
        this.guardandoPassword.set(false);
        this.notificacion.exito('Contraseña actualizada', 'Tu contraseña se ha cambiado correctamente.');
        this.passwordForm.set({ passwordActual: '', nuevaPassword: '', confirmar: '' });
        setTimeout(() => this.mostrarCambioPassword.set(false), 1000);
      },
      error: (err) => {
        this.guardandoPassword.set(false);
        this.notificacion.errorHttp(err, 'Error al cambiar la contraseña');
      }
    });
  }

  esPredeterminada(id: number): boolean {
    return this.perfil()?.direccionPredeterminada === id.toString();
  }

  establecerComoPredeterminada(id: number): void {
    const perfil = this.perfil();
    const datos: ActualizarPerfilDto = {
      dni: perfil?.dni || undefined,
      telefono: perfil?.telefono || undefined,
      direccionPredeterminada: id.toString()
    };
    this.perfilService.actualizarPerfil(datos).subscribe({
      next: (p) => this.perfil.set(p),
      error: (err) => this.notificacion.errorHttp(err, 'Error al actualizar dirección predeterminada')
    });
  }

  // ===== DIRECCIONES =====

  cargarDirecciones(): void {
    this.perfilService.obtenerDirecciones().subscribe({
      next: (dirs) => this.direcciones.set(dirs),
      error: () => this.direcciones.set([])
    });
  }

  abrirFormDireccion(): void {
    this.editandoDireccionId.set(null);
    this.nuevaDireccion.set({
      alias: '', nombreDestinatario: '', direccion: '', codigoPostal: '', ciudad: '', provincia: '', telefono: ''
    });
    this.mostrarFormDireccion.set(true);
    this.direccionError.set('');
  }

  editarDireccion(dir: DireccionFavoritaDto): void {
    this.editandoDireccionId.set(dir.id);
    this.nuevaDireccion.set({
      alias: dir.alias,
      nombreDestinatario: dir.nombreDestinatario,
      direccion: dir.direccion,
      codigoPostal: dir.codigoPostal,
      ciudad: dir.ciudad,
      provincia: dir.provincia,
      telefono: dir.telefono || ''
    });
    this.mostrarFormDireccion.set(true);
    this.direccionError.set('');
  }

  cerrarFormDireccion(): void {
    this.mostrarFormDireccion.set(false);
    this.editandoDireccionId.set(null);
    this.direccionError.set('');
  }

  updateDireccionField(field: keyof CrearDireccionFavoritaDto, value: string): void {
    this.nuevaDireccion.set({ ...this.nuevaDireccion(), [field]: value });
  }

  guardarDireccion(): void {
    const dir = this.nuevaDireccion();
    if (!dir.alias || !dir.nombreDestinatario || !dir.direccion || !dir.codigoPostal || !dir.ciudad || !dir.provincia) {
      this.notificacion.aviso('Campos incompletos', 'Completa todos los campos obligatorios de la dirección.');
      return;
    }
    if (!/^\d{5}$/.test(dir.codigoPostal)) {
      this.notificacion.aviso('Código postal inválido', 'El código postal debe tener exactamente 5 dígitos numéricos.');
      return;
    }

    this.guardandoDireccion.set(true);
    this.direccionError.set('');

    const editId = this.editandoDireccionId();

    if (editId !== null) {
      // Modo edición
      this.perfilService.actualizarDireccion(editId, dir).subscribe({
        next: () => {
          this.guardandoDireccion.set(false);
          this.cerrarFormDireccion();
          this.cargarDirecciones();
          this.notificacion.exito('Dirección actualizada', 'Los cambios se han guardado correctamente.');
        },
        error: (err) => {
          this.guardandoDireccion.set(false);
          this.notificacion.errorHttp(err, 'Error al actualizar la dirección');
        }
      });
    } else {
      // Modo creación
      this.perfilService.agregarDireccion(dir).subscribe({
        next: (nuevaDir) => {
          this.guardandoDireccion.set(false);
          this.cerrarFormDireccion();
          // Si es la primera dirección, marcarla como predeterminada automáticamente
          if (this.direcciones().length === 0 && nuevaDir?.id) {
            this.perfilService.obtenerDirecciones().subscribe({
              next: (dirs) => {
                this.direcciones.set(dirs);
                this.establecerComoPredeterminada(nuevaDir.id);
              }
            });
          } else {
            this.cargarDirecciones();
          }
        },
        error: (err) => {
          this.guardandoDireccion.set(false);
          this.notificacion.errorHttp(err, 'Error al guardar la dirección');
        }
      });
    }
  }

  async eliminarDireccion(id: number): Promise<void> {
    const ok = await this.confirmacionService.confirmar({
      titulo: 'Eliminar dirección',
      mensaje: '¿Eliminar esta dirección de tu agenda?',
      textoConfirmar: 'Eliminar',
      tipo: 'peligro'
    });
    if (!ok) return;

    this.ejecutarEliminarDireccion(id);
  }

  ejecutarEliminarDireccion(id: number): void {

    this.perfilService.eliminarDireccion(id).subscribe({
      next: () => {
        this.cargarDirecciones();
        // Si era la predeterminada, limpiarla
        if (this.perfil()?.direccionPredeterminada === id.toString()) {
          const datos: ActualizarPerfilDto = {
            dni: this.perfil()?.dni || '',
            telefono: this.perfil()?.telefono || '',
            direccionPredeterminada: ''
          };
          this.perfilService.actualizarPerfil(datos).subscribe({
            next: (p) => this.perfil.set(p)
          });
        }
      },
      error: (err) => this.notificacion.errorHttp(err, 'Error al eliminar la dirección')
    });
  }

  // ===== ENVÍOS =====

  cargarEnvios(): void {
    this.cargandoEnvios.set(true);
    this.enviosService.obtenerMisEnvios().subscribe({
      next: (envios) => {
        this.envios.set(envios);
        this.cargandoEnvios.set(false);
      },
      error: () => {
        this.envios.set([]);
        this.cargandoEnvios.set(false);
      }
    });
  }

  getEstadoClase(estado: string): string {
    const clases: Record<string, string> = {
      'Admitido': 'bg-blue-100 text-blue-700',
      'EnTransito': 'bg-yellow-100 text-yellow-700',
      'EnOficina': 'bg-purple-100 text-purple-700',
      'EnReparto': 'bg-orange-100 text-orange-700',
      'Entregado': 'bg-green-100 text-green-700',
      'Incidencia': 'bg-red-100 text-red-700',
      'Devuelto': 'bg-gray-100 text-gray-700'
    };
    return clases[estado] || 'bg-gray-100 text-gray-700';
  }

  getEstadoTexto(estado: string): string {
    const textos: Record<string, string> = {
      'Admitido': 'Admitido',
      'EnTransito': 'En tránsito',
      'EnOficina': 'En oficina',
      'EnReparto': 'En reparto',
      'Entregado': 'Entregado',
      'Incidencia': 'Incidencia',
      'Devuelto': 'Devuelto'
    };
    return textos[estado] || estado;
  }

  descargarEtiqueta(numero: string): void {
    this.enviosService.descargarEtiqueta(numero).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `etiqueta-${numero}.pdf`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => this.notificacion.errorHttp(err, 'Error al descargar la etiqueta')
    });
  }

  descargarFactura(numero: string): void {
    this.enviosService.descargarFactura(numero).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `factura-${numero}.pdf`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => this.notificacion.errorHttp(err, 'Error al descargar la factura')
    });
  }

  // ===== AUTH =====

  async logout(): Promise<void> {
    this.closeUserMenu();
    const ok = await this.confirmacionService.confirmar({
      titulo: 'Cerrar sesión',
      mensaje: '¿Estás seguro de que deseas cerrar sesión?',
      textoConfirmar: 'Cerrar sesión',
      tipo: 'peligro'
    });
    if (!ok) return;
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
