import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  AdminClientesService,
  ClienteListItemDto,
  PerfilCompletoClienteDto
} from '../../services/admin-clientes.service';
import { estadoPublicoLabel } from '../../services/admin-envios.service';

@Component({
  selector: 'app-gestion-clientes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-clientes.component.html',
  styleUrl: './gestion-clientes.component.css'
})
export class GestionClientesComponent implements OnInit {
  private readonly api = inject(AdminClientesService);
  private readonly router = inject(Router);

  readonly estadoPublicoLabel = estadoPublicoLabel;

  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  clientes = signal<ClienteListItemDto[]>([]);
  filtroTexto = signal<string>('');
  filtroBloqueado = signal<'todos' | 'bloqueados' | 'activos'>('todos');

  perfil = signal<PerfilCompletoClienteDto | null>(null);
  clienteSeleccionado = signal<ClienteListItemDto | null>(null);
  vistaPerfil = signal<'datos' | 'agenda' | 'envios'>('datos');

  modalResetAbierto = signal(false);
  nuevaPassword = signal('');

  // KPIs
  totalClientes = computed(() => this.clientes().length);
  totalActivos = computed(() => this.clientes().filter(c => !c.bloqueado && !c.eliminado).length);
  totalBloqueados = computed(() => this.clientes().filter(c => c.bloqueado).length);

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.loading.set(true);
    this.error.set(null);
    const bloq = this.filtroBloqueado();
    const filtros = {
      q: this.filtroTexto() || null,
      bloqueado: bloq === 'todos' ? null : bloq === 'bloqueados'
    };
    this.api.listar(filtros).subscribe({
      next: data => { this.clientes.set(data); this.loading.set(false); },
      error: err => {
        this.error.set(err?.error?.message ?? 'Error al cargar clientes');
        this.loading.set(false);
      }
    });
  }

  verPerfil(c: ClienteListItemDto): void {
    this.clienteSeleccionado.set(c);
    this.vistaPerfil.set('datos');
    this.perfil.set(null);
    this.api.perfilCompleto(c.id).subscribe({
      next: p => this.perfil.set(p),
      error: err => this.error.set(err?.error?.message ?? 'Error al cargar perfil 360')
    });
  }

  cerrarPerfil(): void {
    this.perfil.set(null);
    this.clienteSeleccionado.set(null);
  }

  bloquear(c: ClienteListItemDto): void {
    if (!confirm(`¿Bloquear acceso de ${c.nombreCompleto}?`)) return;
    this.saving.set(true);
    this.api.bloquear(c.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.success.set(`Cliente ${c.nombreCompleto} bloqueado`);
        this.cargar();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'Error al bloquear');
      }
    });
  }

  desbloquear(c: ClienteListItemDto): void {
    this.saving.set(true);
    this.api.desbloquear(c.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.success.set(`Cliente ${c.nombreCompleto} desbloqueado`);
        this.cargar();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'Error al desbloquear');
      }
    });
  }

  abrirResetPassword(): void {
    this.nuevaPassword.set('');
    this.modalResetAbierto.set(true);
  }

  confirmarReset(): void {
    const c = this.clienteSeleccionado();
    const pwd = this.nuevaPassword().trim();
    if (!c) return;
    if (pwd.length < 6) {
      this.error.set('La contraseña debe tener al menos 6 caracteres');
      return;
    }
    this.saving.set(true);
    this.api.resetPassword(c.id, pwd).subscribe({
      next: () => {
        this.saving.set(false);
        this.modalResetAbierto.set(false);
        this.success.set(`Contraseña restablecida para ${c.nombreCompleto}`);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'Error al restablecer contraseña');
      }
    });
  }

  volver(): void { this.router.navigate(['/admin']); }
}
