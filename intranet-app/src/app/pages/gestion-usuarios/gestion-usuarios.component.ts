import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminService, UsuarioAdminDto, AdminCrearEmpleadoDto } from '../../services/admin.service';

const ROLES_EMPLEADO = [
  'Admin',
  'OperarioOficina',
  'OperarioCTA',
  'Supervisor',
  'Repartidor',
  'JefeReparto'
];

const ROLES_TODOS = ['', ...ROLES_EMPLEADO, 'Cliente'];

@Component({
  selector: 'app-gestion-usuarios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-usuarios.component.html',
  styleUrl: './gestion-usuarios.component.css'
})
export class GestionUsuariosComponent implements OnInit {
  readonly rolesEmpleado = ROLES_EMPLEADO;
  readonly rolesTodos    = ROLES_TODOS;

  usuarios    = signal<UsuarioAdminDto[]>([]);
  loading     = signal(false);
  error       = signal<string | null>(null);
  actionError = signal<string | null>(null);

  kpiActivos     = computed(() => this.usuarios().filter(u => !u.bloqueado && !u.eliminado).length);
  kpiBloqueados  = computed(() => this.usuarios().filter(u => u.bloqueado && !u.eliminado).length);
  kpiEliminados  = computed(() => this.usuarios().filter(u => u.eliminado).length);

  // Filtros
  filtroRol      = signal('');
  filtroBloqueado = signal<boolean | undefined>(undefined);
  filtroQ        = signal('');
  filtroIncluirEliminados = signal(false);

  // Modales
  modalCrear   = signal(false);
  modalReset   = signal(false);
  usuarioReset = signal<UsuarioAdminDto | null>(null);

  // Formulario crear
  formCrear: AdminCrearEmpleadoDto = {
    nombreCompleto: '',
    email: '',
    codigoEmpleado: '',
    rol: 'OperarioOficina',
    password: ''
  };
  formCrearError = signal<string | null>(null);
  formCrearOk    = signal(false);

  // Formulario reset password
  nuevaPassword  = '';
  resetOk        = signal(false);

  constructor(private adminService: AdminService, private router: Router) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set(null);
    this.actionError.set(null);

    const rol      = this.filtroRol() || undefined;
    const bloqueado = this.filtroBloqueado();
    const q        = this.filtroQ() || undefined;
    const incluirEliminados = this.filtroIncluirEliminados();

    this.adminService.listarUsuarios(rol, bloqueado, q, incluirEliminados).subscribe({
      next: (lista) => {
        this.usuarios.set(lista);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar la lista de usuarios.');
        this.loading.set(false);
      }
    });
  }

  cambiarRol(usuario: UsuarioAdminDto, nuevoRol: string): void {
    if (!nuevoRol || nuevoRol === usuario.rol) return;
    this.actionError.set(null);

    this.adminService.cambiarRol(usuario.id, nuevoRol).subscribe({
      next: () => {
        usuario.rol = nuevoRol;
        this.usuarios.set([...this.usuarios()]);
      },
      error: (err) => {
        const msg = err.error?.message ?? 'Error al cambiar el rol.';
        this.actionError.set(msg);
      }
    });
  }

  bloquear(usuario: UsuarioAdminDto): void {
    this.actionError.set(null);
    this.adminService.bloquearUsuario(usuario.id).subscribe({
      next: () => {
        usuario.bloqueado = true;
        this.usuarios.set([...this.usuarios()]);
      },
      error: (err) => {
        const msg = err.error?.message ?? 'Error al bloquear el usuario.';
        this.actionError.set(msg);
      }
    });
  }

  desbloquear(usuario: UsuarioAdminDto): void {
    this.actionError.set(null);
    this.adminService.desbloquearUsuario(usuario.id).subscribe({
      next: () => {
        usuario.bloqueado = false;
        this.usuarios.set([...this.usuarios()]);
      },
      error: (err) => {
        const msg = err.error?.message ?? 'Error al desbloquear el usuario.';
        this.actionError.set(msg);
      }
    });
  }

  // ─── Modal crear ───

  abrirModalCrear(): void {
    this.formCrear = { nombreCompleto: '', email: '', codigoEmpleado: '', rol: 'OperarioOficina', password: '' };
    this.formCrearError.set(null);
    this.formCrearOk.set(false);
    this.modalCrear.set(true);
  }

  cerrarModalCrear(): void {
    this.modalCrear.set(false);
  }

  crearEmpleado(): void {
    this.formCrearError.set(null);
    this.formCrearOk.set(false);

    this.adminService.crearEmpleado(this.formCrear).subscribe({
      next: (nuevo) => {
        this.formCrearOk.set(true);
        this.usuarios.set([nuevo, ...this.usuarios()]);
        setTimeout(() => this.cerrarModalCrear(), 1200);
      },
      error: (err) => {
        const msg = err.error?.message ?? 'Error al crear el empleado.';
        this.formCrearError.set(msg);
      }
    });
  }

  // ─── Modal reset password ───

  abrirModalReset(usuario: UsuarioAdminDto): void {
    this.usuarioReset.set(usuario);
    this.nuevaPassword = '';
    this.resetOk.set(false);
    this.modalReset.set(true);
  }

  cerrarModalReset(): void {
    this.modalReset.set(false);
    this.usuarioReset.set(null);
  }

  confirmarReset(): void {
    const u = this.usuarioReset();
    if (!u || !this.nuevaPassword) return;

    this.adminService.resetPasswordUsuario(u.id, this.nuevaPassword).subscribe({
      next: () => {
        this.resetOk.set(true);
        setTimeout(() => this.cerrarModalReset(), 1200);
      },
      error: (err) => {
        const msg = err.error?.message ?? 'Error al restablecer la contraseña.';
        this.actionError.set(msg);
        this.cerrarModalReset();
      }
    });
  }

  // ─── Utilidades ───

  volver(): void {
    this.router.navigate(['/admin']);
  }

  verDetalle(usuario: UsuarioAdminDto): void {
    this.router.navigate(['/gestion-usuarios', usuario.id]);
  }

  rolBadgeClass(rol: string): string {
    const map: Record<string, string> = {
      Admin: 'badge-admin',
      OperarioOficina: 'badge-oficina',
      OperarioCTA: 'badge-cta',
      Supervisor: 'badge-supervisor',
      Repartidor: 'badge-repartidor',
      JefeReparto: 'badge-jefe',
      Cliente: 'badge-cliente'
    };
    return map[rol] ?? 'badge-default';
  }
}
