import { Routes } from '@angular/router';
import { authGuard, loginGuard, adminGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent),
    canActivate: [loginGuard]
  },
  {
    path: 'admin',
    loadComponent: () => import('./pages/admin-panel/admin-panel.component').then(m => m.AdminPanelComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'seguimiento-interno',
    loadComponent: () => import('./pages/seguimiento-interno/seguimiento-interno.component').then(m => m.SeguimientoInternoComponent),
    canActivate: [authGuard]
  },
  {
    path: 'gestion-cta',
    loadComponent: () => import('./pages/gestion-cta/gestion-cta.component').then(m => m.GestionCtaComponent),
    canActivate: [authGuard]
  },
  {
    path: 'asignaciones',
    loadComponent: () => import('./pages/asignaciones/asignaciones.component').then(m => m.AsignacionesComponent),
    canActivate: [authGuard]
  },
  {
    // Ruta deprecada: el escaneo ahora vive integrado dentro de Asignaciones.
    path: 'escaneo',
    redirectTo: 'asignaciones',
    pathMatch: 'full'
  },
  {
    path: 'gestion-usuarios/:id',
    loadComponent: () => import('./pages/usuario-detalle/usuario-detalle.component').then(m => m.UsuarioDetalleComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'gestion-usuarios',
    loadComponent: () => import('./pages/gestion-usuarios/gestion-usuarios.component').then(m => m.GestionUsuariosComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'gestion-repartidores',
    loadComponent: () => import('./pages/gestion-repartidores/gestion-repartidores.component').then(m => m.GestionRepartidoresComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'gestion-ctas-admin',
    loadComponent: () => import('./pages/gestion-ctas-admin/gestion-ctas-admin.component').then(m => m.GestionCtasAdminComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'incidencias-globales',
    loadComponent: () => import('./pages/incidencias-globales/incidencias-globales.component').then(m => m.IncidenciasGlobalesComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'movimientos-globales',
    loadComponent: () => import('./pages/movimientos-globales/movimientos-globales.component').then(m => m.MovimientosGlobalesComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'gestion-tarifas',
    loadComponent: () => import('./pages/gestion-tarifas/gestion-tarifas.component').then(m => m.GestionTarifasComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'gestion-oficinas',
    loadComponent: () => import('./pages/gestion-oficinas/gestion-oficinas.component').then(m => m.GestionOficinasComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'gestion-vehiculos',
    loadComponent: () => import('./pages/gestion-vehiculos/gestion-vehiculos.component').then(m => m.GestionVehiculosComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'gestion-envios',
    loadComponent: () => import('./pages/gestion-envios/gestion-envios.component').then(m => m.GestionEnviosComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'gestion-clientes',
    loadComponent: () => import('./pages/gestion-clientes/gestion-clientes.component').then(m => m.GestionClientesComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'broadcast-notificaciones',
    loadComponent: () => import('./pages/broadcast-notificaciones/broadcast-notificaciones.component').then(m => m.BroadcastNotificacionesComponent),
    canActivate: [adminGuard]
  },
  {
    path: '',
    loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
