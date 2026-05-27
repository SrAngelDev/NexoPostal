import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, forkJoin, map, of, switchMap, throwError } from 'rxjs';
import {
  AdminService,
  UsuarioAdminDto,
  CtaResumenDto,
  AdminOperarioDetalleDto,
  AdminOperarioCtaAsignacionDto,
  AdminOperarioOficinaDto,
  OficinaJsonResumen,
  AdminEditarEmpleadoDto
} from '../../services/admin.service';

const ROLES_CON_CONFIG_CTA = ['OperarioOficina', 'OperarioCTA', 'Supervisor'];
const ROLES_EMPLEADO = ['Admin', 'OperarioOficina', 'OperarioCTA', 'Supervisor', 'Repartidor', 'JefeReparto'];

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

  // Asignación de oficina (solo OperarioOficina)
  oficinaActual = signal<AdminOperarioOficinaDto | null>(null);
  oficinasDisponibles = signal<OficinaJsonResumen[]>([]);
  oficinaCtaContexto = signal<number | null>(null);
  cargandoOficinas = signal(false);
  nuevoOficinaJsonId = signal<number | null>(null);
  savingOficina = signal(false);
  actionOficinaError = signal<string | null>(null);
  actionOficinaOk = signal<string | null>(null);

  // ─── Edición de datos básicos del empleado ───
  editando = signal(false);
  savingEdicion = signal(false);
  actionEdicionError = signal<string | null>(null);
  actionEdicionOk = signal<string | null>(null);
  formEdicion = signal<AdminEditarEmpleadoDto>({
    nombreCompleto: '',
    email: '',
    codigoEmpleado: '',
    phoneNumber: '',
    rol: ''
  });
  readonly rolesEmpleado = ROLES_EMPLEADO;

  // ─── Borrado lógico ───
  savingEliminar = signal(false);
  actionEliminarError = signal<string | null>(null);

  readonly esOperarioOficina = computed(() => this.usuario()?.rol === 'OperarioOficina');

  readonly puedeGuardarOficina = computed(() => {
    const destino = this.nuevoOficinaJsonId();
    if (destino === null || destino <= 0 || this.savingOficina()) return false;
    const actual = this.oficinaActual();
    return !actual || actual.oficinaJsonId !== destino;
  });

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

  readonly puedeAsignarPrimera = computed(() => {
    const detalle = this.detalleOperativo();
    const yaTiene = !!detalle && detalle.asignacionesCta.length > 0;
    const destino = this.nuevoCtaId();
    return !yaTiene && destino !== null && destino > 0 && !this.saving();
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
      ctas: this.adminService.obtenerCtas()
    }).pipe(
      switchMap(({ usuario, ctas }) => {
        const operativo$ = ROLES_CON_CONFIG_CTA.includes(usuario.rol)
          ? this.adminService.obtenerDetalleOperativoUsuario(this.userId).pipe(
              catchError((err) => {
                if (err.status === 404) return of(null);
                return throwError(() => err);
              })
            )
          : of(null);

        return operativo$.pipe(map(operativo => ({ usuario, ctas, operativo })));
      })
    ).subscribe({
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

        // Cargar oficina solo si es OperarioOficina
        if (usuario.rol === 'OperarioOficina') {
          this.cargarOficinaUsuario();
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

  asignarPrimera(): void {
    const usuario = this.usuario();
    const nuevoCtaId = this.nuevoCtaId();

    if (!usuario || nuevoCtaId === null) return;

    this.saving.set(true);
    this.actionError.set(null);
    this.actionOk.set(null);

    this.adminService.asignarPrimeraCtaUsuario(
      usuario.id,
      nuevoCtaId,
      usuario.nombreCompleto,
      usuario.codigoEmpleado ?? '',
      usuario.rol
    ).subscribe({
      next: () => {
        this.actionOk.set('CTA asignado correctamente.');
        this.recargarDetalleOperativo(nuevoCtaId);
      },
      error: (err) => {
        this.actionError.set(err.error?.message ?? 'No se pudo asignar el CTA.');
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

  // ─── Edición de datos básicos ───

  iniciarEdicion(): void {
    const u = this.usuario();
    if (!u) return;
    this.formEdicion.set({
      nombreCompleto: u.nombreCompleto,
      email: u.email,
      codigoEmpleado: u.codigoEmpleado ?? '',
      phoneNumber: u.phoneNumber ?? '',
      rol: u.rol
    });
    this.actionEdicionError.set(null);
    this.actionEdicionOk.set(null);
    this.editando.set(true);
  }

  cancelarEdicion(): void {
    this.editando.set(false);
    this.actionEdicionError.set(null);
  }

  actualizarFormEdicion<K extends keyof AdminEditarEmpleadoDto>(
    campo: K,
    valor: AdminEditarEmpleadoDto[K]
  ): void {
    this.formEdicion.update(f => ({ ...f, [campo]: valor }));
  }

  guardarEdicion(): void {
    const u = this.usuario();
    if (!u) return;

    const dto = this.formEdicion();
    if (!dto.nombreCompleto?.trim() || !dto.email?.trim() || !dto.rol) {
      this.actionEdicionError.set('Nombre, email y rol son obligatorios.');
      return;
    }

    this.savingEdicion.set(true);
    this.actionEdicionError.set(null);
    this.actionEdicionOk.set(null);

    const payload: AdminEditarEmpleadoDto = {
      nombreCompleto: dto.nombreCompleto.trim(),
      email: dto.email.trim(),
      codigoEmpleado: dto.codigoEmpleado?.trim() || undefined,
      phoneNumber: dto.phoneNumber?.trim() || undefined,
      rol: dto.rol
    };

    this.adminService.editarEmpleado(u.id, payload).subscribe({
      next: (actualizado) => {
        this.usuario.set(actualizado);
        this.actionEdicionOk.set('Datos actualizados correctamente.');
        this.editando.set(false);
        this.savingEdicion.set(false);
      },
      error: (err) => {
        this.actionEdicionError.set(err.error?.message ?? 'No se pudieron actualizar los datos.');
        this.savingEdicion.set(false);
      }
    });
  }

  // ─── Borrado lógico / restauración ───

  eliminarUsuario(): void {
    const u = this.usuario();
    if (!u || u.eliminado) return;

    const confirmado = window.confirm(
      `¿Seguro que quieres eliminar a ${u.nombreCompleto}? El usuario no podrá iniciar sesión y desaparecerá de los listados. Podrás restaurarlo más adelante.`
    );
    if (!confirmado) return;

    this.savingEliminar.set(true);
    this.actionEliminarError.set(null);

    this.adminService.eliminarUsuario(u.id).subscribe({
      next: () => {
        this.savingEliminar.set(false);
        this.usuario.set({
          ...u,
          eliminado: true,
          eliminadoEnUtc: new Date().toISOString(),
          bloqueado: true
        });
      },
      error: (err) => {
        this.actionEliminarError.set(err.error?.message ?? 'No se pudo eliminar el usuario.');
        this.savingEliminar.set(false);
      }
    });
  }

  restaurarUsuario(): void {
    const u = this.usuario();
    if (!u || !u.eliminado) return;

    this.savingEliminar.set(true);
    this.actionEliminarError.set(null);

    this.adminService.restaurarUsuario(u.id).subscribe({
      next: () => {
        this.savingEliminar.set(false);
        this.adminService.obtenerDetalleUsuario(u.id).subscribe({
          next: (actualizado) => this.usuario.set(actualizado)
        });
      },
      error: (err) => {
        this.actionEliminarError.set(err.error?.message ?? 'No se pudo restaurar el usuario.');
        this.savingEliminar.set(false);
      }
    });
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

  // ─── Asignación de oficina (solo OperarioOficina) ───

  private cargarOficinaUsuario(): void {
    this.adminService.obtenerOficinaUsuario(this.userId).subscribe({
      next: (oficina) => {
        this.oficinaActual.set(oficina);
        this.nuevoOficinaJsonId.set(oficina?.oficinaJsonId ?? null);
        // El OperarioOficina opera a nivel de oficina, no de CTA: cargamos TODAS
        // las oficinas para que el admin pueda cambiarlo sin tener que asignarle
        // un CTA antes (la asignación a CTA es opcional para este rol).
        this.cargarTodasOficinas();
      },
      error: () => {
        this.oficinaActual.set(null);
        this.cargarTodasOficinas();
      }
    });
  }

  private cargarTodasOficinas(): void {
    this.cargandoOficinas.set(true);
    this.oficinaCtaContexto.set(null);
    this.adminService.obtenerTodasOficinas().subscribe({
      next: (oficinas) => {
        this.oficinasDisponibles.set(oficinas);
        this.cargandoOficinas.set(false);
      },
      error: () => {
        this.oficinasDisponibles.set([]);
        this.cargandoOficinas.set(false);
      }
    });
  }

  onNuevoOficinaChange(oficinaId: number): void {
    this.nuevoOficinaJsonId.set(+oficinaId);
    this.actionOficinaError.set(null);
    this.actionOficinaOk.set(null);
  }

  guardarOficina(): void {
    const usuario = this.usuario();
    const destino = this.nuevoOficinaJsonId();
    if (!usuario || destino === null) return;

    this.savingOficina.set(true);
    this.actionOficinaError.set(null);
    this.actionOficinaOk.set(null);

    const yaTiene = !!this.oficinaActual();
    const obs = yaTiene
      ? this.adminService.actualizarOficinaUsuario(usuario.id, destino)
      : this.adminService.actualizarOficinaUsuario(usuario.id, destino, {
          nombreCompleto: usuario.nombreCompleto,
          codigoEmpleado: usuario.codigoEmpleado ?? '',
          rol: 'OperarioOficina'
        });

    obs.subscribe({
      next: (resultado) => {
        this.oficinaActual.set(resultado);
        this.nuevoOficinaJsonId.set(resultado.oficinaJsonId);
        this.actionOficinaOk.set('Oficina actualizada correctamente.');
        this.savingOficina.set(false);
      },
      error: (err) => {
        this.actionOficinaError.set(err.error?.message ?? 'No se pudo actualizar la oficina.');
        this.savingOficina.set(false);
      }
    });
  }
}
