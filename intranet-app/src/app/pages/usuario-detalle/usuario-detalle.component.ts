import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, forkJoin, of, throwError } from 'rxjs';
import {
  AdminService,
  UsuarioAdminDto,
  CtaResumenDto,
  AdminOperarioDetalleDto,
  AdminOperarioCtaAsignacionDto
} from '../../services/admin.service';

const ROLES_CON_CONFIG_CTA = ['OperarioOficina', 'OperarioCTA', 'Supervisor'];

@Component({
  selector: 'app-usuario-detalle',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: 'usuario-detalle.component.html',
  styleUrls: ['usuario-detalle.component.css']
})
export class UsuarioDetalleComponent implements OnInit {
  private userId = '';

  usuario = signal<UsuarioAdminDto | null>(null);
  ctas = signal<CtaResumenDto[]>([]);
  detalleOperativo = signal<AdminOperarioDetalleDto | null>(null);

  loading = signal(true);
  saving = signal(false);

  error = signal<string | null>(null);
  actionError = signal<string | null>(null);
  actionOk = signal<string | null>(null);

  asignacionSeleccionadaId = signal<number | null>(null);
  nuevoCtaId = signal<number | null>(null);

  readonly esRolConConfigCta = computed(() => {
    const rol = this.usuario()?.rol;
    return !!rol && ROLES_CON_CONFIG_CTA.includes(rol);
  });

  readonly asignacionSeleccionada = computed<AdminOperarioCtaAsignacionDto | null>(() => {
    const detalle = this.detalleOperativo();
    const id = this.asignacionSeleccionadaId();
    if (!detalle || id === null) return null;

    return detalle.asignacionesCta.find(a => a.operarioCtaId === id) ?? null;
  });

  readonly puedeMoverCta = computed(() => {
    const asignacion = this.asignacionSeleccionada();
    const destino = this.nuevoCtaId();
    return !!asignacion && destino !== null && destino > 0 && destino !== asignacion.ctaId && !this.saving();
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private adminService: AdminService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('No se recibió el identificador de usuario.');
      this.loading.set(false);
      return;
    }

    this.userId = id;
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set(null);
    this.actionError.set(null);
    this.actionOk.set(null);

    forkJoin({
      usuario: this.adminService.obtenerDetalleUsuario(this.userId),
      ctas: this.adminService.obtenerCtas(),
      operativo: this.adminService.obtenerDetalleOperativoUsuario(this.userId).pipe(
        catchError((err) => {
          if (err.status === 404) return of(null);
          return throwError(() => err);
        })
      )
    }).subscribe({
      next: ({ usuario, ctas, operativo }) => {
        this.usuario.set(usuario);
        this.ctas.set(ctas);
        this.detalleOperativo.set(operativo);

        if (operativo && operativo.asignacionesCta.length > 0) {
          const primera = operativo.asignacionesCta[0];
          this.asignacionSeleccionadaId.set(primera.operarioCtaId);
          this.nuevoCtaId.set(primera.ctaId);
        } else {
          this.asignacionSeleccionadaId.set(null);
          this.nuevoCtaId.set(null);
        }

        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar el detalle del usuario.');
        this.loading.set(false);
      }
    });
  }

  onAsignacionChange(operarioCtaId: number): void {
    this.asignacionSeleccionadaId.set(+operarioCtaId);

    const asignacion = this.asignacionSeleccionada();
    this.nuevoCtaId.set(asignacion ? asignacion.ctaId : null);

    this.actionError.set(null);
    this.actionOk.set(null);
  }

  onNuevoCtaChange(ctaId: number): void {
    this.nuevoCtaId.set(+ctaId);
    this.actionError.set(null);
    this.actionOk.set(null);
  }

  moverCta(): void {
    const usuario = this.usuario();
    const asignacion = this.asignacionSeleccionada();
    const nuevoCtaId = this.nuevoCtaId();

    if (!usuario || !asignacion || nuevoCtaId === null) return;

    this.saving.set(true);
    this.actionError.set(null);
    this.actionOk.set(null);

    // Guardamos el CTA destino para localizar la asignación tras el refresh
    const ctaDestinoId = nuevoCtaId;

    this.adminService.moverCtaUsuario(usuario.id, nuevoCtaId, asignacion.operarioCtaId).subscribe({
      next: () => {
        this.actionOk.set('CTA actualizado correctamente.');
        this.recargarDetalleOperativo(ctaDestinoId);
      },
      error: (err) => {
        this.actionError.set(err.error?.message ?? 'No se pudo mover la asignación de CTA.');
        this.saving.set(false);
      }
    });
  }

  private recargarDetalleOperativo(ctaDestinoEsperado?: number): void {
    this.adminService.obtenerDetalleOperativoUsuario(this.userId).pipe(
      catchError((err) => {
        if (err.status === 404) return of(null);
        return throwError(() => err);
      })
    ).subscribe({
      next: (operativo) => {
        this.detalleOperativo.set(operativo);

        if (operativo && operativo.asignacionesCta.length > 0) {
          // 1º intento: si tras el move sabemos el CTA destino, seleccionamos esa asignación.
          // 2º intento: misma operarioCtaId que antes (caso sin cambio).
          // 3º fallback: primera asignación.
          const seleccion =
            (ctaDestinoEsperado !== undefined
              ? operativo.asignacionesCta.find(a => a.ctaId === ctaDestinoEsperado)
              : undefined)
            ?? operativo.asignacionesCta.find(a => a.operarioCtaId === this.asignacionSeleccionadaId())
            ?? operativo.asignacionesCta[0];

          this.asignacionSeleccionadaId.set(seleccion.operarioCtaId);
          this.nuevoCtaId.set(seleccion.ctaId);
        } else {
          this.asignacionSeleccionadaId.set(null);
          this.nuevoCtaId.set(null);
        }

        this.saving.set(false);
      },
      error: () => {
        this.actionError.set('El cambio se aplicó, pero no se pudo refrescar el detalle operativo.');
        this.saving.set(false);
      }
    });
  }

  volver(): void {
    this.router.navigate(['/gestion-usuarios']);
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
