import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  BroadcastService,
  BroadcastTipo,
  BroadcastAlcance,
  BroadcastRol
} from '../../services/broadcast.service';
import { AdminService, CtaResumenDto } from '../../services/admin.service';

interface HistorialItem {
  fechaIso: string;
  titulo: string;
  mensaje: string;
  tipo: BroadcastTipo;
  alcance: BroadcastAlcance;
  ctaId?: number | null;
  ctaNombre?: string | null;
  rol?: BroadcastRol | null;
}

const HIST_KEY = 'broadcast-historial-admin';

@Component({
  selector: 'app-broadcast-notificaciones',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './broadcast-notificaciones.component.html',
  styleUrl: './broadcast-notificaciones.component.css'
})
export class BroadcastNotificacionesComponent implements OnInit {
  private readonly api = inject(BroadcastService);
  private readonly adminApi = inject(AdminService);
  private readonly router = inject(Router);

  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  ctas = signal<CtaResumenDto[]>([]);
  historial = signal<HistorialItem[]>([]);

  // Form
  titulo = signal<string>('');
  mensaje = signal<string>('');
  tipo = signal<BroadcastTipo>('info');
  alcance = signal<BroadcastAlcance>('all');
  ctaId = signal<number | null>(null);
  rol = signal<BroadcastRol | null>(null);

  totalEnviados = computed(() => this.historial().length);

  ngOnInit(): void {
    this.cargarCtas();
    this.cargarHistorial();
  }

  private cargarCtas(): void {
    this.adminApi.obtenerCtas().subscribe({
      next: list => this.ctas.set(list),
      error: () => { /* silencioso, sólo es ayuda para el select */ }
    });
  }

  private cargarHistorial(): void {
    try {
      const raw = sessionStorage.getItem(HIST_KEY);
      if (raw) this.historial.set(JSON.parse(raw) as HistorialItem[]);
    } catch {
      this.historial.set([]);
    }
  }

  private persistirHistorial(item: HistorialItem): void {
    const next = [item, ...this.historial()].slice(0, 50);
    this.historial.set(next);
    try { sessionStorage.setItem(HIST_KEY, JSON.stringify(next)); } catch { /* ignore */ }
  }

  alcanceChange(value: BroadcastAlcance): void {
    this.alcance.set(value);
    if (value === 'all' || value === 'admin') {
      this.ctaId.set(null);
      this.rol.set(null);
    } else if (value === 'cta') {
      this.rol.set(null);
    } else if (value === 'cta-rol') {
      if (!this.rol()) this.rol.set('cta');
    }
  }

  enviar(): void {
    this.error.set(null);
    this.success.set(null);
    const titulo = this.titulo().trim();
    const mensaje = this.mensaje().trim();
    if (!titulo) { this.error.set('El título es obligatorio.'); return; }
    if (!mensaje) { this.error.set('El mensaje es obligatorio.'); return; }

    const alcance = this.alcance();
    const ctaId = alcance === 'cta' || alcance === 'cta-rol' ? this.ctaId() : null;
    const rol = alcance === 'cta-rol' ? this.rol() : null;

    if ((alcance === 'cta' || alcance === 'cta-rol') && !ctaId) {
      this.error.set('Selecciona un CTA.'); return;
    }
    if (alcance === 'cta-rol' && !rol) {
      this.error.set('Selecciona un rol dentro del CTA.'); return;
    }

    this.saving.set(true);
    this.api.enviar({
      titulo, mensaje,
      tipo: this.tipo(),
      alcance,
      ctaId,
      rol
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.success.set('Notificación enviada.');
        this.persistirHistorial({
          fechaIso: new Date().toISOString(),
          titulo, mensaje,
          tipo: this.tipo(),
          alcance,
          ctaId,
          ctaNombre: ctaId ? (this.ctas().find(c => c.id === ctaId)?.nombre ?? null) : null,
          rol
        });
        this.titulo.set('');
        this.mensaje.set('');
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'No se ha podido enviar la notificación.');
      }
    });
  }

  limpiarHistorial(): void {
    if (!confirm('¿Borrar historial local de notificaciones?')) return;
    this.historial.set([]);
    try { sessionStorage.removeItem(HIST_KEY); } catch { /* ignore */ }
  }

  badgeAlcance(item: HistorialItem): string {
    switch (item.alcance) {
      case 'all': return 'Todos';
      case 'admin': return 'Admin';
      case 'cta': return `CTA ${item.ctaNombre ?? item.ctaId}`;
      case 'cta-rol': return `CTA ${item.ctaNombre ?? item.ctaId} · ${item.rol}`;
    }
  }

  volver(): void { this.router.navigate(['/admin']); }
}
